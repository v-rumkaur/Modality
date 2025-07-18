// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IModalityCosmosDbClient.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.Azure.Cosmos;

namespace CRM.ICon.Modality.Helpers.ModalityCosmos
{
    public interface IModalityCosmosDbClient
    {
        /// <summary>
        /// Add/Update list of documents for doc db
        /// </summary>
        /// <param name="containerId">Container identifier</param>
        /// <param name="item">Item to upsert</param>
        /// <returns>The upserted item</returns>
        Task<T> UpsertItemAsync<T>(string containerId, T item);

        /// <summary>
        /// Get item by id from container
        /// </summary>
        /// <param name="id">Item identifier</param>
        /// <param name="containerId">Container identifier</param>
        /// <returns>The item if found, otherwise default</returns>
        Task<T> GetItemAsync<T>(string id, string containerId);

        /// <summary>
        /// Query items from container
        /// </summary>
        /// <param name="containerId">Container identifier</param>
        /// <param name="query">Query definition</param>
        /// <returns>Collection of items</returns>
        Task<IEnumerable<T>> QueryItemsAsync<T>(string containerId, QueryDefinition query);

        /// <summary>
        /// Get scalar value from query
        /// </summary>
        /// <param name="containerId">Container identifier</param>
        /// <param name="query">Query definition</param>
        /// <returns>Scalar value</returns>
        Task<T?> GetScalarValueAsync<T>(string containerId, QueryDefinition query);

        /// <summary>
        /// Create item in container
        /// </summary>
        /// <param name="containerId">Container identifier</param>
        /// <param name="item">Item to create</param>
        /// <param name="partitionKey">Partition key value</param>
        Task CreateItemAsync<T>(string containerId, T item, string partitionKey);

        /// <summary>
        /// Get item by id and partition key
        /// </summary>
        /// <param name="containerId">Container identifier</param>
        /// <param name="name">Item name/id</param>
        /// <param name="partitionKey">Partition key value</param>
        /// <returns>The item if found, otherwise default</returns>
        Task<T?> GetItemByIdAsync<T>(string containerId, string name, string partitionKey);

        /// <summary>
        /// Delete item from container
        /// </summary>
        /// <param name="containerId">Container identifier</param>
        /// <param name="id">Item identifier</param>
        /// <param name="partitionKey">Partition key value</param>
        Task DeleteItemAsync(string containerId, string id, string partitionKey);

        /// <summary>
        /// Get container reference
        /// </summary>
        /// <param name="databaseId">Database identifier</param>
        /// <param name="containerId">Container identifier</param>
        /// <returns>Container reference</returns>
        Container GetContainer(string databaseId, string containerId);

        /// <summary>
        /// Query items with iterator
        /// </summary>
        /// <param name="containerId">Container identifier</param>
        /// <param name="query">Query definition</param>
        /// <returns>Feed iterator for items</returns>
        Task<FeedIterator<T>> QueryItemsIteratorAsync<T>(string containerId, QueryDefinition query);
    }
}