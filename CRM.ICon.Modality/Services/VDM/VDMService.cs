
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
            logProperties.AddObjectAsString("RequestId", requestId);

            try
            {
                var vdmKey = (request.SapId + "-" + request.Text).ToLowerInvariant();

                if (this.memoryCache.TryGetValue(vdmKey, out VDMResponse cachedResponse))
                {
                    return cachedResponse;
                }

                var response = await httpClient.PostAsJsonAsync(vdmConfiguration.ServiceEndpoint, request);

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var message = await response.Content.ReadAsStringAsync();
                    logProperties.Add("ErrorDetails", message);
                    this._telemetryService.LogTrace<VDMService>("BadRequest from VDM service", logProperties);
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var stringResponse = await response.Content.ReadAsStringAsync();
                var deserializedResponse = JsonConvert.DeserializeObject<VDMResult>(stringResponse);
                var vdmResponse = deserializedResponse?.purposefulResults?.FirstOrDefault();

                if (vdmResponse != null)
                {
                    this.AddInMemoryCacheEntry(vdmKey, vdmResponse);
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
            this.memoryCache.Set(vdmKey, vdmValue, new MemoryCacheEntryOptions() { AbsoluteExpiration = DateTime.UtcNow.AddMinutes(Constants.VDMCacheTimeInMinutes) });
        }
    }
}
