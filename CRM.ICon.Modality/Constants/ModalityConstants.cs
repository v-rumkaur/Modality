using System.Configuration;

namespace CRM.ICon.Modality
{
    public static class ModalityConstants
    {
        public const string TelemetryConfiguration = "TelemetryConfiguration";
        public const string KeyVaultConfiguration = "KeyVaultConfiguration";
        public const string ApplicationInsightsConnectionString =
            "ApplicationInsightsConnectionString";
        public const double VDMCacheTimeInMinutes = 30;

        //Headers
        public const string Target = "x-msaas-target";

        //RequestId
        public const string RequestId = "RequestId";

        public static class WidgetMappingConstants
        {
            /// <summary>
            /// The partition key property name used in Cosmos DB documents.
            /// </summary>
            public const string PartitionKeyPath = "id";

            /// <summary>
            /// The partition key value used for Widget Mapping documents.
            /// </summary>
            public const string PartitionKeyValue = "id";
        }
    }
}
