// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LiveChatRule.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using CRM.ICon.Modality.Helpers.ModalityCosmos;
using Newtonsoft.Json;

namespace CRM.ICon.Modality.Model.LiveChatSettings
{
    /// <summary>
    /// Represents a live chat rule for managing chat eligibility based on service levels, SAPs,
    /// and other criteria.
    /// </summary>
    public class LiveChatRule : CosmosEntity
    {
        /// <summary>
        /// Unique rule name that also serves as the Cosmos DB document ID.
        /// </summary>
        [JsonProperty("name")]
        public required string Name { get; set; } = string.Empty;

        /// <summary>
        /// The unique identifier for the document in Cosmos DB.
        /// This is set to match the Name property.
        /// TODO: make name the ID property
        /// </summary>
        [JsonProperty("id")]
        public string Id
        {
            get => Name;
            set => Name = value;
        }

        /// <summary>
        /// Allowed service levels (e.g., "Professional", "Premier").
        /// </summary>
        [JsonProperty("allowedServiceLevels")]
        public required List<string> AllowedServiceLevels { get; set; }

        /// <summary>
        /// Whether restricted users are allowed.
        /// </summary>
        [JsonProperty("allowRestricted")]
        public required bool AllowRestricted { get; set; }

        /// <summary>
        /// Allowed Support Area Path IDs (SAPs).
        /// </summary>
        [JsonProperty("allowedSaps")]
        public required List<string> AllowedSaps { get; set; }

        /// <summary>
        /// Service IDs to exclude from chat eligibility.
        /// </summary>
        [JsonProperty("excludedServiceIds")]
        public required List<int> ExcludedServiceIds { get; set; }

        /// <summary>
        /// Forces chat modality if true.
        /// </summary>
        [JsonProperty("isChatForced")]
        public required bool IsChatForced { get; set; }

        /// <summary>
        /// Rule evaluation order (lower = higher priority).
        /// </summary>
        [JsonProperty("evaluationOrder")]
        public int? EvaluationOrder { get; set; }

        public override string ToString() => $"Rule {Name}, Order {EvaluationOrder}";

        public LiveChatRule()
        {
            PartitionKey = LiveChatConstants.PartitionKeyValue;
        }
    }
}
