using CRM.ICon.Modality.Configuration;
//using Microsoft.Identity.Web;
using CRM.ICon.Modality.Common.KeyVault;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CRM.ICon.Modality.Services.Modality
{
    public class ConfigHelper
    {   public static string GetConfigValue(string key, IConfiguration configuration)
        {
  
            var keyValSecretProvider = GetKeyVaultSecretProvider(configuration);
            var configValue = configuration.GetStringValue(ModalityConstants.ConfigSectionName, key);
            if (key.StartsWith(ModalityConstants.EncryptedSettingPrefix) && !string.IsNullOrWhiteSpace(configValue))
            {
                configValue = keyValSecretProvider.GetSecretAsStringAsync(configValue).GetAwaiter().GetResult();
            }

            return configValue;
        }

    private static IKeyVaultSecretProvider GetKeyVaultSecretProvider(IConfiguration configuration)
    {
        var managedIdentityId = configuration.GetStringValue(ModalityConstants.KeyvaultConfigSectionName, ModalityConstants.ManagedIdentityId);
        var keyVaultBaseUri = configuration.GetStringValue(ModalityConstants.KeyvaultConfigSectionName, ModalityConstants.KeyvaultBaseUri);

        var keyVaultSecretProvider = new KeyVaultSecretProvider(
                  keyVaultBaseUri,
                  managedIdentityId);

        return keyVaultSecretProvider;
    }
}
}