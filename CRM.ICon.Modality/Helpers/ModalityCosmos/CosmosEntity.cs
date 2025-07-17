using Newtonsoft.Json;

namespace CRM.ICon.Modality.Helpers.ModalityCosmos
{
    public abstract class CosmosEntity
    {
        [JsonProperty("partitionKey")]
        public string PartitionKey { get; protected set; } = string.Empty;

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; private set; }

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; private set; }

        [JsonProperty("createdBy")]
        public string? CreatedBy { get; private set; }

        [JsonProperty("updatedBy")]
        public string? UpdatedBy { get; private set; }

        public void SetAudit(string user, bool isNew)
        {
            var now = DateTime.UtcNow;
            UpdatedAt = now;
            UpdatedBy = user;

            if (isNew || CreatedAt == default)
            {
                CreatedAt = now;
                CreatedBy = user;
            }
        }
    }
}
