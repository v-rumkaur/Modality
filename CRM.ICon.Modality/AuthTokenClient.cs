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
                    ManagedIdentityClientId = managedIdentityClientId
                });

                var result = await credentials.GetTokenAsync(new TokenRequestContext(new[] { resource })).ConfigureAwait(false);
                return result.Token.ToString();
            }
            catch (Exception ex)
            {
                return ex.StackTrace;
            }
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