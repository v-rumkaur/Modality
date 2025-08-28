namespace CRM.ICon.Modality.Services.VDM
{
    public class VDMConfiguration
    {
        /// <summary>
        /// Gets or sets the OmnichannelServiceEndpoint
        /// </summary>
        public string? ServiceEndpoint { get; set; }
        /// <summary>
        /// Gets or sets the Resource
        /// </summary>
        public string? Resource { get; set; }
        /// <summary>
        /// Gets or sets the Tenant Id
        /// </summary>
        public string? TenantId { get; set; }
        /// <summary>
        /// Gets or sets the App Registration Id for VDM cross-tenant authentication
        /// </summary>
        public string? AppRegistrationId { get; set; }
    }
}
