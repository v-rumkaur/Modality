
using CRM.ICon.Modality.Helpers;
using CRM.ICon.Modality.Helpers.Telemetry;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CRM.ICon.Modality.Services.VDM
{
    public class VDMService : IVDMService
    {
        private readonly HttpClient httpClient;
        private readonly VDMConfiguration vdmConfiguration;
        private readonly ITelemetryService _telemetryService;

        public VDMService(HttpClient httpClient, IOptions<VDMConfiguration> vdmConfiguration, ITelemetryService telemetryService)
        {
            this.httpClient = httpClient;
            this.vdmConfiguration = vdmConfiguration.Value;
            this._telemetryService = telemetryService;
        }
        public async Task<VDMResponse> GetVDMSkill(VDMRequest request)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            try
            {   
                var response = await httpClient.PostAsJsonAsync(vdmConfiguration.ServiceEndpoint, request);
                response.EnsureSuccessStatusCode();
                var stringresponse = await response.Content.ReadAsStringAsync();
                var deserializedResponse = JsonConvert.DeserializeObject<VDMResult>(stringresponse);
                return deserializedResponse.purposefulResults.FirstOrDefault();
            }
            catch (Exception ex)
            {
                this._telemetryService.LogException<VDMService>(ex, logProperties, "GetVDMSkill Failure");
                return null;
            }
        }
    }
}
