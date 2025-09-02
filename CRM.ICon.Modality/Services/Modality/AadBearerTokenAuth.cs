using Azure.Core;
using Azure.Identity;
using CRM.ICon.Modality.Helpers.Logging;
using Microsoft.Identity.Client;
using System.Security.Cryptography.X509Certificates;

namespace CRM.ICon.Modality.Services.Modality
{
    public class AadBearerTokenAuth
    {
        static public async Task<AuthenticationResult> GetTokenAsync(string authority, string appIdUri, string clientId, string clientCert, string aadRegion = null)
        {
            return await GetToken(authority, appIdUri, clientId, clientCert, aadRegion);
        }

        static async Task<AuthenticationResult> GetToken(string authority, string appIdUri, string clientId, string clientCert, string aadRegion)
        {
            try
            {
                var aadAppCert = CertificateHelper.LookupCertificate(X509FindType.FindBySubjectName, clientCert, StoreName.My, StoreLocation.LocalMachine);
                string[] scopes = { appIdUri + "/.default" };

                // aadRegion should be null in case of Common authority URI
                if (!string.IsNullOrEmpty(aadRegion))
                {
                    IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(clientId).WithAzureRegion(aadRegion).WithAuthority(authority).WithCertificate(aadAppCert).Build();
                    var authenticationResult = await app.AcquireTokenForClient(scopes)
                            .WithAuthority(authority).WithSendX5C(true).ExecuteAsync();
                    return authenticationResult;
                }
                else
                {
                    IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(clientId).WithAuthority(authority).WithCertificate(aadAppCert).Build();
                    var authenticationResult = await app.AcquireTokenForClient(scopes)
                            .WithAuthority(authority).WithSendX5C(true).ExecuteAsync();
                    return authenticationResult;
                }
            }
            catch (MsalException msalException)
            {
                throw msalException;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public static TokenCredential GetTokenCredential(string certificate, string tenant, string app, ISllLogger sllLogger)
        {
            TokenCredential tokenCredential;
            try
            {
                var aadAppCert = CertificateHelper.LookupCertificate(X509FindType.FindBySubjectName, certificate, StoreName.My, StoreLocation.LocalMachine);
                tokenCredential = new ClientCertificateCredential(
                    tenant,
                    app,
                    aadAppCert,
                            new ClientCertificateCredentialOptions { SendCertificateChain = true });

            }
            catch (Exception exception)
            {
                sllLogger.WriteInformationalTelemetry("SupportRepository.GetCircuitBreakerFlag", "Token creation failed with exception: {0}", exception);
                return null;
            }
            return tokenCredential;
        }

        /// <summary>
        /// Generate access token using secret key
        /// </summary>
        /// <param name="secretKey">The secret key</param>
        /// <returns>The AAD access token</returns>
        public static async Task<string> GetAccessToken(string secretKey)
        {
            try
            {
                // The below values will be same in all environments therefore not moving to config file
                string[] scopes = { "https://vault.azure.net/.default" };
                string authority = "https://login.windows.net/mspmecloud.onmicrosoft.com";
                string appId = "e9d599ec-6be3-4572-88ac-d7145561bd88";

                IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(appId)
                    .WithClientId(appId)
                    .WithAuthority(authority)
                    .WithClientSecret(secretKey)
                    .Build();
                try
                {
                    var authenticationResult = await app.AcquireTokenForClient(scopes)
                        .WithAuthority(authority).ExecuteAsync();
                    return authenticationResult.AccessToken;
                }
                catch (MsalException msalException)
                {
                    throw msalException;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}

