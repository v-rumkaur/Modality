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
        Task<T> ReplaceItemAsync<T>(string containerId, string id, T item);
        Task<FeedIterator<T>> QueryItemsIteratorAsync<T>(string containerId, QueryDefinition query);
        Task<T> GetItemByIdAsync<T>(string containerId, string id, string partitionKey);
        Task<T> DeleteItemAsync<T>(string containerId, string id, string partitionKey);
    }
}
