using Azure.Core;
using Azure.Identity;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using System.Text.Json.Serialization;

namespace CRM.ICon.Modality
{
        public class AuthTokenClient
        {
            private readonly HttpClient httpClient;
            private readonly Dictionary<string, CachedAuthToken> cachedTokens = new();

            public AuthTokenClient(IHttpClientFactory httpClientFactory)
            {
                httpClient = httpClientFactory.CreateClient("default");
            }

            public async Task<string> GetToken(string clientId, string managedIdentityClientId, string resource, string tenantId)
            {
                return await RetrieveToken(clientId, managedIdentityClientId, resource, tenantId);
            }

            private async Task<string> RetrieveToken(string clientId, string managedIdentityClientId, string resource, string tenantId)
            {
            try
            {
                var credentials = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ManagedIdentityClientId = "15e4ae07-7153-4085-9333-ddd7b948ce5e"
                });

                var result = await credentials.GetTokenAsync(new TokenRequestContext(new[] { "e5eea49c-5b3a-4795-9253-9bab3780f255/.default" })).ConfigureAwait(false);
                return result.Token.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating token for VDM", ex);
            }
            /**ManagedIdentityClientAssertion managedIdentityClientAssertion = new ManagedIdentityClientAssertion("15e4ae07-7153-4085-9333-ddd7b948ce5e");

            var app = ConfidentialClientApplicationBuilder
                        .Create("caee4b01-a5c8-449f-ae78-ed7a3cdfcfe0")
                        .WithClientAssertion((AssertionRequestOptions options) => FetchExternalTokenAsync(managedIdentityClientAssertion))
                        .WithTenantId("975f013f-7f24-47e8-a7d3-abc4752bf346")
                        .Build();

            var result = await app.AcquireTokenForClient(new[] { "e5eea49c-5b3a-4795-9253-9bab3780f255/.default" }).ExecuteAsync();
            return result.AccessToken.ToString();**/
        }

        private async Task<string> FetchExternalTokenAsync(ManagedIdentityClientAssertion managedIdentityClientAssertion)
        {
            return await managedIdentityClientAssertion.GetSignedAssertionAsync(default);
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