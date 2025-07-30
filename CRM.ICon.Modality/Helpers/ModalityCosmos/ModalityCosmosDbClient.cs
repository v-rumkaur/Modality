// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ModalityCosmosDbClient.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Net;
using Azure.Identity;
using CRM.ICon.Modality.Helpers.Telemetry;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using static CRM.ICon.Modality.ModalityConstants;

namespace CRM.ICon.Modality.Helpers.ModalityCosmos
{
    /// <summary>
    /// A client for manipulating Cosmos DB data with telemetry and error handling
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
        /// <param name="cosmosDbConfiguration">Cosmos DB configuration settings</param>
        /// <param name="azureAdConfiguration">Azure AD configuration for authentication</param>
        /// <param name="telemetryService">Telemetry service for logging operations</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        public ModalityCosmosDbClient(
            IOptions<ModalityCosmosDbConfiguration> cosmosDbConfiguration,
            IOptions<AzureAdConfiguration> azureAdConfiguration,
            ITelemetryService telemetryService
        )
        {
            this.cosmosDbConfiguration =
                cosmosDbConfiguration?.Value
                ?? throw new ArgumentNullException(nameof(cosmosDbConfiguration));
            this.azureAdConfiguration =
                azureAdConfiguration?.Value
                ?? throw new ArgumentNullException(nameof(azureAdConfiguration));
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

            telemetryService.LogTrace<ModalityCosmosDbClient>(
                $"ModalityCosmosDbClient initialized with endpoint: {this.cosmosDbConfiguration.CosmosDbEndpoint}, database: {this.cosmosDbConfiguration.DatabaseId}"
            );
        }

        #region CREATE/UPDATE Operations

        /// <inheritdoc/>
        public async Task CreateItemAsync<T>(string containerId, T item, string partitionKey)
        {
            ValidateContainerParameters(containerId, nameof(containerId));
            ValidateItemParameter(item, nameof(item));
            ValidatePartitionKeyParameter(partitionKey, nameof(partitionKey));

            telemetryService.LogTrace<ModalityCosmosDbClient>(
                $"Creating item in container: {containerId}, partitionKey: {partitionKey}"
            );

            try
            {
                var container = await GetContainerAsync(containerId);
                var response = await container.CreateItemAsync(
                    item,
                    new PartitionKey(partitionKey)
                );

                telemetryService.LogTrace<ModalityCosmosDbClient>(
                    $"Successfully created item in container: {containerId}, RU consumed: {response.RequestCharge}"
                );
            }
            catch (Exception ex)
            {
                telemetryService.LogError<ModalityCosmosDbClient>(
                    $"Error creating item in container '{containerId}': {ex.Message}",
                    ex.ToDictionary()
                );
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<T> UpsertItemAsync<T>(string containerId, T item)
        {
            ValidateContainerParameters(containerId, nameof(containerId));
            ValidateItemParameter(item, nameof(item));

            telemetryService.LogTrace<ModalityCosmosDbClient>(
                $"Upserting item in container: {containerId}, item type: {typeof(T).Name}"
            );

            try
            {
                var container = await GetContainerAsync(containerId);
                var response = await container.UpsertItemAsync<T>(item);

                telemetryService.LogTrace<ModalityCosmosDbClient>(
                    $"Successfully upserted item in container: {containerId}, RU consumed: {response.RequestCharge}"
                );

                return response.Resource;
            }
            catch (Exception ex)
            {
                telemetryService.LogError<ModalityCosmosDbClient>(
                    $"Error upserting item in container '{containerId}': {ex.Message}",
                    ex.ToDictionary()
                );
                throw;
            }
        }

        #endregion

        #region READ Operations


        /// <inheritdoc/>
        public async Task<T?> GetItemAsync<T>(string id, string containerId)
        {
            ValidateStringParameter(id, nameof(id));
            ValidateContainerParameters(containerId, nameof(containerId));

            telemetryService.LogTrace<ModalityCosmosDbClient>(
                $"Getting item without partition key from container: {containerId}, id: {id}"
            );

            try
            {
                var container = await GetContainerAsync(containerId);
                var response = await container
                    .ReadItemAsync<T>(id, new PartitionKey(id))
                    .ConfigureAwait(false);

                telemetryService.LogTrace<ModalityCosmosDbClient>(
                    $"Successfully retrieved item from container: {containerId}, RU consumed: {response.RequestCharge}"
                );

                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                telemetryService.LogTrace<ModalityCosmosDbClient>(
                    $"Item not found in container: {containerId}, id: {id}"
                );
                return default(T);
            }
            catch (Exception ex)
            {
                telemetryService.LogError<ModalityCosmosDbClient>(
                    $"Error retrieving item with id '{id}' from container '{containerId}': {ex.Message}",
                    ex.ToDictionary()
                );
                // For legacy compatibility, return default instead of throwing
                return default(T);
            }
        }

        /// <inheritdoc/>
        public async Task<T?> GetItemByIdAsync<T>(
            string containerId,
            string name,
            string partitionKey
        )
        {
            ValidateContainerParameters(containerId, nameof(containerId));
            ValidateStringParameter(name, nameof(name));
            ValidatePartitionKeyParameter(partitionKey, nameof(partitionKey));

            telemetryService.LogTrace<ModalityCosmosDbClient>(
                $"Getting item by ID from container: {containerId}, name: {name}, partitionKey: {partitionKey}"
            );

            try
            {
                var container = await GetContainerAsync(containerId);
                var response = await container.ReadItemAsync<T>(
                    name,
                    new PartitionKey(partitionKey)
                );

                telemetryService.LogTrace<ModalityCosmosDbClient>(
                    $"Successfully retrieved item from container: {containerId}, RU consumed: {response.RequestCharge}"
                );

                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                telemetryService.LogTrace<ModalityCosmosDbClient>(
                    $"Item not found in container: {containerId}, name: {name}"
                );
                return default(T);
            }
            catch (Exception ex)
            {
                telemetryService.LogError<ModalityCosmosDbClient>(
                    $"Error retrieving item with name '{name}' from container '{containerId}': {ex.Message}",
                    ex.ToDictionary()
                );
                throw new InvalidOperationException(
                    $"Error retrieving item with name '{name}' from container '{containerId}'.",
                    ex
                );
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<T>> QueryItemsAsync<T>(
            string containerId,
            QueryDefinition query
        )
        {
            ValidateContainerParameters(containerId, nameof(containerId));
            ValidateQueryParameter(query, nameof(query));

            telemetryService.LogTrace<ModalityCosmosDbClient>(
                $"Executing query on container: {containerId}, query: {query.QueryText}"
            );

            try
            {
                var container = await GetContainerAsync(containerId);
                var results = new List<T>();
                var iterator = container.GetItemQueryIterator<T>(query);
                double totalRU = 0;

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response);
                    totalRU += response.RequestCharge;
                }

                telemetryService.LogTrace<ModalityCosmosDbClient>(
                    $"Query completed on container: {containerId}, results: {results.Count}, total RU: {totalRU}"
                );

                return results;
            }
            catch (Exception ex)
            {
                telemetryService.LogError<ModalityCosmosDbClient>(
                    $"Error executing query on container '{containerId}': {ex.Message}",
                    ex.ToDictionary()
                );
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<T?> GetScalarValueAsync<T>(string containerId, QueryDefinition query)
        {
            ValidateContainerParameters(containerId, nameof(containerId));
            ValidateQueryParameter(query, nameof(query));

            telemetryService.LogTrace<ModalityCosmosDbClient>(
                $"Executing scalar query on container: {containerId}, query: {query.QueryText}"
            );

            try
            {
                var container = await GetContainerAsync(containerId);
                var iterator = container.GetItemQueryIterator<T>(query);

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    var result = response.FirstOrDefault();

                    telemetryService.LogTrace<ModalityCosmosDbClient>(
                        $"Scalar query completed on container: {containerId}, RU consumed: {response.RequestCharge}"
                    );

                    return result;
                }

                telemetryService.LogTrace<ModalityCosmosDbClient>(
                    $"Scalar query returned no results on container: {containerId}"
                );

                return default;
            }
            catch (Exception ex)
            {
                telemetryService.LogError<ModalityCosmosDbClient>(
                    $"Error executing scalar query on container '{containerId}': {ex.Message}",
                    ex.ToDictionary()
                );
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<FeedIterator<T>> QueryItemsIteratorAsync<T>(
            string containerId,
            QueryDefinition query
        )
        {
            ValidateContainerParameters(containerId, nameof(containerId));
            ValidateQueryParameter(query, nameof(query));

            telemetryService.LogTrace<ModalityCosmosDbClient>(
                $"Creating query iterator for container: {containerId}, query: {query.QueryText}"
            );

            try
            {
                var container = await GetContainerAsync(containerId);
                return container.GetItemQueryIterator<T>(query);
            }
            catch (Exception ex)
            {
                telemetryService.LogError<ModalityCosmosDbClient>(
                    $"Error creating query iterator for container '{containerId}': {ex.Message}",
                    ex.ToDictionary()
                );
                throw;
            }
        }

        #endregion

        #region DELETE Operations

        /// <inheritdoc/>
        public async Task DeleteItemAsync(string containerId, string id, string partitionKey)
        {
            ValidateContainerParameters(containerId, nameof(containerId));
            ValidateStringParameter(id, nameof(id));
            ValidatePartitionKeyParameter(partitionKey, nameof(partitionKey));

            telemetryService.LogTrace<ModalityCosmosDbClient>(
                $"Deleting item from container: {containerId}, id: {id}, partitionKey: {partitionKey}"
            );

            try
            {
                var container = await GetContainerAsync(containerId);
                var response = await container.DeleteItemAsync<object>(
                    id,
                    new PartitionKey(partitionKey)
                );

                telemetryService.LogTrace<ModalityCosmosDbClient>(
                    $"Successfully deleted item from container: {containerId}, RU consumed: {response.RequestCharge}"
                );
            }
            catch (Exception ex)
            {
                telemetryService.LogError<ModalityCosmosDbClient>(
                    $"Error deleting item with id '{id}' from container '{containerId}': {ex.Message}",
                    ex.ToDictionary()
                );
                throw;
            }
        }

        #endregion

        #region UTILITY Methods

        /// <summary>
        /// Gets a container reference using the configured database ID
        /// </summary>
        /// <param name="containerId">The container identifier</param>
        /// <returns>Container reference</returns>
        /// <exception cref="InvalidOperationException">Thrown when container cannot be accessed</exception>
        private async Task<Container> GetContainerAsync(string containerId)
        {
            try
            {
                var partitionKeyPath = GetPartitionKeyPath(containerId);
                telemetryService.LogTrace<ModalityCosmosDbClient>(
                    $"Getting container - Database: {cosmosDbConfiguration.DatabaseId}, Container: {containerId}, PartitionKeyPath: {partitionKeyPath}"
                );
                var database = cosmosClient.GetDatabase(cosmosDbConfiguration.DatabaseId);
                var containerResponse = await database.CreateContainerIfNotExistsAsync(
                    containerId,
                    partitionKeyPath
                );
                var container = containerResponse.Container;
                var containerProperties = await container.ReadContainerAsync();
                telemetryService.LogTrace<ModalityCosmosDbClient>(
                    $"Existing container partition key: {containerProperties.Resource.PartitionKeyPath}"
                );
                return container;
            }
            catch (Exception ex)
            {
                var partitionKeyPath = GetPartitionKeyPath(containerId);
                telemetryService.LogError<ModalityCosmosDbClient>(
                    $"Error accessing container - Database: {cosmosDbConfiguration.DatabaseId}, Container: {containerId}, PartitionKeyPath: {partitionKeyPath}, Error: {ex.Message}",
                    ex.ToDictionary()
                );
                throw new InvalidOperationException(
                    $"Error accessing container '{containerId}' in database '{cosmosDbConfiguration.DatabaseId}' with partition key '{partitionKeyPath}'.",
                    ex
                );
            }
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validates container ID parameter
        /// </summary>
        /// <param name="containerId">Container ID to validate</param>
        /// <param name="parameterName">Parameter name for exception</param>
        /// <exception cref="ArgumentException">Thrown when container ID is null or empty</exception>
        private static void ValidateContainerParameters(string containerId, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(containerId))
                throw new ArgumentException("Container ID cannot be null or empty.", parameterName);
        }

        /// <summary>
        /// Validates string parameter
        /// </summary>
        /// <param name="value">String value to validate</param>
        /// <param name="parameterName">Parameter name for exception</param>
        /// <exception cref="ArgumentException">Thrown when value is null or empty</exception>
        private static void ValidateStringParameter(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    $"{parameterName} cannot be null or empty.",
                    parameterName
                );
        }

        /// <summary>
        /// Validates partition key parameter
        /// </summary>
        /// <param name="partitionKey">Partition key to validate</param>
        /// <param name="parameterName">Parameter name for exception</param>
        /// <exception cref="ArgumentException">Thrown when partition key is null or empty</exception>
        private static void ValidatePartitionKeyParameter(string partitionKey, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
                throw new ArgumentException(
                    "Partition key cannot be null or empty.",
                    parameterName
                );
        }

        /// <summary>
        /// Validates item parameter
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="item">Item to validate</param>
        /// <param name="parameterName">Parameter name for exception</param>
        /// <exception cref="ArgumentNullException">Thrown when item is null</exception>
        private static void ValidateItemParameter<T>(T item, string parameterName)
        {
            if (item == null)
                throw new ArgumentNullException(parameterName);
        }

        /// <summary>
        /// Validates query parameter
        /// </summary>
        /// <param name="query">Query to validate</param>
        /// <param name="parameterName">Parameter name for exception</param>
        /// <exception cref="ArgumentNullException">Thrown when query is null</exception>
        private static void ValidateQueryParameter(QueryDefinition query, string parameterName)
        {
            if (query == null)
                throw new ArgumentNullException(parameterName);
        }
        #endregion

        /// <summary>
        /// Gets the partition key path for a given container
        /// </summary>
        /// <param name="containerId">The container identifier</param>
        /// <returns>The partition key path (e.g., "/id" or "/partitionKey")</returns>
        private string GetPartitionKeyPath(string containerId)
        {
            // LiveChatSettings uses a custom partition key, everything else uses /id
            if (
                string.Equals(
                    containerId,
                    cosmosDbConfiguration.ContainerIds.LiveChatSettings,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return "/" + LiveChatConstants.PartitionKeyPath;
            }

            return "/" + WidgetMappingConstants.PartitionKeyPath; // Default for WidgetMapping and any other containers
        }
    }
}
