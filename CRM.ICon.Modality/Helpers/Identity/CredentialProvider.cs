using Azure.Core;
using Azure.Identity;
using System.Collections.Concurrent;

namespace CRM.ICon.Modality.Helpers.Identity
{
    public class CredentialProvider(IWebHostEnvironment environment) : ICredentialProvider
    {
        private readonly ConcurrentDictionary<string, TokenCredential> _clientCredentials = new();
        private readonly TokenCredential _defaultCredential = environment.IsDevelopment()
                ? new DefaultAzureCredential()
                : new ManagedIdentityCredential();

        public TokenCredential GetCredential() => _defaultCredential;

        public TokenCredential GetCredential(string clientId)
        {
            if (environment.IsDevelopment())
                return _defaultCredential;

            return _clientCredentials.TryGetValue(clientId, out var credential)
                ? credential
                : (_clientCredentials[clientId] = new ManagedIdentityCredential(clientId));
        }
    }
}
