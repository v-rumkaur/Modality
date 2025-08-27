
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
                _telemetryService.LogTrace<VDMService>("Calling VDM endpoint", logProperties);

                var jsonContent = JsonConvert.SerializeObject(request);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                logProperties["VDMRequestPayloadString"] = httpContent.ReadAsStringAsync().Result;

                var response = await httpClient.PostAsync(vdmConfiguration.ServiceEndpoint, httpContent);
                logProperties["StatusCode"] = response.StatusCode.ToString();
                logProperties["StatusCodeNumber"] = ((int)response.StatusCode).ToString();
                logProperties["ReasonPhrase"] = response.ReasonPhrase ?? "No reason phrase";

                var responseContent = await response.Content.ReadAsStringAsync();
                logProperties["VDMResponse"] = responseContent;
                _telemetryService.LogTrace<VDMService>("Received VDM response", logProperties);
                if (!response.IsSuccessStatusCode)
                {
                    this._telemetryService.LogTrace<VDMService>("VDM service call failed with " + response.StatusCode, logProperties);
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
