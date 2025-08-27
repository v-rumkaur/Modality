
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
                
                // Log the exact JSON payload being sent to VDM
                var jsonPayload = JsonConvert.SerializeObject(request);
                logProperties["VDMRequestPayload"] = jsonPayload;
                _telemetryService.LogTrace<VDMService>($"VDM JSON Payload: {jsonPayload}", logProperties);
                
                _telemetryService.LogTrace<VDMService>("Calling VDM endpoint", logProperties);
                var response = await httpClient.PostAsJsonAsync(vdmConfiguration.ServiceEndpoint, request);
                logProperties["StatusCode"] = response.StatusCode.ToString();
                
                // Log response headers for debugging
                var responseHeaders = new Dictionary<string, string>();
                foreach (var header in response.Headers)
                {
                    responseHeaders[header.Key] = string.Join(",", header.Value);
                }
                logProperties["ResponseHeaders"] = JsonConvert.SerializeObject(responseHeaders);
                logProperties["StatusCode"] = response.StatusCode.ToString();
                
                // Log response headers for debugging
               if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var message = await response.Content.ReadAsStringAsync();
                    logProperties.Add("ErrorDetails", message);
                    this._telemetryService.LogTrace<VDMService>("BadRequest from VDM service", logProperties);
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var message = await response.Content.ReadAsStringAsync();
                    logProperties.Add("UnauthorizedErrorDetails", message);
                    logProperties.Add("WWWAuthenticateHeader", response.Headers.WwwAuthenticate?.ToString() ?? "Not present");
                    this._telemetryService.LogError<VDMService>("VDM service returned 401 Unauthorized", logProperties);
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    var message = await response.Content.ReadAsStringAsync();
                    logProperties.Add("ForbiddenErrorDetails", message);
                    this._telemetryService.LogError<VDMService>("VDM service returned 403 Forbidden - access denied", logProperties);
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    var message = await response.Content.ReadAsStringAsync();
                    logProperties.Add("RateLimitErrorDetails", message);
                    logProperties.Add("RetryAfterHeader", response.Headers.RetryAfter?.ToString() ?? "Not present");
                    this._telemetryService.LogError<VDMService>("VDM service returned 429 Too Many Requests - rate limited", logProperties);
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    var message = await response.Content.ReadAsStringAsync();
                    logProperties.Add("ServerErrorDetails", message);
                    this._telemetryService.LogError<VDMService>("VDM service returned 500 Internal Server Error", logProperties);
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadGateway || 
                    response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || 
                    response.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)
                {
                    var message = await response.Content.ReadAsStringAsync();
                    logProperties.Add("ServiceUnavailableDetails", message);
                    logProperties.Add("ServiceStatus", response.StatusCode.ToString());
                    this._telemetryService.LogError<VDMService>("VDM service unavailable or gateway error", logProperties);
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var stringResponse = await response.Content.ReadAsStringAsync();
                var deserializedResponse = JsonConvert.DeserializeObject<VDMResult>(stringResponse);
                var vdmResponse = deserializedResponse?.Result?.purposefulResult;

                if (vdmResponse != null)
                {
                    AddInMemoryCacheEntry(vdmKey, vdmResponse);
                    logProperties["VDMResponse"] = JsonConvert.SerializeObject(vdmResponse);
                    _telemetryService.LogTrace<VDMService>("Returning VDM response", logProperties);
                }
                else
                {
                    _telemetryService.LogTrace<VDMService>("No VDM response found in results", logProperties);
                }

                return vdmResponse;
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
