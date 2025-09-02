using Microsoft.Extensions.Configuration;

namespace CRM.ICon.Modality.Configuration
{
    public static class ConfigurationExtensions
    {
        public static string GetStringValue(this IConfiguration configuration, string section, string key)
            => configuration.GetSection(section)?[key] ?? string.Empty;

        public static int GetInt32Value(this IConfiguration configuration, string section, string key)
            => int.TryParse(configuration.GetSection(section)?[key], out var value) ? value : 0;
    }
}
