using Azure.Core;
using Azure.Identity;
using CRM.ICon.Modality.Helpers.KeyVaultClient;
using CRM.ICon.Modality.Helpers;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using CRM.ICon.Modality.Controllers;
using Microsoft.Identity.ServiceEssentials;
using CRM.ICon.Modality.Helpers.Telemetry;

namespace CRM.ICon.Modality
{
    public class AuthTokenClient
    {
        private readonly HttpClient httpClient;
        private readonly Dictionary<string, CachedAuthToken> cachedTokens = new();
        private readonly IKeyVaultClient keyVaultClient;
        private readonly ITelemetryService telemetryService;
        public AuthTokenClient(IHttpClientFactory httpClientFactory, IKeyVaultClient keyVaultClient, ITelemetryService telemetryService)
        {
            httpClient = httpClientFactory.CreateClient("default");
            this.keyVaultClient = keyVaultClient ?? throw new ArgumentNullException(nameof(keyVaultClient));
            this.telemetryService = telemetryService;
        }

        public async Task<string> GetToken(string clientId, string managedIdentityClientId, string resource, string tenantId)
        {
            return await RetrieveToken(clientId, managedIdentityClientId, resource, tenantId);
        }

        public async Task<string> GetTokenWithCertAsync(string clientId, string certificateSubjectName, string resource, string tenantId)
        {
            return await RetrieveTokenWithCertAsync(clientId, certificateSubjectName, resource, tenantId);
        }

        private async Task<string> RetrieveToken(string clientId, string managedIdentityClientId, string resource, string tenantId)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            try
            {
                
                var credentials = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ManagedIdentityClientId = managedIdentityClientId
                });
                
                var result = await credentials.GetTokenAsync(new TokenRequestContext(new[] { resource }));
                return result.Token.ToString();
            }
            catch (Exception ex)
            {
                this.telemetryService.LogException<AuthTokenClient>(ex, logProperties, "VDM token Generation Failed");
                throw;
            }
        }

        /// <summary>
        /// Retrives Generated Token
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="certificateSubjectName"></param>
        /// <param name="scope"></param>
        /// <param name="tenantId"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task<string> RetrieveTokenWithCertAsync(string clientId, string certificateSubjectName, string scope, string tenantId)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            try
            {
                X509Certificate2 cert = await keyVaultClient.GetCertificateAsync(certificateSubjectName);
                var app = ConfidentialClientApplicationBuilder
                            .Create(clientId)
                            .WithTenantId(tenantId)
                            .WithAzureRegion()
                            .WithCertificate(cert)
                            .Build();

                var result = await app.AcquireTokenForClient(new[] { scope + "/.default" }).WithSendX5C(true).ExecuteAsync();
                return result.AccessToken.ToString();
            }
            catch (Exception ex)
            {
                this.telemetryService.LogException<AuthTokenClient>(ex, logProperties, "Omnichannel token Generation Failed");
                throw;
            }
        }
        class CachedAuthToken
        {
            public string Token { get; set; }
            public DateTime ExpiresAt { get; set; }
        }

        class TokenResponse
        {
            /// <summary>
            /// Gets or sets the access token.
            /// </summary>
            public string AccessToken { get; set; }

            /// <summary>
            /// Gets or sets the expiration time.
            /// </summary>
            public int ExpiresIn { get; set; }
        }
    }
}