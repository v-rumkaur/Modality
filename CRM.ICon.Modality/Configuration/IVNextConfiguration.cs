namespace CRM.ICon.Modality.Configuration
{
    public interface IVNextConfiguration
    {
        /// <summary>
        /// The scheme
        /// </summary>
        string Scheme { get; set; }

        /// <summary>
        /// The host
        /// </summary>
        string Host { get; set; }

        /// <summary>
        /// The test host when isTest flag is true
        /// </summary>
        string TestHost { get; set; }

        /// <summary>
        /// The default port
        /// </summary>
        int Port { get; set; }
    }
}
