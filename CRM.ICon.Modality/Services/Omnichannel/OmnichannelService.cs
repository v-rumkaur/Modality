using Azure.Core;
using CRM.ICon.Modality.Helpers;
using CRM.ICon.Modality.Helpers.ModalityCosmos;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model;
using CRM.ICon.Modality.Services.VDM;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenTelemetry.Resources;
using System;
using System.Text.Json;

namespace CRM.ICon.Modality.Services.Omnichannel
{
    public class OmnichannelService : IOmnichannelService
    {
        private readonly HttpClient httpClient;
        private readonly OmnichannelConfiguration omnichannelConfiguration;
        private readonly Dictionary<string, WidgetDetails> widgetConfiguration;
        private readonly Dictionary<string, string> skillcharacteristicConfiguration;
        private readonly ITelemetryService _telemetryService;
        private readonly Dictionary<string, WorkstreamDetails> workstreamConfiguration;
        private readonly IModalityCosmosDbClient modalityCosmosDbClient;

        public OmnichannelService(HttpClient httpClient, IOptions<OmnichannelConfiguration> options, 
            IOptions<Dictionary<string, WidgetDetails>> widgetConfiguration, 
            IOptions<Dictionary<string, string>> skillcharacteristicConfiguration,
            ITelemetryService telemetryService, IOptions<Dictionary<string, WorkstreamDetails>> workstreamConfiguration, IModalityCosmosDbClient modalityCosmosDbClient)
        {
            this.httpClient = httpClient;
            this.omnichannelConfiguration = options.Value;
            this.widgetConfiguration = widgetConfiguration.Value;
            this.skillcharacteristicConfiguration = skillcharacteristicConfiguration.Value;
            this._telemetryService = telemetryService;
            this.workstreamConfiguration = workstreamConfiguration.Value;
            this.modalityCosmosDbClient = modalityCosmosDbClient;
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
                
                var agentAvailabilityUrl = omnichannelConfiguration.ServiceEndpoint + "/c2q/v1.0/getagentavailabilitypublic/" + workstreamDetails?.WorkstreamId + "/" + tracingId;

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

        public WidgetDetails GetWidgetDetails(string language, string source, string userType, bool isMCS, string ring)
        {
            if (string.IsNullOrEmpty(userType))
            {
                userType = "commercial"; //default value is commercial
            }

            WidgetDetails widgetDetails = new WidgetDetails();
            if (!isMCS)
            {
                string widgetkey = (source + "-" + language + "-" + userType).ToLowerInvariant();
                if (widgetConfiguration.TryGetValue(widgetkey, out widgetDetails))
                {

                    widgetDetails.OrgUrl = omnichannelConfiguration != null ? omnichannelConfiguration.OrgUrl : null;
                    widgetDetails.OrgId = omnichannelConfiguration != null ? omnichannelConfiguration.OrgId : null;
                }
            }
            else
            {
                string region;
                region = RingRegionalMapping.RingRegionalData.TryGetValue(ring, out region) ? region : "nam";

                string widgetkey = (source + "-" + language + "-" + userType + "-" + region).ToLowerInvariant();
                var response = modalityCosmosDbClient.GetItemAsync<WidgetMappingResponse>(widgetkey).Result;
                if (response != null)
                {
                    widgetDetails.WidgetId = response.Primary?.WidgetId;
                    widgetDetails.OrgUrl = omnichannelConfiguration != null ? omnichannelConfiguration.OrgUrl : null;
                    widgetDetails.OrgId = omnichannelConfiguration != null ? omnichannelConfiguration.OrgId : null;
                }
            }

            return widgetDetails;
        }

        public WidgetMappingResponse CreateWidgetDetails(WidgetMappingRequest widgetMappingRequest,string language, string source, string userType)
        {

            WidgetMappingResponse widgetMappingResponse = new WidgetMappingResponse();
            widgetMappingResponse.Source = source;
            widgetMappingResponse.Language = language;
            widgetMappingResponse.UserType = userType;
            widgetMappingResponse.IsMCS = widgetMappingRequest.IsMCS;

            widgetMappingResponse.Primary = new WidgetData
            {
                WidgetId = widgetMappingRequest.Primary?.WidgetId,
                WorkstreamId = widgetMappingRequest.Primary?.WorkstreamId,
            };

            widgetMappingResponse.Secondary = new WidgetData
            {
                WidgetId = widgetMappingRequest.Secondary?.WidgetId,
                WorkstreamId = widgetMappingRequest.Secondary?.WorkstreamId,
            };

            widgetMappingResponse.Backup = new WidgetData
            {
                WidgetId = widgetMappingRequest.Backup?.WidgetId,
                WorkstreamId = widgetMappingRequest.Backup?.WorkstreamId,
            };

            string ring = widgetMappingRequest.Ring ?? "Ring4";
            if (RingRegionalMapping.RingRegionalData.TryGetValue(ring, out string region))
            {
                widgetMappingResponse.Region = region;
            }
            else
            {
                widgetMappingResponse.Region = "nam";
            }

            if (string.IsNullOrEmpty(userType))
            {
                userType = "commercial"; //default value is commercial
            }

            if (widgetMappingRequest.IsMCS) {
                 widgetMappingResponse.Id = (source + "-" + language + "-" + userType + "-" + region).ToLowerInvariant();
            }
            else
            {
                widgetMappingResponse.Id = (source + "-" + language + "-" + userType).ToLowerInvariant();
            }

            var response = modalityCosmosDbClient.UpsertItemAsync<WidgetMappingResponse>(widgetMappingResponse).Result;

                return response;
        }

        public string GetSkillCharacteristicId(string skill)
        {
            skillcharacteristicConfiguration.TryGetValue(skill,out string characteristicid);
            return characteristicid;
        }
    }
}
