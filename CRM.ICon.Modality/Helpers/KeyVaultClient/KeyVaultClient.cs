using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure;
using CRM.ICon.Modality.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using CRM.ICon.Modality.Helpers.KeyVaultClient;

namespace CRM.ICon.Modality.Helpers.KeyvaultClient
{
    public class KeyVaultClient : IKeyVaultClient
    {
        private readonly IMemoryCache memoryCache;
        private readonly SecretClient secretClient;
        private readonly KeyVaultConfiguration keyvaultConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultClient"/> class.
        /// </summary>
        /// <param name="memoryCache">InMemoryCache object</param>
        /// <param name="keyVaultConfiguration">Keyvault configuration</param>
        public KeyVaultClient(IMemoryCache memoryCache, IOptions<KeyVaultConfiguration> keyVaultConfiguration)
        {
            this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            this.keyvaultConfiguration = keyVaultConfiguration?.Value ?? throw new ArgumentNullException(nameof(keyvaultConfiguration));
            this.secretClient = new SecretClient(this.keyvaultConfiguration.BaseAddress, new ManagedIdentityCredential(this.keyvaultConfiguration.ManagedIdentityClientId));
        }


        /// <summary>
        /// Gets secret from key vault
        /// </summary>
        /// <param name="secretName"></param>
        /// <returns></returns>
        public async Task<string> GetSecretAsync(string secretName)
        {
            if (!this.memoryCache.TryGetValue(secretName, out string secret))
            {
                KeyVaultSecret secretBundle = await this.secretClient.GetSecretAsync(secretName);
                secret = secretBundle.Value;
                this.AddInMemoryCacheEntry(secretName, secret);
            }
            return secret;
        }

        /// <summary>
        /// Gets certificate from key vault
        /// </summary>
        /// <param name="certificateName">Certificate Name</param>
        /// <returns>Certificate Bundle</returns>
        public async Task<X509Certificate2> GetCertificateAsync(string certificateName)
        {
            if (!this.memoryCache.TryGetValue(certificateName, out X509Certificate2 certificate))
            {
                try
                {
                    KeyVaultSecret certificateBundle = await this.secretClient.GetSecretAsync(certificateName);
                    certificate = new X509Certificate2(Convert.FromBase64String(certificateBundle.Value), string.Empty, X509KeyStorageFlags.MachineKeySet);
                    this.AddInMemoryCacheEntry(certificateName, certificate);
                }
                catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
                {
                    return null;
                }
            }
            return certificate;
        }

        /// <summary>
        /// Gets certificate properties from key vault
        /// </summary>
        /// <param name="certificateName">Certificate Name</param>
        /// <returns>Certificate Bundle</returns>
        public async Task<SecretProperties> GetCertificatePropertiesAsync(string certificateName)
        {
            if (!this.memoryCache.TryGetValue($"{certificateName}-properties", out SecretProperties certificateProperties))
            {
                try
                {
                    KeyVaultSecret certificateBundle = await this.secretClient.GetSecretAsync(certificateName);
                    certificateProperties = certificateBundle.Properties;
                    this.AddInMemoryCacheEntry($"{certificateName}-properties", certificateProperties);
                }
                catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
                {
                    return null;
                }
            }
            return certificateProperties;
        }

        /// <summary>
        /// Creates a secret entry into InMemoryCache
        /// </summary>
        /// <typeparam name="T">Secret type</typeparam>
        /// <param name="secretName">Secret name</param>
        /// <param name="secretValue">Secret value</param>
        private void AddInMemoryCacheEntry<T>(string secretName, T secretValue)
        {
            this.memoryCache.CreateEntry(secretName);
            this.memoryCache.Set(secretName, secretValue, new MemoryCacheEntryOptions() { AbsoluteExpiration = DateTime.UtcNow.AddDays(this.keyvaultConfiguration.CacheExpirationInDays) });
        }
    }
}
