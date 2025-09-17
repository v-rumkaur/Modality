namespace CRM.ICon.Modality.Services.Modality
{
    public static class Constants
    {
        public static class Common
        {
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
        }
    }
}
