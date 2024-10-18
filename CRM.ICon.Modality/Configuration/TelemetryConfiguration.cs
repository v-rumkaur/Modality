namespace CRM.ICon.Modality.Configuration
{
    public class TelemetryConfiguration
    {
        /// <summary>
        /// Gets or sets the log provider to use
        /// </summary>
        public string LogProviderToUse { get; set; }

        /// <summary>
        /// Gets or sets the application insights connection string
        /// </summary>
        public string ApplicationInsightsConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the geneva metrics connection string
        /// </summary>
        public string GenevaMetricsConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the geneva logs connection string
        /// </summary>
        public string GenevaLogsConnectionString { get; set; }
    }
}
