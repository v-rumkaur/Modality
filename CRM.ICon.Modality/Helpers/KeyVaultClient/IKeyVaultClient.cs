using Azure.Security.KeyVault.Secrets;
using System.Security.Cryptography.X509Certificates;

namespace CRM.ICon.Modality.Helpers.KeyVaultClient
{
    public interface IKeyVaultClient
    {
        /// <summary>
        /// Gets secret from key vault
        /// </summary>
        /// <param name="secretName"></param>
        /// <returns></returns>
        Task<string> GetSecretAsync(string secretName);

        /// <summary>
        /// Gets certificate from key vault
        /// </summary>
        /// <param name="certificateName">Certificate Name</param>
        /// <returns>Certificate Bundle</returns>
        Task<X509Certificate2> GetCertificateAsync(string certificateName);

        /// <summary>
        /// Gets certificate properties from key vault
        /// </summary>
        /// <param name="certificateName">Certificate Name</param>
        /// <returns>Certificate Bundle</returns>
        Task<SecretProperties> GetCertificatePropertiesAsync(string certificateName);
    }
}
