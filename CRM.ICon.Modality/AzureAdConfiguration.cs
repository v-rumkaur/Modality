namespace CRM.ICon.Modality
{
    public class AzureAdConfiguration
    {
        /// <summary>
        /// Gets or sets the ClientId
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the TenantId
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the managed identity client id
        /// </summary>
        public string ManagedIdentityClientId { get; set; }

        public string?  Instance { get; set; }
        public string OAuthVersion { get; set; }
    }
}