using CRM.ICon.Modality.Model;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CRM.ICon.Modality.Services.Modality
{
    public interface IModalitiesService
    {
        /// <summary>
        /// Get Support Channels
        /// </summary>
        /// <param name="id">The subject Id</param>
        /// <param name="issue">The issue</param>
        /// <param name="partnerId">The partner id</param>
        /// <param name="platform">The platform, eg "web", "ios"</param>
        /// <param name="language">The language, eg "en"</param>
        /// <param name="country">The country, eg "us"</param>
        /// <param name="mode">The mode, eg "live"</param>
        /// <param name="preview">Whether to use draft resources</param>
        /// <param name="disability">Whether to use disability resources</param>
        /// <param name="fallback">Whether to use locale fallback if given locale (language, country) is not supported</param>
        /// <param name="host">The hostname</param>
        /// <param name="isVNext">If is VNext request</param>
        /// <param name="isTest">If is a test request</param>
        /// <returns>A ModalityResult object</returns>
        Task<ModalityResult> GetModalities(string id, string issue, string partnerId, string platform, string language, string country, string mode, bool preview, bool disability, bool fallback, string host, bool isVNext, bool isTest);

        Task<CircuitBreaker> UpdateCircuitBreakerForModalities(CircuitBreaker circuitBreaker, HttpRequestHeaders httpRequestHeaders);
    }
}