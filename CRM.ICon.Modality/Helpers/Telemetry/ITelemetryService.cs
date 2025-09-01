using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics.Metrics;

namespace CRM.ICon.Modality.Helpers.Telemetry
{
    public interface ITelemetryService
    {
        /// <summary>
        /// Logs custom metrics
        /// </summary>
        /// <param name="metricValue">Metric value</param>
        /// <param name="metricName">Metric name</param>
        /// <param name="counter">Geneva counter object</param>
        /// <param name="histogram">Geneva histogram object</param>
        /// <param name="properties">Metric properties</param>
        void LogMetric(double metricValue, string metricName = null, Counter<double> counter = null, Histogram<double> histogram = null, IDictionary<string, object?> properties = null);

        /// <summary>
        /// Logs custom traces
        /// </summary>
        /// <typeparam name="T">Logger instance category name</typeparam>
        /// <param name="message">Trace message</param>
        /// <param name="properties">Trace properties</param>
        /// <param name="severityLevel">Trace severity level</param>
        void LogTrace<T>(string message, IDictionary<string, string> properties = null, SeverityLevel severityLevel = SeverityLevel.Information);

        /// <summary>
        /// Logs custom errors
        /// </summary>
        /// <typeparam name="T">Logger instance category name</typeparam>
        /// <param name="message">Trace message</param>
        /// <param name="properties">Trace properties</param>
        void LogError<T>(string message, IDictionary<string, string> properties);

        /// <summary>
        /// Logs exeptions
        /// </summary>
        /// <typeparam name="T">Logger instance category name</typeparam>
        /// <param name="exception">Exception object</param>
        /// <param name="properties">Exception properties</param>
        /// <param name="message">Error message</param>
        void LogException<T>(Exception exception, IDictionary<string, string> properties, string? message);

        /// <summary>
        /// Logs custom events
        /// </summary>
        /// <typeparam name="T">Logger instance category name</typeparam>
        /// <param name="message">Event message</param>
        /// <param name="properties">Event properties</param>
        void LogCustomEvent<T>(string message, IDictionary<string, string> properties);
    }
}
