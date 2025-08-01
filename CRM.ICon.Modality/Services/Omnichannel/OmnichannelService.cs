using System.Text.Json;
using CRM.ICon.Modality.Helpers;
using CRM.ICon.Modality.Helpers.ModalityCosmos;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model;
using Microsoft.Extensions.Options;
using static CRM.ICon.Modality.ModalityConstants;

namespace CRM.ICon.Modality.Services.Omnichannel
{
    public class OmnichannelService : IOmnichannelService
    {
        private readonly HttpClient httpClient;
        private readonly ModalityCosmosDbConfiguration cosmosDbConfiguration;
        private readonly OmnichannelConfiguration omnichannelConfiguration;
        private readonly Dictionary<string, WidgetDetails> widgetConfiguration;
        private readonly Dictionary<string, string> skillcharacteristicConfiguration;
        private readonly ITelemetryService _telemetryService;
        private readonly Dictionary<string, WorkstreamDetails> workstreamConfiguration;
        private readonly IModalityCosmosDbClient modalityCosmosDbClient;
        private readonly string containerId;

        public OmnichannelService(
            HttpClient httpClient,
            IOptions<OmnichannelConfiguration> options,
            IOptions<ModalityCosmosDbConfiguration> cosmosDbConfiguration,
            IOptions<Dictionary<string, WidgetDetails>> widgetConfiguration,
            IOptions<Dictionary<string, string>> skillcharacteristicConfiguration,
            ITelemetryService telemetryService,
            IOptions<Dictionary<string, WorkstreamDetails>> workstreamConfiguration,
            IModalityCosmosDbClient modalityCosmosDbClient
        )
        {
            this.httpClient = httpClient;
            this.cosmosDbConfiguration =
                cosmosDbConfiguration?.Value
                ?? throw new ArgumentNullException(nameof(cosmosDbConfiguration));
            this.omnichannelConfiguration = options.Value;
            this.widgetConfiguration = widgetConfiguration.Value;
            this.skillcharacteristicConfiguration = skillcharacteristicConfiguration.Value;
            this._telemetryService = telemetryService;
            this.workstreamConfiguration = workstreamConfiguration.Value;
            this.modalityCosmosDbClient = modalityCosmosDbClient;
            this.containerId = this.cosmosDbConfiguration.ContainerIds.WidgetMapping;
        }

        public async Task<OmnichannelResponse> GetAgentAvailability(
            OmnichannelRequest request,
            string source,
            string userType,
            string requestId
        )
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties.AddObjectAsString("RequestId", requestId);
            try
            {
                var tracingId = Guid.NewGuid().ToString();
                string workstreamKey = (source + "-" + userType).ToLowerInvariant();
                workstreamConfiguration.TryGetValue(
                    workstreamKey,
                    out WorkstreamDetails workstreamDetails
                );
                var agentAvailabilityUrl =
                    omnichannelConfiguration.ServiceEndpoint
                    + "/c2q/v1.0/getagentavailabilitypublic/"
                    + workstreamDetails?.WorkstreamId
                    + "/"
                    + tracingId;

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null, // Prevent camelCase conversion
                };
                var response = await httpClient.PostAsJsonAsync(
                    agentAvailabilityUrl,
                    request,
                    options
                );
                response.EnsureSuccessStatusCode();
                var deserializedResponse =
                    await response.Content.ReadFromJsonAsync<OmnichannelResponse>();
                return deserializedResponse;
            }
            catch (Exception exception)
            {
                _telemetryService.LogException<OmnichannelService>(
                    exception,
                    logProperties,
                    "GetAgentAvailability Failure"
                );
                return null;
            }
        }

        public async Task<WidgetDetails> GetWidgetDetails(
            string language,
            string source,
            string userType,
            bool isMCS,
            string ring
        )
        {
            if (string.IsNullOrEmpty(userType))
            {
                userType = "commercial"; //default value is commercial
            }

            WidgetDetails widgetDetails = new WidgetDetails();
            widgetDetails.IsMCS = isMCS;
            if (!isMCS)
            {
                string widgetkey = (source + "-" + language + "-" + userType).ToLowerInvariant();
                if (widgetConfiguration.TryGetValue(widgetkey, out widgetDetails))
                {
                    widgetDetails.OrgUrl =
                        omnichannelConfiguration != null ? omnichannelConfiguration.OrgUrl : null;
                    widgetDetails.OrgId =
                        omnichannelConfiguration != null ? omnichannelConfiguration.OrgId : null;
                }
            }
            else
            {
                string region = RingRegionalMapping.RingRegionalData.TryGetValue(
                    ring,
                    out var regionResult
                )
                    ? regionResult
                    : "nam";

                string widgetkey = (
                    source + "-" + language + "-" + userType + "-" + region
                ).ToLowerInvariant();

                var response = await modalityCosmosDbClient.GetItemAsync<WidgetMappingResponse>(
                    widgetkey,
                    containerId
                );

                if (response != null)
                {
                    widgetDetails.WidgetId = response.IsBackUp
                        ? response.Backup?.WidgetId
                        : response.Primary?.WidgetId;
                    widgetDetails.BotId = response.IsBackUp
                        ? response.Backup?.BotId
                        : response.Primary?.BotId;
                    widgetDetails.OrgUrl = omnichannelConfiguration?.OrgUrl;
                    widgetDetails.OrgId = omnichannelConfiguration?.OrgId;
                }
            }

            return widgetDetails;
        }

        public async Task<WidgetMappingResponse> CreateWidgetDetails(
            WidgetMappingRequest widgetMappingRequest,
            string language,
            string source,
            string userType
        )
        {
            //default to commercial
            userType ??= "commercial";

            // Extract country
            string country = "us";
            if (!string.IsNullOrEmpty(widgetMappingRequest.Locale))
            {
                var parts = widgetMappingRequest.Locale.Replace('_', '-').Split('-');
                if (parts.Length > 1)
                {
                    country = parts[1].ToLowerInvariant();
                }
            }

            // Initialize response
            var widgetMappingResponse = new WidgetMappingResponse
            {
                Source = source,
                Language = language,
                UserType = userType,
                IsMCS = widgetMappingRequest.IsMCS,
                IsBackUp = widgetMappingRequest.IsBackUp,
                Country = country,
                Primary = new WidgetData
                {
                    WidgetId = widgetMappingRequest.Primary?.WidgetId,
                    WorkstreamId = widgetMappingRequest.Primary?.WorkstreamId,
                    BotId = widgetMappingRequest.Primary?.BotId,
                },
                Backup = new WidgetData
                {
                    WidgetId = widgetMappingRequest.Backup?.WidgetId,
                    WorkstreamId = widgetMappingRequest.Backup?.WorkstreamId,
                    BotId = widgetMappingRequest.Backup?.BotId,
                },
            };

            // Get region from ring
            string ring = widgetMappingRequest.Ring ?? "Ring4";
            widgetMappingResponse.Region = RingRegionalMapping.RingRegionalData.TryGetValue(
                ring,
                out string region
            )
                ? region
                : "nam";

            // Generate ID
            widgetMappingResponse.Id = widgetMappingResponse.IsMCS
                ? $"{source}-{language}-{userType}-{widgetMappingResponse.Region}".ToLowerInvariant()
                : $"{source}-{language}-{userType}".ToLowerInvariant();

            // Save to Cosmos DB
            return await modalityCosmosDbClient.UpsertItemAsync<WidgetMappingResponse>(
                containerId,
                widgetMappingResponse
            );
        }

        public string GetSkillCharacteristicId(string skill)
        {
            skillcharacteristicConfiguration.TryGetValue(skill, out string characteristicid);
            return characteristicid;
        }
    }
}
