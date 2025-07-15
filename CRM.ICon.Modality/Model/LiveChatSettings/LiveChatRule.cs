using CRM.ICon.Modality.Helpers.Cosmos;
using Newtonsoft.Json;
using CRM.ICon.Modality.Helpers.ModalityCosmos;

namespace CRM.ICon.Modality.Model.LiveChatSettings
{
    public class LiveChatRule : CosmosEntity
    {
        // Cosmos DB ID (same as Name)
        [JsonProperty("id")]
        public string Id => Name;

        // Unique rule name
        [JsonProperty("name")]
        public required string Name { get; set; }

        public override string ToString() => $"Rule {Name}, Order {EvaluationOrder}";

        public LiveChatRule()
        {
            PartitionKey = "livechat"; // Set your logical partition group
        }

        // Allowed service levels (e.g., "Professional", "Premier")
        [JsonProperty("allowedServiceLevels")]
        public required List<string> AllowedServiceLevels { get; set; }

        // Whether restricted users are allowed
        [JsonProperty("allowRestricted")]
        public required bool AllowRestricted { get; set; }

        // Allowed Support Area Path IDs (SAPs)
        [JsonProperty("allowedSaps")]
        public required List<string> AllowedSaps { get; set; }

        // Service IDs to exclude from chat eligibility
        [JsonProperty("excludedServiceIds")]
        public required List<int> ExcludedServiceIds { get; set; }

        // Forces chat modality if true
        [JsonProperty("isChatForced")]
        public required bool IsChatForced { get; set; }

        // rule evaluation (lower = higher priority)
        [JsonProperty("evaluationOrder")]
        public int? EvaluationOrder { get; set; }
    }
}
