namespace CRM.ICon.Modality.Helpers.KeyVaultClient
{
    public interface IKeyVaultSecretProvider
    {
        /// <summary>
        /// Get a secret from azure key vault
        /// </summary>
        /// <param name="secretName">Name of the secret.</param>
        /// <returns>System.String.</returns>
        Task<string> GetSecretAsStringAsync(string secretName);
    }
}
