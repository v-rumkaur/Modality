using System.Data;
using CRM.ICon.Modality.Helpers;
using CRM.ICon.Modality.Helpers.ModalityCosmos;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model.LiveChatSettings;
using CRM.ICon.Modality.Model.LiveChatSettings.Requests;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CRM.ICon.Modality.Services.LiveChatSettings
{
    public class LiveChatSettingsService : ILiveChatSettingsService
    {
        private readonly ModalityCosmosDbConfiguration cosmosDbConfiguration;
        private readonly IModalityCosmosDbClient cosmosDbClient;
        private readonly string containerId;
        private readonly ITelemetryService telemetryService;
        private readonly string partitionKey = Constants.LiveChat.PartitionKey;

        public LiveChatSettingsService(
            IOptions<ModalityCosmosDbConfiguration> cosmosDbConfiguration,
            IModalityCosmosDbClient cosmosDbClient,
            ITelemetryService telemetryService
        )
        {
            this.cosmosDbConfiguration =
                cosmosDbConfiguration?.Value
                ?? throw new ArgumentNullException(nameof(cosmosDbConfiguration));
            this.cosmosDbClient =
                cosmosDbClient ?? throw new ArgumentNullException(nameof(cosmosDbClient));
            this.telemetryService = telemetryService;
            this.containerId = this.cosmosDbConfiguration.ContainerIds.LiveChatSettings;
        }

        public async Task<LiveChatRule> CreateRuleAsync(LiveChatRule newRule, string user)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["NewRule"] = JsonConvert.SerializeObject(newRule);
            logProperties["User"] = user;
            telemetryService.LogTrace<LiveChatSettingsService>(
                "Received CreateRuleAsync request",
                logProperties
            );

            if (await GetRuleByNameAsync(newRule.Name) != null)
            {
                throw new InvalidOperationException(
                    $"A rule with name '{newRule.Name}' already exists."
                );
            }

            if (newRule.EvaluationOrder is int evalOrder)
            {
                var conflictingRules = await GetRulesAtOrAfterOrderAsync(evalOrder);
                foreach (var rule in conflictingRules)
                {
                    rule.EvaluationOrder++;
                    await cosmosDbClient.UpsertItemAsync(containerId, rule);
                }
            }
            else
            {
                var max = await GetMaxEvaluationOrderAsync();
                newRule.EvaluationOrder = max + 1;
            }

            newRule.SetAudit(user, true);
            telemetryService.LogTrace<LiveChatSettingsService>(
                "Creating new LiveChatRule",
                newRule.ToDictionary()
            );
            await cosmosDbClient.UpsertItemAsync(containerId, newRule);
            return newRule;
        }

        public async Task<LiveChatRule> UpdateRuleAsync(LiveChatRule request, string user)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["UpdateRuleRequest"] = JsonConvert.SerializeObject(request);
            logProperties["User"] = user;
            telemetryService.LogTrace<LiveChatSettingsService>(
                "Received UpdateRuleAsync request",
                logProperties
            );

            // Evaluation order conflict resolution
            if (request.EvaluationOrder is int newOrder)
            {
                // Fetch rules at or after the new evaluation order, excluding the one being updated
                var conflictingRules = (await GetRulesAtOrAfterOrderAsync(newOrder))
                    .Where(r => r.Name != request.Name)
                    .OrderByDescending(r => r.EvaluationOrder);

                foreach (var rule in conflictingRules)
                {
                    rule.EvaluationOrder++;
                    rule.SetAudit(user, isNew: false);
                    await cosmosDbClient.ReplaceItemAsync(containerId, rule.Id, rule);
                }
            }

            request.SetAudit(user, isNew: false);
            telemetryService.LogTrace<LiveChatSettingsService>(
                "Updating LiveChatRule",
                request.ToDictionary()
            );
            await cosmosDbClient.ReplaceItemAsync(containerId, request.Id, request);
            return request;
        }

        public async Task<LiveChatRule?> MatchUserAsync(MatchRuleRequest user)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["MatchRuleRequest"] = JsonConvert.SerializeObject(user);
            logProperties["ContainerId"] = containerId;
            logProperties["PartitionKey"] = partitionKey;

            telemetryService.LogTrace<LiveChatSettingsService>(
                "Starting MatchUserAsync request",
                logProperties
            );

            try
            {
                var query = BuildMatchUserQuery(user);
                var iterator = await cosmosDbClient.QueryItemsIteratorAsync<LiveChatRule>(
                    containerId,
                    query
                );

                while (iterator.HasMoreResults)
                {
                    telemetryService.LogTrace<LiveChatSettingsService>(
                        "Executing Cosmos DB query",
                        iterator.ToDictionary()
                    );

                    var response = await iterator.ReadNextAsync();

                    logProperties["ResponseStatusCode"] = response.StatusCode.ToString();
                    logProperties["ResponseRequestCharge"] = response.RequestCharge.ToString();
                    logProperties["ResultCount"] = response.Resource?.Count().ToString() ?? "0";

                    telemetryService.LogTrace<LiveChatSettingsService>(
                        "Received response from Cosmos DB",
                        logProperties
                    );

                    if (response.Resource.FirstOrDefault() is { } matched)
                    {
                        logProperties["MatchedRuleName"] = matched.Name;
                        telemetryService.LogTrace<LiveChatSettingsService>(
                            "Found matching rule",
                            logProperties
                        );
                        return matched;
                    }
                }

                telemetryService.LogTrace<LiveChatSettingsService>(
                    "No matching rules found",
                    logProperties
                );
                return null;
            }
            catch (CosmosException cosmosEx)
            {
                telemetryService.LogTrace<LiveChatSettingsService>(
                    "Cosmos DB error in MatchUserAsync",
                    cosmosEx.ToDictionary()
                );
                telemetryService.LogException<LiveChatSettingsService>(
                    cosmosEx,
                    logProperties,
                    "Cosmos DB error in MatchUserAsync"
                );
                throw;
            }
            catch (Exception ex)
            {
                telemetryService.LogTrace<LiveChatSettingsService>(
                    "Cosmos DB error in MatchUserAsync",
                    ex.ToDictionary()
                );
                telemetryService.LogException<LiveChatSettingsService>(
                    ex,
                    logProperties,
                    "General error in MatchUserAsync"
                );
                throw;
            }
        }

        public async Task<List<LiveChatRule>> GetAllAsync()
        {
            var query = new QueryDefinition(Constants.LiveChat.Queries.GET_ALL_RULES).WithParameter(
                "@partitionKey",
                partitionKey
            );

            var iterator = await cosmosDbClient.QueryItemsIteratorAsync<LiveChatRule>(
                containerId,
                query
            );

            var results = new List<LiveChatRule>();

            while (iterator.HasMoreResults)
            {
                results.AddRange((await iterator.ReadNextAsync()).Resource);
            }

            return results;
        }

        public async Task<int> GetMaxEvaluationOrderAsync()
        {
            var query = new QueryDefinition(Constants.LiveChat.Queries.GET_MAX_ORDER).WithParameter(
                "@partitionKey",
                partitionKey
            );

            var iterator = await cosmosDbClient.QueryItemsIteratorAsync<int>(containerId, query);

            var response = await iterator.ReadNextAsync();
            return response.Resource.FirstOrDefault();
        }

        private async Task<List<LiveChatRule>> GetRulesAtOrAfterOrderAsync(int order)
        {
            var query = new QueryDefinition(Constants.LiveChat.Queries.GET_RULES_AT_ORDER)
                .WithParameter("@partitionKey", partitionKey)
                .WithParameter("@order", order);

            var iterator = await cosmosDbClient.QueryItemsIteratorAsync<LiveChatRule>(
                containerId,
                query
            );
            var results = new List<LiveChatRule>();
            while (iterator.HasMoreResults)
            {
                results.AddRange((await iterator.ReadNextAsync()).Resource);
            }
            return results;
        }

        public async Task<LiveChatRule?> GetRuleByNameAsync(string name)
        {
            try
            {
                return await cosmosDbClient.GetItemByIdAsync<LiveChatRule>(
                    containerId,
                    name,
                    partitionKey
                );
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                telemetryService.LogTrace<LiveChatSettingsService>($"Rule '{name}' not found.");
                throw;
            }
            catch (Exception ex)
            {
                telemetryService.LogTrace<LiveChatSettingsService>(
                    $"Error getting rule '{name}'",
                    ex.ToDictionary()
                );
                throw;
            }
        }

        public async Task DeleteRuleAsync(LiveChatRule rule)
        {
            try
            {
                await cosmosDbClient.DeleteItemAsync<LiveChatRule>(
                    containerId,
                    rule.Id,
                    partitionKey
                );
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Item not found, nothing to delete
                telemetryService.LogTrace<LiveChatSettingsService>(
                    $"Rule '{rule.Name}' with ID '{rule.Id}' not found for deletion."
                );
                throw;
            }
            catch (Exception ex)
            {
                telemetryService.LogTrace<LiveChatSettingsService>(
                    $"Error deleting rule '{rule.Name}' with ID '{rule.Id}'",
                    ex.ToDictionary()
                );
                throw;
            }
        }

        private QueryDefinition BuildMatchUserQuery(MatchRuleRequest user)
        {
            return new QueryDefinition(Constants.LiveChat.Queries.MATCH_USER)
                .WithParameter("@partitionKey", partitionKey)
                .WithParameter("@serviceLevel", user.ServiceLevel)
                .WithParameter("@isRestricted", user.IsRestricted)
                .WithParameter("@sapId", user.SapId)
                .WithParameter("@serviceId", user.ServiceId);
        }
    }
}
