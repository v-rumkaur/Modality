using System.Configuration;

namespace CRM.ICon.Modality
{
    public static class Constants
    {
        public const string TelemetryConfiguration = "TelemetryConfiguration";
        public const string KeyVaultConfiguration = "KeyVaultConfiguration";
        public const string ApplicationInsightsConnectionString = "ApplicationInsightsConnectionString";
        public const double VDMCacheTimeInMinutes = 30;

        //Headers
        public const string Target = "x-msaas-target";

        //RequestId
        public const string RequestId = "RequestId";

        /// <summary>
        /// Constants related to LiveChat functionality
        /// </summary>
        public static class LiveChat
        {
            public const string PartitionKey = "LiveChat";

            /// <summary>
            /// Cosmos DB queries for LiveChat rules
            /// </summary>
            public static class Queries
            {
                public const string MATCH_USER = @"
                    SELECT * FROM c
                    WHERE c.partitionKey = @partitionKey
                      AND ARRAY_CONTAINS(c.allowedServiceLevels, @serviceLevel)
                      AND c.allowRestricted = @isRestricted
                      AND ARRAY_CONTAINS(c.allowedSaps, @sapId)
                      AND (NOT ARRAY_CONTAINS(c.excludedServiceIds, @serviceId))
                    ORDER BY c.evaluationOrder ASC";

                public const string GET_ALL_RULES = @"
                    SELECT * FROM c 
                    WHERE c.partitionKey = @partitionKey
                    ORDER BY c.evaluationOrder ASC";

                public const string GET_MAX_ORDER = @"
                    SELECT VALUE MAX(c.evaluationOrder) FROM c 
                    WHERE c.partitionKey = @partitionKey";

                public const string GET_RULES_AT_ORDER = @"
                    SELECT * FROM c 
                    WHERE c.partitionKey = @partitionKey 
                    AND c.evaluationOrder >= @order";
            }
        }
    }
}