using CRM.ICon.Modality.Helpers;

namespace CRM.ICon.Modality.Services.Modality
{
    public class VNextConfiguration : IVNextConfiguration
    {
        // Partner config
        private const string VNextConfigSection = "VNextConfig";
        private const string SchemeKey = "Scheme";
        private const string HostKey = "Host";
        private const string TestHostKey = "TestHost";
        private const string PortKey = "Port";

        /// <summary>
        /// The scheme
        /// </summary>
        public string Scheme { get; set; }

        /// <summary>
        /// The host
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The test host when isTest flag is true
        /// </summary>
        public string TestHost { get; set; }

        /// <summary>
        /// The port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VNextConfiguration"/> class. The VNextConfiguration, which contains the url to use in construction of modality links.
        /// </summary>
        /// <param name="scheme">The scheme</param>
        /// <param name="host">The host</param>
        /// <param name="testHost">The test host when isTest flag is true</param>
        /// <param name="port">The port</param>
        public VNextConfiguration(
            string scheme,
            string host,
            string testHost,
            int port)
        {
            this.Scheme = scheme;
            this.Host = host;
            this.TestHost = testHost;
            this.Port = port;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VNextConfiguration"/> class. The VNextConfiguration, which contains the url to use in construction of modality links
        /// </summary>
        /// <param name="configuration">The configuration object</param>
        public VNextConfiguration(IConfiguration configuration)
            : this(
                configuration.GetStringValue(VNextConfigSection, SchemeKey),
                configuration.GetStringValue(VNextConfigSection, HostKey),
                configuration.GetStringValue(VNextConfigSection, TestHostKey),
                configuration.GetInt32Value(VNextConfigSection, PortKey))
        {
        }
    }
}