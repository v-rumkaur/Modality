using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.Collections.Concurrent;

namespace CRM.ICon.Modality.Helpers.KeyVaultClient
{
    public class KeyVaultSecretProvider : IKeyVaultSecretProvider
    {
        readonly string managedIdentityClientId;
        readonly string keyVaultBaseUri;
        readonly SecretClient secretClient;

        public KeyVaultSecretProvider(
            string keyVaultBaseUri,
            string managedIdentityClientId)
        {
            this.keyVaultBaseUri = keyVaultBaseUri;
            this.managedIdentityClientId = managedIdentityClientId;
            var credential = new ManagedIdentityCredential(this.managedIdentityClientId);
            this.secretClient = new SecretClient(new Uri(this.keyVaultBaseUri), credential);
        }

        /// <summary>
        /// Get pfx certificate secret from azure key vault
        /// </summary>
        /// <param name="secretName">Name of the secret.</param>
        /// <param name="loggingContext">The loggingContext</param>
        /// <returns><see cref="X509Certificate2"/></returns>
        public async Task<string> GetSecretAsStringAsync(string secretName)
        {
            string secretValue;
            if (!string.IsNullOrWhiteSpace(secretName))
            {
                if (ConcurrentSecretStringValueDictionary.TryGetValue(secretName, out secretValue))
                {
                    return secretValue;
                }

                var secret = await secretClient.GetSecretAsync(secretName);
                ConcurrentSecretStringValueDictionary.TryAdd(secretName, secret.Value.Value);
                return secret.Value.Value;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// The private static concurrent dictionary holding values of string secret values
        /// </summary>
        private static ConcurrentDictionary<string, string> ConcurrentSecretStringValueDictionary { get; set; } = new ConcurrentDictionary<string, string>();
    }
}