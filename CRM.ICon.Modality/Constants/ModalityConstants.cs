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

        //Migrated from supportChannel
        public const string KeyvaultBaseUri = "KeyvaultBaseUri";
        public const string ManagedIdentityId = "ManagedIdentityId";
        public const string CRBSOCPSubscriptionKey = "Ocp-Apim-Subscription-Key";
        public const string CRBSOCPSubscriptionValue = "CRBS.OCPSubscriptionKey";
        public const string EncryptedSettingPrefix = "Encrypted.";
        public const string ConfigSectionName = "OCQueueAvailabilityServiceConfig";
        public const string KeyvaultConfigSectionName = "KeyvaultConfig";

        public const int DefaultCachedItemDurationInSeconds = -1; // never expires
        public const int CachedEwtDurationInSeconds = 120;
        public const string MicrosoftDomain = "microsoft.com";

        public static class WidgetMappingConstants
        {
            /// <summary>
            /// The partition key property name used in Cosmos DB documents.
            /// </summary>
            public const string PartitionKeyPath = "id";
        }
    }
}
