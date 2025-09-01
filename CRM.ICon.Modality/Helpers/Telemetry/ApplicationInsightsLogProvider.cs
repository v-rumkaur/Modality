using Azure.Identity;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;
using CRM.ICon.Modality.Helpers.HttpContext;
using CRM.ICon.Modality.Helpers.Identity;

namespace CRM.ICon.Modality.Helpers.Telemetry
{
    public class ApplicationInsightsLogProvider : ITelemetryProvider
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IHttpContextHandler httpContextHandler;
        private readonly Configuration.TelemetryConfiguration telemetryConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsLogProvider"/> class.
        /// </summary>
        /// <param name="telemetryConfiguration">Telemetry configuration</param>
        /// <param name="httpContextHandler">Http context handler object</param>
        public ApplicationInsightsLogProvider(IOptions<Configuration.TelemetryConfiguration> telemetryConfiguration, IHttpContextHandler httpContextHandler, ICredentialProvider credentialProvider)
        {
            this.telemetryConfiguration = telemetryConfiguration?.Value ?? throw new ArgumentNullException(nameof(telemetryConfiguration));
            this.httpContextHandler = httpContextHandler ?? throw new ArgumentNullException(nameof(httpContextHandler));

            // Use default App Service managed identity authentication to avoid credential conflicts
            Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration applicationInsightsConfiguration = Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.CreateDefault();
            applicationInsightsConfiguration.ConnectionString = this.telemetryConfiguration.ApplicationInsightsConnectionString;
            
            this.telemetryClient = new TelemetryClient(applicationInsightsConfiguration);
        }

        /// <summary>
        /// Logs custom metrics
        /// </summary>
        /// <param name="metricValue">Metric value</param>
        /// <param name="metricName">Metric name</param>
        /// <param name="counter">Geneva counter object</param>
        /// <param name="histogram">Geneva histogram object</param>
        /// <param name="properties">Metric properties</param>
        public void LogMetric(double metricValue, string metricName = null, Counter<double> counter = null, Histogram<double> histogram = null, IDictionary<string, object?> properties = null)
        {
            this.telemetryClient.GetMetric(metricName).TrackValue(metricValue);
        }

        /// <summary>
        /// Logs custom traces
        /// </summary>
        /// <typeparam name="T">Logger instance category name</typeparam>
        /// <param name="message">Trace message</param>
        /// <param name="properties">Trace properties</param>
        /// <param name="severityLevel">Trace severity level</param>
        public void LogTrace<T>(string message, IDictionary<string, string> properties, SeverityLevel severityLevel = SeverityLevel.Information)
        {
            this.telemetryClient.TrackTrace(message, severityLevel, properties.AddContextProperties(this.httpContextHandler));
        }

        /// <summary>
        /// Logs custom errors
        /// </summary>
        /// <typeparam name="T">Logger instance category name</typeparam>
        /// <param name="message">Trace message</param>
        /// <param name="properties">Trace properties</param>
        public void LogError<T>(string message, IDictionary<string, string> properties)
        {
            this.telemetryClient.TrackTrace(message, SeverityLevel.Error, properties.AddContextProperties(this.httpContextHandler));
        }

        /// <summary>
        /// Logs exeptions
        /// </summary>
        /// <typeparam name="T">Logger instance category name</typeparam>
        /// <param name="exception">Exception object</param>
        /// <param name="properties">Exception properties</param>
        /// <param name="message">Error message</param>
        public void LogException<T>(Exception exception, IDictionary<string, string> properties, string? message)
        {
            this.telemetryClient.TrackException(exception, properties.AddContextProperties(this.httpContextHandler));
        }

        /// <summary>
        /// Logs custom events
        /// </summary>
        /// <typeparam name="T">Logger instance category name</typeparam>
        /// <param name="message">Event message</param>
        /// <param name="properties">Event properties</param>
        public void LogCustomEvent<T>(string message, IDictionary<string, string> properties)
        {
            this.telemetryClient.TrackEvent(message, properties.AddContextProperties(this.httpContextHandler));
        }
    }
}

