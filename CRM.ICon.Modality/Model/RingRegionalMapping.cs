namespace CRM.ICon.Modality.Model
{
    public class RingRegionalMapping
    {
        /// <summary>
        /// Ring Regional data.
        /// </summary>
        public static readonly Dictionary<string, string> RingRegionalData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"Ring0","CAN" },
            {"Ring1", "CFR" },
            {"Ring2", "APAC" },
            {"Ring3", "EUR" },
            {"Ring4", "NAM" }
        };
    }
}

