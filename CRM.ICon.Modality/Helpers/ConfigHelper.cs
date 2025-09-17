using CRM.ICon.Modality.Helpers.KeyVaultClient;
using CRM.ICon.Modality.Services.Modality;

namespace CRM.ICon.Modality.Helpers
{
    public static class ConfigHelper
    {
        public static string GetConfigValue(string key, IConfiguration configuration)
        {
            var keyValSecretProvider = GetKeyVaultSecretProvider(configuration);
            var configValue = configuration.GetStringValue(Constants.Common.ConfigSectionName, key);
            if (key.StartsWith(Constants.Common.EncryptedSettingPrefix) && !string.IsNullOrWhiteSpace(configValue))
            {
                configValue = keyValSecretProvider.GetSecretAsStringAsync(configValue).GetAwaiter().GetResult();
            }

            return configValue;
        }

        private static IKeyVaultSecretProvider GetKeyVaultSecretProvider(IConfiguration configuration)
        {
            var managedIdentityId = configuration.GetStringValue(Constants.Common.KeyvaultConfigSectionName, Constants.Common.ManagedIdentityId);
            var keyVaultBaseUri = configuration.GetStringValue(Constants.Common.KeyvaultConfigSectionName, Constants.Common.KeyvaultBaseUri);

            var keyVaultSecretProvider = new KeyVaultSecretProvider(
                      keyVaultBaseUri,
                      managedIdentityId);

            return keyVaultSecretProvider;
        }
    }
}

