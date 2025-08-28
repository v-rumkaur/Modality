using Azure.Core;
using CRM.ICon.Modality.Helpers;
using CRM.ICon.Modality.Helpers.Identity;
using CRM.ICon.Modality.Helpers.KeyVaultClient;
using CRM.ICon.Modality.Helpers.Telemetry;
using Microsoft.Identity.Client;
using System.Security.Cryptography.X509Certificates;

namespace CRM.ICon.Modality
{
    public class AuthTokenClient
    {
        private readonly HttpClient httpClient;
        private readonly Dictionary<string, CachedAuthToken> cachedTokens = new();
        private readonly IKeyVaultClient keyVaultClient;
        private readonly ITelemetryService telemetryService;
        private readonly ICredentialProvider credentialProvider;
        public AuthTokenClient(IHttpClientFactory httpClientFactory, IKeyVaultClient keyVaultClient, ITelemetryService telemetryService, ICredentialProvider credentialProvider)
        {
            httpClient = httpClientFactory.CreateClient("default");
            this.keyVaultClient = keyVaultClient ?? throw new ArgumentNullException(nameof(keyVaultClient));
            this.telemetryService = telemetryService;
            this.credentialProvider = credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));
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
            logProperties["RequestedTenantId"] = tenantId;
            logProperties["RequestedResource"] = resource;
            logProperties["ClientId"] = clientId;
            logProperties["ManagedIdentityClientId"] = managedIdentityClientId;

            try
            {
                this.telemetryService.LogTrace<AuthTokenClient>("Starting managed identity token request", logProperties);

                var credentials = credentialProvider.GetCredential(managedIdentityClientId);
                var result = await credentials.GetTokenAsync(new TokenRequestContext([resource]), default);
                
                return result.Token.ToString();
            }
            catch (Exception ex)
            {
                this.telemetryService.LogException<AuthTokenClient>(ex, logProperties, "token Generation Failed");
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
            logProperties["CertMethod_ClientId"] = clientId;
            logProperties["CertMethod_TenantId"] = tenantId;
            logProperties["CertMethod_Scope"] = scope;

            try
            {
                this.telemetryService.LogTrace<AuthTokenClient>("Attempting to retrieve token using certificate", logProperties);
                
                X509Certificate2 cert = await keyVaultClient.GetCertificateAsync(certificateSubjectName);
                
                var app = ConfidentialClientApplicationBuilder
                            .Create(clientId)
                            .WithTenantId(tenantId)
                            .WithAzureRegion()
                            .WithCertificate(cert)
                            .Build();

                var result = await app.AcquireTokenForClient(new[] { scope + "/.default" }).WithSendX5C(true).ExecuteAsync();
                
                this.telemetryService.LogTrace<AuthTokenClient>("Successfully retrieved token using certificate", logProperties);
                return result.AccessToken.ToString();
            }
            catch (Exception ex)
            {
                this.telemetryService.LogException<AuthTokenClient>(ex, logProperties, "Certificate token Generation Failed");
                throw;
            }
        }

        public async Task<string> GetVDMTokenAsync(string vdmTenantId, string appRegistrationId, string managedIdentityId, string resource)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["VDM_TARGET_TENANT"] = vdmTenantId;
            logProperties["VDM_RESOURCE"] = resource;

            try
            {
                this.telemetryService.LogTrace<AuthTokenClient>("VDM: Starting ClientAssertionCredential authentication", logProperties);

                var token = await VDMCredentialHelper.GetVDMAccessTokenAsync(
                    telemetryService,
                    vdmTenantId,
                    appRegistrationId,
                    managedIdentityId,
                    $"{resource}/.default"
                );
                return token;
            }
            catch (Exception ex)
            {
                this.telemetryService.LogException<AuthTokenClient>(ex, logProperties, "VDM ClientAssertionCredential authentication failed");
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