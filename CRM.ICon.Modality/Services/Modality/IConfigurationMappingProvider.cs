using CRM.ICon.Modality.Helpers.Logging;

namespace CRM.ICon.Modality.Services.Modality
{
    public interface IConfigurationMappingProvider
    {
        /// <summary>
        /// Get list of documents from doc db
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sllLogger"></param>
        /// <returns></returns>
        Task<Tuple<string, T>> ExecuteQueryAsync<T>(String query, ISllLogger sllLogger);
    }
}
