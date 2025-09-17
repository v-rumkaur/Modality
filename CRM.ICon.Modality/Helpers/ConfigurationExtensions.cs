namespace CRM.ICon.Modality.Helpers
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Gets a string value from a section and key.
        /// </summary>
        public static string GetStringValue(this IConfiguration config, string sectionName, string key)
        {
            // Try getting from section, fallback to root key
            return config.GetSection(sectionName)?[key] ?? config[key];
        }
        /// <summary>
        /// Gets an int value from a section and key.
        /// </summary>
        public static int GetInt32Value(this IConfiguration config, string sectionName, string key)
        {
            var value = config.GetSection(sectionName)?[key] ?? config[key];
            return int.TryParse(value, out var result) ? result : 0; // or throw if preferred
        }
    }
}
