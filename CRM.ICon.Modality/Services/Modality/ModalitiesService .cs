using CRM.ICon.Modality.Helpers.Logging;
using CRM.ICon.Modality.Helpers.Repositories;
using CRM.ICon.Modality.Model.Modalities;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Http.Headers;
using System.Runtime.ExceptionServices;

namespace CRM.ICon.Modality.Services.Modality
{
    public class ModalitiesService : IModalitiesService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModalitiesService"/> class.
        /// </summary>
        /// <param name="supportRepository">The support repository.</param>
        /// <param name="sllLogger">The SLL logger.</param>
        public ModalitiesService(
            ISupportRepository supportRepository,
            ISllLogger sllLogger)
        {
            this.SupportRepository = supportRepository;
            this.SllLogger = sllLogger;
        }

        /// <summary>
        /// Gets the log writer.
        /// </summary>
        protected ISllLogger SllLogger { get; }

        /// <summary>
        /// Gets the support repository.
        /// </summary>
        protected ISupportRepository SupportRepository { get; }

        /// <summary>Get the set of modalities which correspond to the given params</summary>
        /// <param name="id">The product id.</param>
        /// <param name="issue">The issue.</param>
        /// <param name="partnerId">The partner id.</param>
        /// <param name="platform">The platform.</param>
        /// <param name="language">The language.</param>
        /// <param name="country">The country</param>
        /// <param name="mode">The mode</param>
        /// <param name="preview">The preview.</param>
        /// <param name="disability">The accessibility.</param>
        /// <param name="fallback">fallback</param>
        /// <param name="host">The assisted support environment</param>
        /// <param name="isVNext">If is VNext request</param>
        /// <param name="isTest">If is a test request</param>
        /// <returns>A support channel result</returns>
        public async Task<ModalityResult> GetModalities(string id, string issue, string partnerId, string platform, string language, string country, string mode, bool preview, bool disability, bool fallback, string host, bool isVNext, bool isTest)
        {
            var locale = LocaleFromLanguageAndCountry(language, country);
            if (fallback)
            {
                locale = this.SupportRepository.GetFallbackLocale(locale);
            }

            try
            {
                var modalities =
                    await
                        this.SupportRepository.GetModalities(
                            id,
                            issue,
                            partnerId,
                            platform,
                            preview,
                            mode,
                            disability,
                            locale,
                            host,
                            isVNext,
                            isTest);
                return new ModalityResult(modalities);
            }
            catch (Exception ex)
            {
                this.SllLogger.WriteErrorTelemetry(
                    "modalitiesService.Services.GetModalities",
                    $"The following exception was thrown while trying to get support channels from the repository: {ex}");
                ExceptionDispatchInfo.Capture(ex).Throw();
                return null; // Keep compiler happy
            }
        }

        /// <summary>Updates the circuitbreaker modalities for given request body</summary>
        /// <param name="circuitBreaker">The request body</param>
        /// <param name="headers">The header params.</param>
        /// <returns>A support channel result</returns>
        public async Task<CircuitBreaker> UpdateCircuitBreakerForModalities(CircuitBreaker circuitBreaker, HttpRequestHeaders headers)
        {
            try
            {
                var token = AadBearerTokenAuth.GetAccessToken(GetHeaderValue(headers, "SecretKey"));
                if (token != null && token.Result != null)
                {
                    var result = await this.SupportRepository.UpdateCircuitBreaker(circuitBreaker);
                    return result;
                }
            }
            catch (Exception ex)
            {
                this.SllLogger.WriteErrorTelemetry(
                    "modalitiesService.Services.UpdateCircuitBreakerModalities",
                    $"The following exception was thrown while trying to update Circuit Breaker Details: {ex}");
                return null;
            }

            return null;
        }

        private static string LocaleFromLanguageAndCountry(string language, string country)
        {
            return $"{language}-{country}";
        }

        private static string GetHeaderValue(HttpHeaders headers, string headerName)
        {
            if (string.IsNullOrWhiteSpace(headerName))
            {
                return null;
            }

            IEnumerable<string> values;
            return headers.TryGetValues(headerName, out values) ? values.FirstOrDefault() : "";
        }
    }
}

