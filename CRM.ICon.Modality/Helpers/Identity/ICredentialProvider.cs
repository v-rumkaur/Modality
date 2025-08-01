using Azure.Core;

namespace CRM.ICon.Modality.Helpers.Identity
{
    public interface ICredentialProvider
    {
        TokenCredential GetCredential();
        TokenCredential GetCredential(string clientId);
    }
}
