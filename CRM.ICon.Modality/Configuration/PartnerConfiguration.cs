namespace CRM.ICon.Modality.Configuration
{
    using System;
    using CRM.ICon.Modality.Configuration;
    public class PartnerConfiguration : IPartnerConfiguration
    {
        // Partner config
        private const string PartnerConfig = "PartnerConfig";
        private const string DefaultScheme = "DefaultScheme";
        private const string DefaultHost = "DefaultHost";
        private const string DefaultPort = "DefaultPort";

        /// <summary>
        /// The scheme
        /// </summary>
        public string Scheme { get; set; }

        /// <summary>
        /// The host
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// The PartnerConfiguration, which contains the url to use in
        /// construction of modality links
        /// </summary>
        /// <param name="scheme"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public PartnerConfiguration(
            string scheme,
            string host,
            int port)
        {
            this.Scheme = scheme;
            this.Host = host;
            this.Port = port;
        }

        /// <summary>
        /// The PartnerConfiguration, which contains the url to use in
        /// construction of modality links
        /// </summary>
        /// <param name="configuration"></param>
        public PartnerConfiguration(IConfiguration configuration)
            : this(configuration.GetStringValue(PartnerConfig, DefaultScheme),
                  configuration.GetStringValue(PartnerConfig, DefaultHost),
                  configuration.GetInt32Value(PartnerConfig, DefaultPort))
        {
        }

        /// <summary>
        /// Constructs and returns a URI based on the members of this PartnerConfiguration instance
        /// </summary>
        /// <returns></returns>
        public Uri GetUri()
        {
            return new UriBuilder(this.Scheme, this.Host, this.Port).Uri;
        }
    }
}