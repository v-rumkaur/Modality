// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CosmosDbClientProvider.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Net;
using Azure.Identity;
using CRM.ICon.Modality.Helpers.Telemetry;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace CRM.ICon.Modality.Helpers.ModalityCosmos
{
    /// <summary>
    /// A client for manipulating DocumentDB data
    /// </summary>
    public class ModalityCosmosDbClient : IModalityCosmosDbClient
    {
        private readonly ModalityCosmosDbConfiguration cosmosDbConfiguration;
        private readonly CosmosClient cosmosClient;
        private readonly AzureAdConfiguration azureAdConfiguration;
        private readonly ITelemetryService telemetryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModalityCosmosDbClient"/> class.
        /// </summary>
        /// <param name="cosmosDbConfiguration">Cosmos db configuration</param>
        public ModalityCosmosDbClient(
            IOptions<ModalityCosmosDbConfiguration> cosmosDbConfiguration,
            IOptions<AzureAdConfiguration> azureAdConfiguration,
            ITelemetryService telemetryService
        )
        {
            this.cosmosDbConfiguration =
                cosmosDbConfiguration?.Value
                ?? throw new ArgumentNullException(nameof(cosmosDbConfiguration));
            this.azureAdConfiguration = azureAdConfiguration.Value;
            this.telemetryService =
                telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            var cosmosRequestTimeout =
                this.cosmosDbConfiguration.RequestTimeout > 0
                    ? this.cosmosDbConfiguration.RequestTimeout
                    : 2;
            var cosmosClientOptions = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Direct,
                RequestTimeout = TimeSpan.FromMinutes(cosmosRequestTimeout),
                MaxRetryAttemptsOnRateLimitedRequests = 2,
                SerializerOptions = new CosmosSerializationOptions()
                {
                    IgnoreNullValues = true,
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                },
            };
            cosmosClient = new CosmosClient(
                this.cosmosDbConfiguration.CosmosDbEndpoint,
                new DefaultAzureCredential(
                    new DefaultAzureCredentialOptions
                    {
                        ManagedIdentityClientId = this.azureAdConfiguration.ManagedIdentityClientId,
                    }
                ),
                cosmosClientOptions
            );
        }

        // CREATE/UPDATE operations
        /// <inheritdoc/>
        public async Task<T> UpsertItemAsync<T>(string containerId, T item)
        {
            if (string.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException(
                    "Container ID cannot be null or empty.",
                    nameof(containerId)
                );
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var container = GetContainer(containerId);
            var response = await container.UpsertItemAsync<T>(item);
            return response.Resource;
        }

        /// <inheritdoc/>
        public async Task CreateItemAsync<T>(string containerId, T item, string partitionKey)
        {
            if (string.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException(
                    "Container ID cannot be null or empty.",
                    nameof(containerId)
                );
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (string.IsNullOrWhiteSpace(partitionKey))
                throw new ArgumentException(
                    "Partition key cannot be null or empty.",
                    nameof(partitionKey)
                );

            var container = GetContainer(containerId);
            await container.CreateItemAsync(item, new PartitionKey(partitionKey));
        }

        // READ operations (single items, queries)

        /// <inheritdoc/>
        public async Task<T?> GetItemByIdAsync<T>(
            string containerId,
            string name,
            string partitionKey
        )
        {
            if (string.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException(
                    "Container ID cannot be null or empty.",
                    nameof(containerId)
                );
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(partitionKey))
                throw new ArgumentException(
                    "Partition key cannot be null or empty.",
                    nameof(partitionKey)
                );

            try
            {
                var container = GetContainer(containerId);
                var response = await container.ReadItemAsync<T>(
                    name,
                    new PartitionKey(partitionKey)
                );
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return default(T);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error retrieving item with name '{name}' from container '{containerId}'.",
                    ex
                );
            }
        }

        /// <inheritdoc/>
        /// Retrieves an item by ID without partition key (for legacy containers).
        public async Task<T?> GetItemAsync<T>(string id, string containerId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be null or empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException(
                    "Container ID cannot be null or empty.",
                    nameof(containerId)
                );

            try
            {
                var container = GetContainer(containerId);
                var response = await container.ReadItemAsync<T>(id, new PartitionKey());
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return default(T);
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<T>> QueryItemsAsync<T>(
            string containerId,
            QueryDefinition query
        )
        {
            if (string.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException(
                    "Container ID cannot be null or empty.",
                    nameof(containerId)
                );
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            var container = GetContainer(containerId);
            var results = new List<T>();
            var iterator = container.GetItemQueryIterator<T>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        /// <inheritdoc/>
        public async Task<T?> GetScalarValueAsync<T>(string containerId, QueryDefinition query)
        {
            if (string.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException(
                    "Container ID cannot be null or empty.",
                    nameof(containerId)
                );
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            var container = GetContainer(containerId);
            var iterator = container.GetItemQueryIterator<T>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }

            return default;
        }

        /// <inheritdoc/>
        public FeedIterator<T> QueryItemsIterator<T>(string containerId, QueryDefinition query)
        {
            if (string.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException(
                    "Container ID cannot be null or empty.",
                    nameof(containerId)
                );
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            var container = GetContainer(containerId);
            return container.GetItemQueryIterator<T>(query);
        }

        // DELETE operations
        /// <inheritdoc/>
        public async Task DeleteItemAsync(string containerId, string id, string partitionKey)
        {
            if (string.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException(
                    "Container ID cannot be null or empty.",
                    nameof(containerId)
                );
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be null or empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(partitionKey))
                throw new ArgumentException(
                    "Partition key cannot be null or empty.",
                    nameof(partitionKey)
                );

            var container = GetContainer(containerId);
            await container.DeleteItemAsync<object>(id, new PartitionKey(partitionKey));
        }

        // UTILITY methods
        /// <inheritdoc/>
        public Container GetContainer(string databaseId, string containerId) =>
            cosmosClient.GetContainer(databaseId, containerId);

        private Container GetContainer(string containerId)
        {
            try
            {
                return cosmosClient.GetContainer(cosmosDbConfiguration.DatabaseId, containerId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error accessing container '{containerId}' in database '{cosmosDbConfiguration.DatabaseId}'.",
                    ex
                );
            }
        }
    }
}
// public async Task<T> UpsertItemAsync<T>(string containerId, T item)
// {
//     telemetryService.LogTrace<ModalityCosmosDbClient>($"Starting UpsertItemAsync on container: {containerId}");
//     var container = await GetContainerAsync(containerId);
//     var response = await container.UpsertItemAsync<T>(item).ConfigureAwait(false);
//     var resource = response.Resource;
//     if (resource != null)
//     {
//         return (T)(dynamic)resource;
//     }
//     return default(T);
// }

// // used for widgetmapping, partition key will break livechat settings
// public async Task<T> GetItemAsync<T>(string id, string containerId)
// {
//     try
//     {
//         var container = await GetContainerAsync(containerId);
//         var response = await container.ReadItemAsync<T>(id, new PartitionKey()).ConfigureAwait(false);
//         var resource = response.Resource;
//         if (resource != null)
//         {
//             return (T)(dynamic)resource;
//         }
//     }
//     catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
//     {
//         return default(T);
//     }
//     catch (Exception ex)
//     {
//         return default(T);
//     }
//     return default(T);
// }
