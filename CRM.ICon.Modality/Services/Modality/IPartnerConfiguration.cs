namespace CRM.ICon.Modality.Services.Modality
{
    public interface IPartnerConfiguration
    {
        string Scheme { get; set; }

        /// <summary>
        /// The default host
        /// </summary>
        string Host { get; set; }

        /// <summary>
        /// The default port
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// Get a Uri representation of the partner config
        /// </summary>
        /// <returns></returns>
        Uri GetUri();
    }
}
