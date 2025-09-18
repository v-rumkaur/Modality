using CRM.ICon.Modality.Model;

namespace CRM.ICon.Modality.Helpers.Repositories
{
    public interface ISupportRepository
    {
        /// <summary>
        /// Get support modalities
        /// </summary>
        /// <param name="product">
        /// The product
        /// </param>
        /// <param name="issue">
        /// The issue
        /// </param>
        /// <param name="partnerId">
        /// The partner id
        /// </param>
        /// <param name="platform">
        /// The platform
        /// </param>
        /// <param name="preview">
        /// Request preview resources
        /// </param>
        /// <param name="mode">
        /// The mode (eg "live")
        /// </param>
        /// <param name="disability">
        /// Request disability resources
        /// </param>
        /// <param name="locale">
        /// The language-country locale pair (eg en-us)
        /// </param>
        /// <param name="host">
        /// The assisted support environment, used for assisted support links</param>
        /// <param name="isVNext">
        /// If is VNext request</param>
        /// <param name="isTest">
        /// If is a test request</param>
        /// <returns>
        /// A collection of support modalities
        /// </returns>
        Task<IEnumerable<Dictionary<string, string>>> GetModalities(
            string product,
            string issue,
            string partnerId,
            string platform,
            bool preview,
            string mode,
            bool disability,
            string locale,
            string host,
            bool isVNext,
            bool isTest);

        /// <summary>
        /// Get locale
        /// </summary>
        /// <param name="locale">the locale</param>
        /// <returns>
        /// The fallback locale, if it exists, else the original
        /// </returns>
        string GetFallbackLocale(string locale);

        /// <summary>
        /// Get locale
        /// </summary>
        /// <param name="locale">the locale</param>
        /// <returns>
        /// The fallback locale, if it exists, else the original
        /// </returns>
        Task<CircuitBreaker> UpdateCircuitBreaker(CircuitBreaker circuitBreaker);
    }
}
