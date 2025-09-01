namespace CRM.ICon.Modality.Configuration
{
    public class KeyVaultConfiguration
    {
        /// <summary>
        /// Gets or sets the Base Address
        /// </summary>
        public Uri BaseAddress { get; set; }

        /// <summary>
        /// Gets or sets the cache expiration in days
        /// </summary>
        public int CacheExpirationInDays { get; set; }

        /// <summary>
        /// Gets or sets the managed identity client id
        /// </summary>
        public string ManagedIdentityClientId { get; set; }
    }
}
