using Microsoft.Azure.Cosmos;

namespace CRM.ICon.Modality.Helpers.ModalityCosmos
{
    public interface IModalityCosmosDbClient
    {
        /// <summary>
        /// Add/Update list of documents for doc db
        /// </summary>
        /// <param name="query"></param>
        /// <param name="logger"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        Task<T> UpsertItemAsync<T>(string containerId, T item);
        Task<T> GetItemAsync<T>(string containerId, string id);
        Task<IEnumerable<T>> QueryItemsAsync<T>(string containerId, QueryDefinition query);
        Task<T?> GetScalarValueAsync<T>(string containerId, QueryDefinition query);
        Task CreateItemAsync<T>(string containerId, T item, string partitionKey);
    }
}
