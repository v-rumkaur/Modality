// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CosmosEntity.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;

namespace CRM.ICon.Modality.Helpers.ModalityCosmos
{
    /// <summary>
    /// Abstract base class for all Cosmos DB entities providing common properties and audit functionality.
    /// </summary>
    public abstract class CosmosEntity
    {
        /// <summary>
        /// The partition key value for this document.
        /// </summary>
        [JsonProperty(LiveChatConstants.PartitionKeyPath)]
        public string PartitionKey { get; set; } = LiveChatConstants.PartitionKeyValue;

        /// <summary>
        /// The UTC timestamp when this entity was created.
        /// </summary>
        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// The UTC timestamp when this entity was last updated.
        /// </summary>
        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; private set; }

        /// <summary>
        /// The identifier of the user who created this entity.
        /// </summary>
        [JsonProperty("createdBy")]
        public string? CreatedBy { get; private set; }

        /// <summary>
        /// The identifier of the user who last updated this entity.
        /// </summary>
        [JsonProperty("updatedBy")]
        public string? UpdatedBy { get; private set; }

        /// <summary>
        /// Updates audit fields for this entity.
        /// </summary>
        /// <param name="user">The user performing the operation.</param>
        /// <param name="isNew">True if this is a new entity, false if updating an existing entity.</param>
        public void SetAudit(string user, bool isNew)
        {
            if (string.IsNullOrWhiteSpace(user))
                throw new ArgumentException("User cannot be null or empty.", nameof(user));

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
