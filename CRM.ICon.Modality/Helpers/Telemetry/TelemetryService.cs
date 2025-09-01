using CRM.ICon.Modality.Helpers.Repositories;
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics.Metrics;

namespace CRM.ICon.Modality.Helpers.Telemetry
{
    public class TelemetryService : ITelemetryService
    {
        private readonly ITelemetryRepository telemetryRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryService"/> class.
        /// </summary>
        /// <param name="telemetryRepository">Application insights repository object</param>
        public TelemetryService(ITelemetryRepository telemetryRepository)
        {
            this.telemetryRepository = telemetryRepository ?? throw new ArgumentNullException(nameof(telemetryRepository));
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
            this.telemetryRepository.LogMetric(metricValue, metricName, counter, histogram, properties);
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
            this.telemetryRepository.LogTrace<T>(message, properties, severityLevel);
        }

        /// <summary>
        /// Logs custom errors
        /// </summary>
        /// <typeparam name="T">Logger instance category name</typeparam>
        /// <param name="message">Trace message</param>
        /// <param name="properties">Trace properties</param>
        public void LogError<T>(string message, IDictionary<string, string> properties)
        {
            this.telemetryRepository.LogError<T>(message, properties);
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
            this.telemetryRepository.LogException<T>(exception, properties, message);
        }

        /// <summary>
        /// Logs custom events
        /// </summary>
        /// <typeparam name="T">Logger instance category name</typeparam>
        /// <param name="message">Event message</param>
        /// <param name="properties">Event properties</param>
        public void LogCustomEvent<T>(string message, IDictionary<string, string> properties)
        {
            this.telemetryRepository.LogCustomEvent<T>(message, properties);
        }
    }
}
