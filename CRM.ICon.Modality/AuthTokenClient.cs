using Azure.Core;
using Azure.Identity;
using CRM.ICon.Modality.Helpers;
using CRM.ICon.Modality.Helpers.Identity;
using CRM.ICon.Modality.Helpers.KeyVaultClient;
using CRM.ICon.Modality.Helpers.Telemetry;
using Microsoft.Identity.Client;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text;

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
            logProperties["TENANT_DEBUG_RequestedTenantId"] = tenantId;
            logProperties["TENANT_DEBUG_RequestedResource"] = resource;
            logProperties["TENANT_DEBUG_ClientId"] = clientId;
            logProperties["TENANT_DEBUG_ManagedIdentityClientId"] = managedIdentityClientId;

            try
            {
                this.telemetryService.LogTrace<AuthTokenClient>("TENANT_DEBUG: Starting managed identity v2.0 token request", logProperties);

                var credentials = credentialProvider.GetCredential(managedIdentityClientId);
                // Create TokenRequestContext with tenant-specific parameters
                var tokenRequestContext = new TokenRequestContext(
                    scopes: [resource],
                    tenantId: tenantId  // Specify the target tenant
                );

                logProperties["TENANT_DEBUG_TokenRequestContextTenantId"] = tokenRequestContext.TenantId ?? "null";
                logProperties["TENANT_DEBUG_TokenRequestContextScopes"] = string.Join(",", tokenRequestContext.Scopes ?? []);
                logProperties["TENANT_DEBUG_Scope"] = resource;

                this.telemetryService.LogTrace<AuthTokenClient>("TENANT_DEBUG: About to call GetTokenAsync with v2.0 TokenRequestContext", logProperties);

                var result = await credentials.GetTokenAsync(new TokenRequestContext([resource]), default);
                // Decode JWT to analyze actual token details
                var tokenClaims = DecodeJwtClaims(result.Token);
                logProperties["TENANT_DEBUG_ActualTokenTenant"] = tokenClaims.GetValueOrDefault("tid", "not found");
                logProperties["TENANT_DEBUG_ActualTokenIssuer"] = tokenClaims.GetValueOrDefault("iss", "not found");
                logProperties["TENANT_DEBUG_ActualTokenAudience"] = tokenClaims.GetValueOrDefault("aud", "not found");
                logProperties["TENANT_DEBUG_TokenAppId"] = tokenClaims.GetValueOrDefault("appid", "not found");
                logProperties["TENANT_DEBUG_TokenVersion"] = tokenClaims.GetValueOrDefault("ver", "not found");
                logProperties["TENANT_DEBUG_TokenExpires"] = result.ExpiresOn.ToString();

                // Check if issuer indicates v1.0 vs v2.0 token
                var issuer = tokenClaims.GetValueOrDefault("iss", "");
                if (issuer.Contains("sts.windows.net"))
                {
                    logProperties["TENANT_DEBUG_TokenType"] = "v1.0 (sts.windows.net)";
                }
                else if (issuer.Contains("login.microsoftonline.com"))
                {
                    logProperties["TENANT_DEBUG_TokenType"] = "v2.0 (login.microsoftonline.com)";
                }
                else
                {
                    logProperties["TENANT_DEBUG_TokenType"] = $"Unknown issuer: {issuer}";
                }

                logProperties["TENANT_DEBUG_TokenReceived"] = "Success";
                this.telemetryService.LogTrace<AuthTokenClient>("TENANT_DEBUG: Token Success! analysis - REQUESTED vs ACTUAL tenant comparison", logProperties);
                return result.Token.ToString();
            }
            catch (Exception ex)
            {
                this.telemetryService.LogException<AuthTokenClient>(ex, logProperties, "Cross-tenant VDM token Generation Failed");
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
            logProperties["TENANT_DEBUG_CertMethod_ClientId"] = clientId;
            logProperties["TENANT_DEBUG_CertMethod_TenantId"] = tenantId;
            logProperties["TENANT_DEBUG_CertMethod_Scope"] = scope;

            try
            {
                this.telemetryService.LogTrace<AuthTokenClient>("Attempting to retrieve token using certificate", logProperties);

                X509Certificate2 cert = await keyVaultClient.GetCertificateAsync(certificateSubjectName);

                // Validate certificate
                logProperties["TENANT_DEBUG_CertSubject"] = cert.Subject;
                logProperties["TENANT_DEBUG_CertIssuer"] = cert.Issuer;
                logProperties["TENANT_DEBUG_CertThumbprint"] = cert.Thumbprint;
                logProperties["TENANT_DEBUG_CertSerialNumber"] = cert.SerialNumber;
                logProperties["TENANT_DEBUG_CertHasPrivateKey"] = cert.HasPrivateKey.ToString();
                logProperties["TENANT_DEBUG_CertNotBefore"] = cert.NotBefore.ToString();
                logProperties["TENANT_DEBUG_CertNotAfter"] = cert.NotAfter.ToString();
                logProperties["TENANT_DEBUG_CertExpired"] = (cert.NotAfter < DateTime.UtcNow).ToString();
                logProperties["TENANT_DEBUG_CertKeySize"] = cert.GetRSAPublicKey()?.KeySize.ToString() ?? "Unknown";
                logProperties["TENANT_DEBUG_CertSignatureAlgorithm"] = cert.SignatureAlgorithm.FriendlyName ?? "Unknown";
                logProperties["TENANT_DEBUG_CertVersion"] = cert.Version.ToString();

                this.telemetryService.LogTrace<AuthTokenClient>("TENANT_DEBUG: Certificate validation details", logProperties);

                // Log MSAL configuration details
                logProperties["TENANT_DEBUG_MSALClientId"] = clientId;
                logProperties["TENANT_DEBUG_MSALTenantId"] = tenantId;
                logProperties["TENANT_DEBUG_MSALScope"] = scope + "/.default";
                logProperties["TENANT_DEBUG_MSALAuthority"] = $"https://login.microsoftonline.com/{tenantId}/";
                logProperties["TENANT_DEBUG_SendX5C"] = "true";

                this.telemetryService.LogTrace<AuthTokenClient>("TENANT_DEBUG: MSAL configuration details", logProperties);

                var app = ConfidentialClientApplicationBuilder
                            .Create(clientId)
                            .WithCertificate(cert)
                            .Build();

                // Log just before token request
                logProperties["TENANT_DEBUG_AboutToCallMSAL"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                this.telemetryService.LogTrace<AuthTokenClient>("TENANT_DEBUG: About to call MSAL AcquireTokenForClient", logProperties);

                AuthenticationResult result;
                try
                {
                    result = await app.AcquireTokenForClient(new[] { scope + "/.default" }).WithSendX5C(true).ExecuteAsync();
                    telemetryService.LogTrace<AuthTokenClient>("TENANT_DEBUG: MSAL token acquisition successful", logProperties);
                }
                catch (MsalServiceException msalEx)
                {
                    // Capture detailed MSAL service exception information
                    logProperties["TENANT_DEBUG_MSALError"] = "MsalServiceException";
                    logProperties["TENANT_DEBUG_MSALErrorCode"] = msalEx.ErrorCode ?? "Unknown";
                    logProperties["TENANT_DEBUG_MSALStatusCode"] = msalEx.StatusCode.ToString();
                    logProperties["TENANT_DEBUG_MSALResponseBody"] = msalEx.ResponseBody ?? "No response body";
                    logProperties["TENANT_DEBUG_MSALCorrelationId"] = msalEx.CorrelationId ?? "No correlation ID";
                    logProperties["TENANT_DEBUG_MSALClaims"] = msalEx.Claims ?? "No claims";
                    logProperties["TENANT_DEBUG_MSALMessage"] = msalEx.Message ?? "No message";

                    this.telemetryService.LogException<AuthTokenClient>(msalEx, logProperties, "TENANT_DEBUG: MSAL Service Exception with detailed Azure AD error");
                    throw;
                }
                catch (MsalClientException clientEx)
                {
                    // Capture MSAL client exception information
                    logProperties["TENANT_DEBUG_MSALError"] = "MsalClientException";
                    logProperties["TENANT_DEBUG_MSALErrorCode"] = clientEx.ErrorCode ?? "Unknown";
                    logProperties["TENANT_DEBUG_MSALMessage"] = clientEx.Message ?? "No message";
                    logProperties["TENANT_DEBUG_MSALCorrelationId"] = clientEx.CorrelationId ?? "No correlation ID";

                    this.telemetryService.LogException<AuthTokenClient>(clientEx, logProperties, "TENANT_DEBUG: MSAL Client Exception");
                    throw;
                }
                catch (Exception genericEx)
                {
                    // Capture any other exceptions
                    logProperties["TENANT_DEBUG_MSALError"] = "GenericException";
                    logProperties["TENANT_DEBUG_MSALMessage"] = genericEx.Message ?? "No message";
                    logProperties["TENANT_DEBUG_MSALStackTrace"] = genericEx.StackTrace ?? "No stack trace";

                    this.telemetryService.LogException<AuthTokenClient>(genericEx, logProperties, "TENANT_DEBUG: Generic exception during MSAL token acquisition");
                    throw;
                }

                // Decode and analyze the certificate-based token
                var tokenClaims = DecodeJwtClaims(result.AccessToken);
                logProperties["TENANT_DEBUG_CertToken_Tenant"] = tokenClaims.GetValueOrDefault("tid", "not found");
                logProperties["TENANT_DEBUG_CertToken_Issuer"] = tokenClaims.GetValueOrDefault("iss", "not found");
                logProperties["TENANT_DEBUG_CertToken_Audience"] = tokenClaims.GetValueOrDefault("aud", "not found");
                logProperties["TENANT_DEBUG_CertToken_AppId"] = tokenClaims.GetValueOrDefault("appid", "not found");
                logProperties["TENANT_DEBUG_CertToken_Version"] = tokenClaims.GetValueOrDefault("ver", "not found");

                // Check token version based on issuer
                var issuer = tokenClaims.GetValueOrDefault("iss", "");
                if (issuer.Contains("sts.windows.net"))
                {
                    logProperties["TENANT_DEBUG_CertTokenType"] = "v1.0 (sts.windows.net)";
                }
                else if (issuer.Contains("login.microsoftonline.com"))
                {
                    logProperties["TENANT_DEBUG_CertTokenType"] = "v2.0 (login.microsoftonline.com)";
                }
                else
                {
                    logProperties["TENANT_DEBUG_CertTokenType"] = $"Unknown issuer: {issuer}";
                }

                this.telemetryService.LogTrace<AuthTokenClient>("TENANT_DEBUG: Successfully retrieved certificate token with analysis", logProperties);
                return result.AccessToken.ToString();
            }
            catch (Exception ex)
            {
                this.telemetryService.LogException<AuthTokenClient>(ex, logProperties, "Certificate token Generation Failed");
                throw;
            }
        }

        private Dictionary<string, string> DecodeJwtClaims(string token)
        {
            var claims = new Dictionary<string, string>();
            try
            {
                var parts = token.Split('.');
                if (parts.Length >= 2)
                {
                    var payload = parts[1];
                    // Add padding if needed
                    switch (payload.Length % 4)
                    {
                        case 2: payload += "=="; break;
                        case 3: payload += "="; break;
                    }

                    var jsonBytes = Convert.FromBase64String(payload);
                    var json = Encoding.UTF8.GetString(jsonBytes);
                    var document = JsonDocument.Parse(json);

                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        claims[property.Name] = property.Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                this.telemetryService.LogTrace<AuthTokenClient>($"TENANT_DEBUG: Failed to decode JWT: {ex.Message}", ModalityExtensions.GetRequestProperties());
            }
            return claims;
        }

        // In AuthTokenClient.cs - add this method
        public async Task<string> GetVDMTokenAsync(string vdmTenantId, string appRegistrationId, string managedIdentityId, string resource)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["VDM_AUTH_METHOD"] = "ClientAssertionCredential";
            logProperties["VDM_TARGET_TENANT"] = vdmTenantId;
            logProperties["VDM_RESOURCE"] = resource;

            try
            {
                this.telemetryService.LogTrace<AuthTokenClient>("VDM: Starting ClientAssertionCredential authentication", logProperties);

                // Log all parameters being passed for debugging
                this.telemetryService.LogTrace<AuthTokenClient>($"VDM DEBUG - Calling VDMCredentialHelper with TenantId: {vdmTenantId}, AppRegId: {appRegistrationId}, ManagedIdentityId: {managedIdentityId}, Resource: {resource}/.default", logProperties);

                var token = await VDMCredentialHelper.GetVDMAccessTokenAsync(
                    telemetryService,
                    vdmTenantId,
                    appRegistrationId,
                    managedIdentityId,
                    $"{resource}/.default"
                );

                logProperties["VDM_TOKEN_SUCCESS"] = "true";
                logProperties["VDM_FINAL_TOKEN"] = token; // Log the final token received
                this.telemetryService.LogTrace<AuthTokenClient>("VDM: ClientAssertionCredential authentication successful", logProperties);
                this.telemetryService.LogTrace<AuthTokenClient>($"VDM DEBUG - Final token received: {token}", logProperties);

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