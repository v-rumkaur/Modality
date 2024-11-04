using CRM.ICon.Modality.Helpers.HttpContext;
using CRM.ICon.Modality.Helpers.Telemetry;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace CRM.ICon.Modality.Helpers
{
    public static class ModalityExtensions
    {
        /// <summary>
        /// Gets base request properties
        /// </summary>
        /// <param name="callerMemberName">Caller method name</param>
        /// <returns>Base telemetry properties</returns>
        public static Dictionary<string, string> GetRequestProperties([CallerMemberName] string callerMemberName = "")
        {
            return new Dictionary<string, string>
            {
                { TelemetryConstants.RequestTime, DateTime.UtcNow.ToString() },
                { TelemetryConstants.CallerMethodName, callerMemberName }
            };
        }

        /// <summary>
        /// Adds context properties to custom properties
        /// </summary>
        /// <param name="properties">Custom properties</param>
        /// <returns>Dictionary with context properties</returns>
        public static IDictionary<string, string> AddContextProperties(this IDictionary<string, string> properties, IHttpContextHandler httpContextHandler)
        {
            properties ??= new Dictionary<string, string>();
            properties[TelemetryConstants.UserEmail] = properties.ContainsKey(TelemetryConstants.UserEmail) ? properties[TelemetryConstants.UserEmail] : httpContextHandler.UserEmail;
            properties[TelemetryConstants.UserObjectId] = properties.ContainsKey(TelemetryConstants.UserObjectId) ? properties[TelemetryConstants.UserObjectId] : httpContextHandler.UserObjectId;
            properties[TelemetryConstants.HttpCorrelationId] = properties.ContainsKey(TelemetryConstants.HttpCorrelationId) ? properties[TelemetryConstants.HttpCorrelationId] : httpContextHandler.CorrelationId;
            return properties;
        }

        /// <summary>
        /// Adds object to dictionary as string
        /// </summary>
        /// <param name="properties">Dictionary to add</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>Dictionary with added value</returns>
        public static Dictionary<string, string> AddObjectAsString<T>(this Dictionary<string, string> properties, string key, T value)
        {
            properties ??= new Dictionary<string, string>();
            string objectString = value == null ? string.Empty : typeof(T) == typeof(string) ? value.ToString() : JsonConvert.SerializeObject(value);
            properties[key] = objectString;
            return properties;
        }

        /// <summary>
        /// Gets the singleton telemetry provider instance
        /// </summary>
        /// <param name="logServiceToUse">Log service to use</param>
        /// <param name="telemetryProviders">Telemetry providers collection</param>
        /// <returns>Extraction command Instance</returns>
        public static ITelemetryProvider GetTelemetryProviderFromConfig(string logServiceToUse, IEnumerable<ITelemetryProvider> telemetryProviders)
        {
            return logServiceToUse switch

            {
                TelemetryConstants.ApplicationInsightsLogProvider => GetTelemetryProviderFromType(typeof(ApplicationInsightsLogProvider), telemetryProviders),
                _ => telemetryProviders.FirstOrDefault()

            };
        }

        /// <summary>
        /// Gets the telemetry provider instance based on type
        /// </summary>
        /// <param name="type">Telemetry provider type</param>
        /// <returns>Telemetry provider</returns>
        private static ITelemetryProvider GetTelemetryProviderFromType(Type type, IEnumerable<ITelemetryProvider> telemetryProviders)
        {
            return telemetryProviders.FirstOrDefault(x => x.GetType() == type);
        }


    }
}

