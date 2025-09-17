using Azure.Core;
using CRM.ICon.Modality.Helpers;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.ICon.Modality.Services.Modality
{
    public class ConfigurationMappingDocDbProvider : IConfigurationMappingProvider
    {
        private readonly CosmosClient reliableCosmosClient;
        private readonly Container container;
        private readonly ISllLogger sllLogger;

        public ConfigurationMappingDocDbProvider(string endpointUri, TokenCredential token, string preferredLocations, ISllLogger sllLogger, string database, string container)
        {
            this.sllLogger = Verify.NotNull(sllLogger, nameof(sllLogger));
            try
            {
                var cosmosClientOptions = new CosmosClientOptions()
                {
                    ConnectionMode = ConnectionMode.Direct,
                    RequestTimeout = TimeSpan.FromSeconds(30),
                    ApplicationPreferredRegions = preferredLocations.Trim().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                };
                this.reliableCosmosClient = new CosmosClient(endpointUri, token, cosmosClientOptions);
                this.container = this.GetContainer(database, container);
            }
            catch (Exception exception)
            {
                sllLogger.WriteInformationalTelemetry("ConfigurationMappingDocDbProvider", "Initialization of ConfigurationMappingDocDbProvider failed with exception: {0}", exception);
            }
        }

        private Container GetContainer(string Database, string Container)
        {
            return this.reliableCosmosClient.GetContainer(Database, Container);
        }

        /// <summary>
        /// Get list of documents from doc db
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sllLogger"></param>
        /// <returns></returns>
        public async Task<Tuple<string, T>> ExecuteQueryAsync<T>(string query, ISllLogger sllLogger)
        {
            var result = default(Tuple<string, T>);

            using (OperationQoS operation = new OperationQoS(sllLogger, "ExecuteQueryAsync"))
            {
                try
                {
                    this.sllLogger.TrackOutgoingRequest(
                PartnerOps.ExecuteDocumentDbQueryOp.OpName,
                PartnerOps.ExecuteDocumentDbQueryOp,
                (qosEvent) =>
                {
                    //Comment Ruminder
                    //var @customQosEvent = qosEvent as OutgoingQosEventWrapper<Microsoft.Telemetry.Data<Ms.Qos.OutgoingServiceRequest>>;
                });
                    var documentQuery = await this.container.ReadItemAsync<T>(query, new PartitionKey(query)).ConfigureAwait(false);


                    double requestCharges = 0;
                    var activityId = string.Empty;

                    if (documentQuery != null)
                    {
                        requestCharges = documentQuery.RequestCharge;
                        activityId = documentQuery.ActivityId;

                        var response = documentQuery.Resource;

                        if (response != null)
                        {
                            result = new Tuple<string, T>(query, (T)(dynamic)response);
                        }

                        sllLogger.WriteInformationalTelemetry("CantileverConfiguration.DocDBDetails", "crossPartition:{0}:{1}:Success", activityId, requestCharges);
                    }
                }
                catch (Exception ex)
                {
                    sllLogger.WriteErrorTelemetry("CantileverConfiguration.DocDBDetails", "crossPartition:{0}:{1}:Error", "DocumentDB execute query async failed", ex.Message);
                    return null;
                }

                return result;
            }
        }

        /// <summary>
        /// Get list of documents from doc db
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sllLogger"></param>
        /// <returns></returns>
        public async Task<T> ExecuteUpdateQueryAsync<T>(T query, ISllLogger sllLogger, string id)
        {
            using (OperationQoS operation = new OperationQoS(sllLogger, "ExecuteUpdateQueryAsync"))
            {
                try
                {
                    var documentQuery = await this.container.UpsertItemAsync<T>(query).ConfigureAwait(false);
                    double requestCharges = 0;
                    var activityId = string.Empty;

                    if (documentQuery != null)
                    {
                        requestCharges = documentQuery.RequestCharge;
                        activityId = documentQuery.ActivityId;

                        var response = documentQuery.Resource;

                        if (response != null)
                        {
                            var result = (T)(dynamic)response;
                            return result;
                        }
                    }
                }
                catch (Exception exception)
                {
                    sllLogger.WriteErrorTelemetry("CantileverConfiguration.DocDBDetails", "crossPartition:{0}:{1}:Error", "DocumentDB execute update query async failed", exception);
                    return default(T);
                }

                return default(T);
            }
        }
    }
}