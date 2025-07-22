// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IModalityCosmosDbClient.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.Azure.Cosmos;

namespace CRM.ICon.Modality.Helpers.ModalityCosmos
{
    /// <summary>
    /// Provides abstraction layer for Cosmos DB operations with error handling and async support.
    /// </summary>
    public interface IModalityCosmosDbClient
    {
        /// <summary>
        /// Creates or updates an item in the specified container.
        /// </summary>
        /// <typeparam name="T">The type of item to upsert.</typeparam>
        /// <param name="containerId">The container identifier.</param>
        /// <param name="item">The item to create or update.</param>
        /// <returns>The upserted item.</returns>
        Task<T> UpsertItemAsync<T>(string containerId, T item);

        /// <summary>
        /// Retrieves an item by ID without partition key (for legacy containers).
        /// Used for widget mapping where partition key breaks live chat settings.
        /// </summary>
        /// <typeparam name="T">The type of item to retrieve.</typeparam>
        /// <param name="id">The item identifier.</param>
        /// <param name="containerId">The container identifier.</param>
        /// <returns>The item if found, otherwise null.</returns>
        Task<T?> GetItemAsync<T>(string id, string containerId);

        /// <summary>
        /// Creates a new item in the specified container with partition key.
        /// </summary>
        /// <typeparam name="T">The type of item to create.</typeparam>
        /// <param name="containerId">The container identifier.</param>
        /// <param name="item">The item to create.</param>
        /// <param name="partitionKey">The partition key value.</param>
        Task CreateItemAsync<T>(string containerId, T item, string partitionKey);

        /// <summary>
        /// Retrieves an item by ID and partition key.
        /// </summary>
        /// <typeparam name="T">The type of item to retrieve.</typeparam>
        /// <param name="containerId">The container identifier.</param>
        /// <param name="id">The item identifier.</param>
        /// <param name="partitionKey">The partition key value.</param>
        /// <returns>The item if found, otherwise null.</returns>
        Task<T?> GetItemByIdAsync<T>(string containerId, string id, string partitionKey);

        /// <summary>
        /// Deletes an item by ID and partition key.
        /// </summary>
        /// <param name="containerId">The container identifier.</param>
        /// <param name="id">The item identifier.</param>
        /// <param name="partitionKey">The partition key value.</param>
        Task DeleteItemAsync(string containerId, string id, string partitionKey);

        /// <summary>
        /// Executes a query and returns all matching items.
        /// </summary>
        /// <typeparam name="T">The type of items to return.</typeparam>
        /// <param name="containerId">The container identifier.</param>
        /// <param name="query">The query definition to execute.</param>
        /// <returns>Collection of matching items.</returns>
        Task<IEnumerable<T>> QueryItemsAsync<T>(string containerId, QueryDefinition query);

        /// <summary>
        /// Executes a query and returns the first scalar value (useful for COUNT, MAX, etc.).
        /// </summary>
        /// <typeparam name="T">The type of scalar value to return.</typeparam>
        /// <param name="containerId">The container identifier.</param>
        /// <param name="query">The query definition to execute.</param>
        /// <returns>The first scalar value or default if no results.</returns>
        Task<T?> GetScalarValueAsync<T>(string containerId, QueryDefinition query);

        /// <summary>
        /// Creates a query iterator for streaming large result sets.
        /// </summary>
        /// <typeparam name="T">The type of items to iterate over.</typeparam>
        /// <param name="containerId">The container identifier.</param>
        /// <param name="query">The query definition to execute.</param>
        /// <returns>A feed iterator for processing results in batches.</returns>
        Task<FeedIterator<T>> QueryItemsIteratorAsync<T>(string containerId, QueryDefinition query);

        /// <summary>
        /// Gets a container reference (synchronous helper method).
        /// </summary>
        /// <param name="databaseId">The database identifier.</param>
        /// <param name="containerId">The container identifier.</param>
        /// <returns>Container reference.</returns>
        Task<Container> GetContainerAsync(string containerId);
    }
}
