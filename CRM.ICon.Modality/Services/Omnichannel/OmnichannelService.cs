using CRM.ICon.Modality.Helpers;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model;
using CRM.ICon.Modality.Services.VDM;
using Microsoft.Extensions.Options;

namespace CRM.ICon.Modality.Services.Omnichannel
{
    public class OmnichannelService : IOmnichannelService
    {
        private readonly HttpClient httpClient;
        private readonly OmnichannelConfiguration omnichannelConfiguration;
        private readonly Dictionary<string, WidgetDetails> widgetConfiguration;
        private readonly Dictionary<string, string> skillcharacteristicConfiguration;
        private readonly ITelemetryService _telemetryService;

        public OmnichannelService(HttpClient httpClient, IOptions<OmnichannelConfiguration> options, 
            IOptions<Dictionary<string, WidgetDetails>> widgetConfiguration, 
            IOptions<Dictionary<string, string>> skillcharacteristicConfiguration,
            ITelemetryService telemetryService)
        {
            this.httpClient = httpClient;
            this.omnichannelConfiguration = options.Value;
            this.widgetConfiguration = widgetConfiguration.Value;
            this.skillcharacteristicConfiguration = skillcharacteristicConfiguration.Value;
            this._telemetryService = telemetryService;
        }

        public async Task<OmnichannelResponse> GetAgentAvailability(OmnichannelRequest request)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            try
            {
                var tracingId = Guid.NewGuid().ToString();
                var agentAvailabilityUrl = omnichannelConfiguration.ServiceEndpoint + "/routing/agentavailability/" + omnichannelConfiguration.WorkstreamId + "/" + tracingId;
                var response = await httpClient.PostAsJsonAsync(agentAvailabilityUrl, request.customContext);
                response.EnsureSuccessStatusCode();
                
                var deserializedResponse = await response.Content.ReadFromJsonAsync<OmnichannelResponse>();
                return deserializedResponse;
            }
            catch (Exception exception)
            {
                _telemetryService.LogException<OmnichannelService>(exception, null, "GetAgentAvailability Failure");
                return null;
            }
        }

        public WidgetDetails GetWidgetDetails(string language)
        {
            widgetConfiguration.TryGetValue(language, out WidgetDetails widgetDetails);
            if (widgetDetails != null)
            {
                widgetDetails.OrgUrl = omnichannelConfiguration.OrgUrl;
                widgetDetails.OrgId = omnichannelConfiguration.OrgId;
            }
 
            return widgetDetails;
        }

        public string GetSkillCharacteristicId(string skill)
        {
            skillcharacteristicConfiguration.TryGetValue(skill,out string characteristicid);
            return characteristicid;
        }
    }
}
