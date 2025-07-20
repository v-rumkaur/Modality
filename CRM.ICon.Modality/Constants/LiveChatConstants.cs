// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LiveChatConstants.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CRM.ICon.Modality
{
    /// <summary>
    /// Contains constants used throughout the Live Chat Settings application.
    /// </summary>
    public static class LiveChatConstants
    {
        /// <summary>
        /// The partition key property name used in Cosmos DB documents.
        /// </summary>
        public const string PartitionKeyPath = "partitionKey";

        /// <summary>
        /// The partition key value used for Live Chat Settings documents.
        /// </summary>
        public const string PartitionKeyValue = "livechat";

        /// <summary>
        /// The increment value used when adjusting evaluation orders.
        /// </summary>
        public const int EvaluationOrderIncrement = 1;

        /// <summary>
        /// The minimum allowed evaluation order value.
        /// </summary>
        public const int MinEvaluationOrder = 0;

        /// <summary>
        /// Contains SQL query definitions for Cosmos DB operations.
        /// </summary>
        public static class Queries
        {
            /// <summary>
            /// Query to retrieve all rules ordered by evaluation order.
            /// Parameters: @partitionKey
            /// </summary>
            public static readonly string GetAllRules =
                $"SELECT * FROM c WHERE c.{PartitionKeyPath} = @partitionKey ORDER BY c.evaluationOrder ASC";

            /// <summary>
            /// Query to find matching rules based on user context.
            /// Parameters: @partitionKey, @serviceLevel, @isRestricted, @sapId, @serviceId
            /// </summary>
            public static readonly string MatchRule =
                $@"SELECT * FROM c
                   WHERE c.{PartitionKeyPath} = @partitionKey
                     AND ARRAY_CONTAINS(c.allowedServiceLevels, @serviceLevel)
                     AND c.allowRestricted = @isRestricted
                     AND ARRAY_CONTAINS(c.allowedSaps, @sapId)
                     AND (NOT ARRAY_CONTAINS(c.excludedServiceIds, @serviceId))
                   ORDER BY c.evaluationOrder ASC";

            /// <summary>
            /// Query to retrieve rules at and after a specific evaluation order.
            /// Parameters: @partitionKey, @evaluationOrder
            /// </summary>
            public static readonly string GetRulesByEvaluationOrder =
                $"SELECT * FROM c WHERE c.{PartitionKeyPath} = @partitionKey AND c.evaluationOrder >= @evaluationOrder ORDER BY c.evaluationOrder ASC";

            /// <summary>
            /// Query to get the maximum evaluation order value.
            /// Parameters: @partitionKey
            /// </summary>
            public static readonly string GetMaxEvaluationOrder =
                $"SELECT VALUE MAX(c.evaluationOrder) FROM c WHERE c.{PartitionKeyPath} = @partitionKey";

            /// <summary>
            /// Query to retrieve a specific rule by name.
            /// Parameters: @partitionKey, @name
            /// </summary>
            public static readonly string GetRuleByName =
                $"SELECT * FROM c WHERE c.{PartitionKeyPath} = @partitionKey AND c.Name = @name";
        }
    }
}
