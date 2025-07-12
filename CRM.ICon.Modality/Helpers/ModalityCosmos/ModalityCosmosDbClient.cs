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
            this.cosmosClient = new CosmosClient(this.cosmosDbConfiguration.CosmosDbEndpoint, new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = this.azureAdConfiguration.ManagedIdentityClientId }), cosmosClientOptions);
        }

        public async Task<T> UpsertItemAsync<T>(T item)
        {
            var container = await GetContainerAsync();
            var response = await container.UpsertItemAsync<T>(item).ConfigureAwait(false);
            var resource = response.Resource;
            if (resource != null)
            {
                return (T)(dynamic)resource;
            }
            return default(T);
        }

        public async Task<T> GetItemAsync<T>(string id)
        {
            try
            {
                var container = await GetContainerAsync();
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

        private async Task<Container> GetContainerAsync()
        {
            var database = await this.cosmosClient.CreateDatabaseIfNotExistsAsync(this.cosmosDbConfiguration.DatabaseId);
            return await database.Database.CreateContainerIfNotExistsAsync(this.cosmosDbConfiguration.ContainerIds.WidgetMapping, "/id");
        }
    }
}