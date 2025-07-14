// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CosmosDbClient.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CRM.ICon.Modality.Helpers.ModalityCosmos
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Identity;
    using CRM.ICon.Modality;
    using CRM.ICon.Modality.Helpers.Telemetry;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.Linq;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;

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

        public async Task<T> UpsertItemAsync<T>(string ContainerId, T item)
        {
            var container = await GetContainerAsync(ContainerId);
            var response = await container.UpsertItemAsync<T>(item).ConfigureAwait(false);
            var resource = response.Resource;
            if (resource != null)
            {
                return (T)(dynamic)resource;
            }
            return default(T);
        }

        public async Task<T> GetItemAsync<T>(string containerId, string id)
        {
            try
            {
                var container = await GetContainerAsync(containerId);
                var response = await container.ReadItemAsync<T>(id, new PartitionKey(id)).ConfigureAwait(false);
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

        private async Task<Container> GetContainerAsync(string containerId)
        {
            try
            {
                telemetryService.LogTrace<ModalityCosmosDbClient>($"Starting GetContainerAsync on {containerId}");

                if (string.IsNullOrEmpty(containerId))
                {
                    containerId = cosmosDbConfiguration.ContainerIds.WidgetMapping;
                    telemetryService.LogTrace<ModalityCosmosDbClient>($"Using default container: {containerId}");
                }

                telemetryService.LogTrace<ModalityCosmosDbClient>($"Attempting to get/create database: {cosmosDbConfiguration.DatabaseId}");

                var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDbConfiguration.DatabaseId);

                // Log database creation result
                if (databaseResponse.StatusCode == HttpStatusCode.Created)
                {
                    telemetryService.LogTrace<ModalityCosmosDbClient>($"Database '{cosmosDbConfiguration.DatabaseId}' was created successfully");
                }
                else if (databaseResponse.StatusCode == HttpStatusCode.OK)
                {
                    telemetryService.LogTrace<ModalityCosmosDbClient>($"Database '{cosmosDbConfiguration.DatabaseId}' exists");
                }
                else
                {
                    telemetryService.LogTrace<ModalityCosmosDbClient>($"Unexpected status code when creating/getting database: {databaseResponse.StatusCode}");
                }

                telemetryService.LogTrace<ModalityCosmosDbClient>($"Attempting to get/create container: {containerId}");

                var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(containerId, "/id");

                // Log container creation result
                if (containerResponse.StatusCode == HttpStatusCode.Created)
                {
                    telemetryService.LogTrace<ModalityCosmosDbClient>($"Container '{containerId}' was created successfully");
                }
                else if (containerResponse.StatusCode == HttpStatusCode.OK)
                {
                    telemetryService.LogTrace<ModalityCosmosDbClient>($"Container '{containerId}' exists");
                }
                else
                {
                    telemetryService.LogTrace<ModalityCosmosDbClient>($"Unexpected status code when creating/getting container: {containerResponse.StatusCode}");
                }
                return containerResponse.Container;
            }
            catch (CosmosException cosmosEx)
            {
                telemetryService.LogTrace<ModalityCosmosDbClient>($"Cosmos DB error when getting/creating container '{containerId}': {cosmosEx.Message}", cosmosEx.ToDictionary());
                throw;
            }
            catch (Exception ex)
            {
                telemetryService.LogTrace<ModalityCosmosDbClient>($"General error when getting/creating container '{containerId}': {ex.Message}", ex.ToDictionary());
                throw;
            }
        }

        public async Task<T> ReplaceItemAsync<T>(string containerId, string id, T item)
        {
            try
            {
                var container = await GetContainerAsync(containerId);
                var response = await container
                    .ReplaceItemAsync<T>(item, id, new PartitionKey(id))
                    .ConfigureAwait(false);
                return response.Resource != null ? (T)(dynamic)response.Resource : default(T);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Item to replace was not found
                return default(T);
            }
            catch (Exception ex)
            {
                // Optionally log or wrap the exception
                throw;
            }
        }

        public async Task<FeedIterator<T>> QueryItemsIteratorAsync<T>(string containerId, QueryDefinition query)
        {
            var container = await GetContainerAsync(containerId);
            return container.GetItemQueryIterator<T>(query);
        }

        public async Task<T> GetItemByIdAsync<T>(string containerId, string id, string partitionKey)
        {
            try
            {
                var container = await GetContainerAsync(containerId);
                var response = await container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }
        }

        public async Task<T> DeleteItemAsync<T>(string containerId, string id, string partitionKey)
        {
            try
            {
                var container = await GetContainerAsync(containerId);
                var response = await container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }
            catch (Exception ex)
            {
                telemetryService.LogTrace<ModalityCosmosDbClient>($"Error deleting item with id '{id}' from container '{containerId}': {ex.Message}", ex.ToDictionary());
                throw;
            }
        }
    }
}
