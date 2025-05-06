using Azure.Core;
using CRM.ICon.Modality.Helpers;
using CRM.ICon.Modality.Helpers.ModalityCosmos;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model;
using CRM.ICon.Modality.Services.VDM;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Text.Json;

namespace CRM.ICon.Modality.Services.Omnichannel
{
    public class OmnichannelEUService : IOmnichannelEUService
    {
        private readonly HttpClient httpClient;
        private readonly OmnichannelEUConfiguration omnichannelConfiguration;
        private readonly Dictionary<string, WidgetDetails> widgetConfiguration;
        private readonly Dictionary<string, string> skillcharacteristicConfiguration;
        private readonly ITelemetryService _telemetryService;
        private readonly Dictionary<string, WorkstreamDetails> workstreamConfiguration;

        public OmnichannelEUService(HttpClient httpClient, IOptions<OmnichannelEUConfiguration> options,
            IOptions<Dictionary<string, WidgetDetails>> widgetConfiguration,
            IOptions<Dictionary<string, string>> skillcharacteristicConfiguration,
            ITelemetryService telemetryService, IOptions<Dictionary<string, WorkstreamDetails>> workstreamConfiguration)
        {
            this.httpClient = httpClient;
            this.omnichannelConfiguration = options.Value;
            this.widgetConfiguration = widgetConfiguration.Value;
            this.skillcharacteristicConfiguration = skillcharacteristicConfiguration.Value;
            this._telemetryService = telemetryService;
            this.workstreamConfiguration = workstreamConfiguration.Value;
        }

        public async Task<OmnichannelResponse> GetAgentAvailability(OmnichannelRequest request, string source, string userType, string requestId)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties.AddObjectAsString("RequestId", requestId);
            try
            {
                var tracingId = Guid.NewGuid().ToString();
                string workstreamKey = (source + "-" + userType).ToLowerInvariant();
                workstreamConfiguration.TryGetValue(workstreamKey, out WorkstreamDetails workstreamDetails);
                var agentAvailabilityUrl = this.omnichannelConfiguration.ServiceEndpoint + "/c2q/v1.0/getagentavailabilitypublic/" + workstreamDetails?.WorkstreamId + "/" + tracingId;

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null // Prevent camelCase conversion
                };
                var response = await httpClient.PostAsJsonAsync(agentAvailabilityUrl, request, options);
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

        public async Task<WidgetDetails> GetWidgetDetails(string language, string source, string userType, bool isMCS, string Ring)
        {
            if (string.IsNullOrEmpty(userType))
            {
                userType = "commercial"; //default value is commercial
            }
            string widgetkey = (source + "-" + language + "-" + userType).ToLowerInvariant();
            if (widgetConfiguration.TryGetValue(widgetkey, out WidgetDetails widgetDetails))
            {
                
                widgetDetails.OrgUrl = this.omnichannelConfiguration != null ? omnichannelConfiguration.OrgUrl : null;
                widgetDetails.OrgId = this.omnichannelConfiguration != null ? omnichannelConfiguration.OrgId : null;
            }

            return widgetDetails;
        }

        public string GetSkillCharacteristicId(string skill)
        {
            skillcharacteristicConfiguration.TryGetValue(skill, out string characteristicid);
            return characteristicid;
        }

        public async Task<WidgetMappingResponse> CreateWidgetDetails(WidgetMappingRequest widgetmappingRequest, string language, string source, string userType)
        {
            throw new NotImplementedException();
        }
    }
}
