
using Azure.Security.KeyVault.Secrets;
using CRM.ICon.Modality.Helpers;
using CRM.ICon.Modality.Helpers.Telemetry;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace CRM.ICon.Modality.Services.VDM
{
    public class VDMService : IVDMService
    {
        private readonly HttpClient httpClient;
        private readonly VDMConfiguration vdmConfiguration;
        private readonly ITelemetryService _telemetryService;
        private readonly IMemoryCache memoryCache;

        public VDMService(HttpClient httpClient, IOptions<VDMConfiguration> vdmConfiguration, ITelemetryService telemetryService, IMemoryCache cache)
        {
            this.httpClient = httpClient;
            this.vdmConfiguration = vdmConfiguration.Value;
            this._telemetryService = telemetryService;
            this.memoryCache = cache;
        }
        public async Task<VDMResponse> GetVDMSkill(VDMRequest request, string requestId)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["RequestId"] = requestId;
            logProperties["VDMRequest"] = JsonConvert.SerializeObject(request);
            _telemetryService.LogTrace<VDMService>("Received GetVDMSkill request", logProperties);

            try
            {
                var vdmKey = (request.SapId + "-" + request.Text).ToLowerInvariant();

                if (this.memoryCache.TryGetValue(vdmKey, out VDMResponse cachedResponse))
                {
                    logProperties["CacheHit"] = "true";
                    _telemetryService.LogTrace<VDMService>("Returning cached VDM response", logProperties);
                    return cachedResponse;
                }

                logProperties["CacheHit"] = "false";
                logProperties["VDMEndpoint"] = vdmConfiguration.ServiceEndpoint;

                // Log request headers for debugging
                var requestHeaders = new Dictionary<string, string>();
                foreach (var header in httpClient.DefaultRequestHeaders)
                {
                    requestHeaders[header.Key] = string.Join(",", header.Value);
                }
                logProperties["RequestHeaders"] = JsonConvert.SerializeObject(requestHeaders);


                var jsonContent = JsonConvert.SerializeObject(request);
                logProperties["VDMRequestPayload"] = jsonContent;
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                logProperties["VDMRequestPayloadString"] = httpContent.ReadAsStringAsync().Result;
                _telemetryService.LogTrace<VDMService>("Calling VDM endpoint", logProperties);

                var response = await httpClient.PostAsync(vdmConfiguration.ServiceEndpoint, httpContent);
                
                // COMPREHENSIVE RESPONSE LOGGING - Capture everything about the response
                logProperties["StatusCode"] = response.StatusCode.ToString();
                logProperties["StatusCodeNumber"] = ((int)response.StatusCode).ToString();
                logProperties["ReasonPhrase"] = response.ReasonPhrase ?? "No reason phrase";
                logProperties["IsSuccessStatusCode"] = response.IsSuccessStatusCode.ToString();
                logProperties["HttpVersion"] = response.Version?.ToString() ?? "Unknown";
                
                // Log all response headers
                var responseHeaders = new Dictionary<string, string>();
                foreach (var header in response.Headers)
                {
                    responseHeaders[header.Key] = string.Join(",", header.Value);
                }
                
                // Log content headers separately
                var contentHeaders = new Dictionary<string, string>();
                if (response.Content?.Headers != null)
                {
                    foreach (var header in response.Content.Headers)
                    {
                        contentHeaders[header.Key] = string.Join(",", header.Value);
                    }
                }
                
                logProperties["ResponseHeaders"] = JsonConvert.SerializeObject(responseHeaders);
                logProperties["ContentHeaders"] = JsonConvert.SerializeObject(contentHeaders);
                
                // Read and log the full response content
                var responseContent = await response.Content.ReadAsStringAsync();
                logProperties["VDMResponse"] = responseContent;
                logProperties["ResponseContentLength"] = responseContent?.Length.ToString() ?? "0";
                logProperties["ResponseContentType"] = response.Content?.Headers?.ContentType?.ToString() ?? "Unknown";
                
                // Log if response is empty or null
                logProperties["IsResponseEmpty"] = string.IsNullOrEmpty(responseContent).ToString();
                
                _telemetryService.LogTrace<VDMService>("FULL VDM RESPONSE DETAILS", logProperties);

                // Log response headers for debugging
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    logProperties.Add("ErrorDetails", responseContent);
                    this._telemetryService.LogTrace<VDMService>("BadRequest from VDM service", logProperties);
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logProperties.Add("UnauthorizedErrorDetails", responseContent);
                    logProperties.Add("WWWAuthenticateHeader", response.Headers.WwwAuthenticate?.ToString() ?? "Not present");
                    this._telemetryService.LogError<VDMService>("VDM service returned 401 Unauthorized", logProperties);
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    logProperties.Add("ForbiddenErrorDetails", responseContent);
                    this._telemetryService.LogError<VDMService>("VDM service returned 403 Forbidden - access denied", logProperties);
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    logProperties.Add("RateLimitErrorDetails", responseContent);
                    logProperties.Add("RetryAfterHeader", response.Headers.RetryAfter?.ToString() ?? "Not present");
                    this._telemetryService.LogError<VDMService>("VDM service returned 429 Too Many Requests - rate limited", logProperties);
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    logProperties.Add("ServerErrorDetails", responseContent);
                    this._telemetryService.LogError<VDMService>("VDM service returned 500 Internal Server Error", logProperties);
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadGateway ||
                    response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                    response.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)
                {
                    logProperties.Add("ServiceUnavailableDetails", responseContent);
                    logProperties.Add("ServiceStatus", response.StatusCode.ToString());
                    this._telemetryService.LogError<VDMService>("VDM service unavailable or gateway error", logProperties);
                    return null;
                }

                response.EnsureSuccessStatusCode();

                // Use the already-read response content and add detailed deserialization logging
                logProperties["RawResponseForDeserialization"] = responseContent ?? "NULL_RESPONSE";
                
                try
                {
                    // First, deserialize the outer response which has Result as a string
                    var outerResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    logProperties["OuterDeserializationSuccess"] = (outerResponse != null).ToString();
                    
                    if (outerResponse?.Result != null)
                    {
                        // The Result property is a JSON string that needs to be deserialized again
                        string resultJsonString = outerResponse.Result.ToString();
                        logProperties["ResultJsonString"] = resultJsonString;
                        
                        var deserializedResult = JsonConvert.DeserializeObject<VDMResultData>(resultJsonString);
                        logProperties["InnerDeserializationSuccess"] = (deserializedResult != null).ToString();
                        
                        if (deserializedResult != null)
                        {
                            logProperties["PurposefulResultsCount"] = deserializedResult.purposefulResults?.Count().ToString() ?? "0";
                            logProperties["PurposefulResultsExists"] = (deserializedResult.purposefulResults != null).ToString();
                        }
                        
                        var vdmResponse = deserializedResult?.purposefulResults?.FirstOrDefault();
                        logProperties["VDMResponseExtracted"] = (vdmResponse != null).ToString();
                        
                        if (vdmResponse != null)
                        {
                            AddInMemoryCacheEntry(vdmKey, vdmResponse);
                            logProperties["SerializedVDMResponse"] = JsonConvert.SerializeObject(vdmResponse);
                            _telemetryService.LogTrace<VDMService>("Successfully extracted and returning VDM response", logProperties);
                        }
                        else
                        {
                            logProperties["NoVDMResponseReason"] = "No purposefulResults found or empty array";
                            _telemetryService.LogTrace<VDMService>("No VDM response found in results", logProperties);
                        }

                        return vdmResponse;
                    }
                    else
                    {
                        logProperties["NoVDMResponseReason"] = "Result property is null or missing";
                        _telemetryService.LogTrace<VDMService>("No Result property found in VDM response", logProperties);
                        return null;
                    }
                }
                catch (JsonException jsonEx)
                {
                    logProperties["JsonDeserializationError"] = jsonEx.Message;
                    logProperties["JsonDeserializationStackTrace"] = jsonEx.StackTrace ?? "No stack trace";
                    _telemetryService.LogError<VDMService>("Failed to deserialize VDM response JSON", logProperties);
                    return null;
                }
            }
            catch (TaskCanceledException ex)
            {
                this._telemetryService.LogException<VDMService>(ex, logProperties, "VDM Service timeout");
                return null;
            }
            catch (Exception ex)
            {
                this._telemetryService.LogException<VDMService>(ex, logProperties, "GetVDMSkill failure");
                return null;
            }
        }

        /// <summary>
        /// Creates a vdm entry into InMemoryCache
        /// </summary>
        /// <typeparam name="T">vdm type</typeparam>
        /// <param name="vdmKey">vdm name</param>
        /// <param name="vdmValue">vdm value</param>
        private void AddInMemoryCacheEntry<T>(string vdmKey, T vdmValue)
        {
            this.memoryCache.CreateEntry(vdmKey);
            this.memoryCache.Set(vdmKey, vdmValue, new MemoryCacheEntryOptions() { AbsoluteExpiration = DateTime.UtcNow.AddMinutes(ModalityConstants.VDMCacheTimeInMinutes) });
        }
    }
}
