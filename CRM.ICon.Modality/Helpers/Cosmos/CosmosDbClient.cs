// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CosmosDbClient.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CRM.ICon.Modality.Helpers.Cosmos
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
    using CRM.ICon.Modality.Helpers.Identity;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.Linq;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// A client for manipulating DocumentDB data
    /// </summary>
    public class CosmosDbClient : ICosmosDbClient
    {
        private readonly CosmosDbConfiguration cosmosDbConfiguration;
        private readonly CosmosClient cosmosClient;
        private readonly AzureAdConfiguration azureAdConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbClient"/> class.
        /// </summary>
        /// <param name="cosmosDbConfiguration">Cosmos db configuration</param>
        public CosmosDbClient(IOptions<CosmosDbConfiguration> cosmosDbConfiguration, IOptions<AzureAdConfiguration> azureAdConfiguration, ICredentialProvider credentialProvider)
        {
            this.cosmosDbConfiguration = cosmosDbConfiguration?.Value ?? throw new ArgumentNullException(nameof(cosmosDbConfiguration));
            this.azureAdConfiguration = azureAdConfiguration.Value;
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
            this.cosmosClient = new CosmosClient(this.cosmosDbConfiguration.CosmosDbEndpoint, credentialProvider.GetCredential(this.azureAdConfiguration.ManagedIdentityClientId), cosmosClientOptions);
        }

        public async Task<T> UpsertItemAsync<T>(T item)
        {
            var container = GetContainerAsync();
            var response = await container.UpsertItemAsync<T>(item).ConfigureAwait(false);
            var resource = response.Resource;
            if (resource != null)
            {
                return (T)(dynamic)resource;
            }
            return default(T);
        }

        private Container GetContainerAsync()
        {
            return this.cosmosClient.GetContainer(cosmosDbConfiguration.DatabaseId, cosmosDbConfiguration.ContainerId);
        }
    }
}