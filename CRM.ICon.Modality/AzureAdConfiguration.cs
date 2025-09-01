namespace CRM.ICon.Modality
{
    public class AzureAdConfiguration
    {
        /// <summary>
        /// Gets or sets the ClientId for API identity (MISE authentication)
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the FPA ClientId for cross-tenant Omnichannel calls
        /// </summary>
        public string FPAClientId { get; set; }

        /// <summary>
        /// Gets or sets the Audience
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// Gets or sets the TenantId
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the managed identity client id
        /// </summary>
        public string ManagedIdentityClientId { get; set; }

        /// <summary>
        /// Gets or sets the Client cert name
        /// </summary>
        public string? ClientCertSubjectName { get; set; } = string.Empty;

        public string? Instance { get; set; }
        public string OAuthVersion { get; set; }
    }
}
