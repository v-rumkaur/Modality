namespace CRM.ICon.Modality.Model
{
    public class LocaleRegionalMapping
    {
        /// <summary>
        /// Theme rule data.
        /// </summary>
        public static readonly Dictionary<string, string> LocaleRegionalData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"en-us","Americas" },
            {"en-gb", "EMEA" },
            {"en-au", "APAC" }
        };
    }
}
