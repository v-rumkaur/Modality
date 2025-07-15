// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CosmosDbClient.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CRM.ICon.Modality.Helpers.ModalityCosmos
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Azure.Identity;
    using CRM.ICon.Modality;
    using CRM.ICon.Modality.Helpers.Telemetry;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Extensions.Options;

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
        public ModalityCosmosDbClient(IOptions<ModalityCosmosDbConfiguration> cosmosDbConfiguration, IOptions<AzureAdConfiguration> azureAdConfiguration, ITelemetryService telemetryService)
        {
            this.cosmosDbConfiguration = cosmosDbConfiguration?.Value ?? throw new ArgumentNullException(nameof(cosmosDbConfiguration));
            this.azureAdConfiguration = azureAdConfiguration.Value;
            this.telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            var cosmosRequestTimeout = this.cosmosDbConfiguration.RequestTimeout > 0 ? this.cosmosDbConfiguration.RequestTimeout : 2;
            var cosmosClientOptions = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Direct,
                RequestTimeout = TimeSpan.FromMinutes(cosmosRequestTimeout),
                MaxRetryAttemptsOnRateLimitedRequests = 2,
                SerializerOptions = new CosmosSerializationOptions()
                {
                    IgnoreNullValues = true,
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };
            cosmosClient = new CosmosClient(this.cosmosDbConfiguration.CosmosDbEndpoint, new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = this.azureAdConfiguration.ManagedIdentityClientId }), cosmosClientOptions);
        }

        public async Task<T> UpsertItemAsync<T>(string containerId, T item)
        {
            telemetryService.LogTrace<ModalityCosmosDbClient>($"Starting UpsertItemAsync on container: {containerId}");
            var container = await GetContainerAsync(containerId, "/id");
            var response = await container.UpsertItemAsync<T>(item).ConfigureAwait(false);
            var resource = response.Resource;
            if (resource != null)
            {
                return (T)(dynamic)resource;
            }
            return default(T);
        }

        // used for widgetmapping, partition key will break livechat settings
        public async Task<T> GetItemAsync<T>(string id, string containerId)
        {
            try
            {
                var container = await GetContainerAsync(containerId, "/id");
                var response = await container.ReadItemAsync<T>(id, new PartitionKey()).ConfigureAwait(false);
                var resource = response.Resource;
                if (resource != null)
                {
                    return (T)(dynamic)resource;
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return default(T);
            }
            catch (Exception ex)
            {
                return default(T);
            }
            return default(T);
        }

        private async Task<Container> GetContainerAsync(string containerId, string partitionKey)
        {
            telemetryService.LogTrace<ModalityCosmosDbClient>($"Starting GetContainerAsync on {containerId} of database {cosmosDbConfiguration.DatabaseId} with partition key {partitionKey}");
            try
            {
                {
                    var database = await this.cosmosClient.CreateDatabaseIfNotExistsAsync(this.cosmosDbConfiguration.DatabaseId);
                    return await database.Database.CreateContainerIfNotExistsAsync(containerId, partitionKey);
                }
            }
            catch (Exception ex)
            {
                telemetryService.LogTrace<ModalityCosmosDbClient>($"Error when getting/creating container '{containerId}'!", ex.ToDictionary());
                throw;
            }
        }

        public async Task<IEnumerable<T>> QueryItemsAsync<T>(string containerId, QueryDefinition query)
        {
            var container = await GetContainerAsync(containerId, Constants.LiveChat.PartitionKey);
            var results = new List<T>();
            var iterator = container.GetItemQueryIterator<T>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        public async Task<T?> GetScalarValueAsync<T>(string containerId, QueryDefinition query)
        {
            var container = await GetContainerAsync(containerId, Constants.LiveChat.PartitionKey);
            var iterator = container.GetItemQueryIterator<T>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }

            return default;
        }

        public async Task CreateItemAsync<T>(string containerId, T item, string partitionKey)
        {
            var container = await GetContainerAsync(containerId, Constants.LiveChat.PartitionKey);
            telemetryService.LogTrace<ModalityCosmosDbClient>($"Creating item in container: {containerId} with partition key: {partitionKey}");
            await container.CreateItemAsync(item, new PartitionKey(partitionKey));
        }
    }
}
