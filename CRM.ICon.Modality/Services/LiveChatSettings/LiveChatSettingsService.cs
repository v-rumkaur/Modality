using System.Data;
using CRM.ICon.Modality.Helpers;
using CRM.ICon.Modality.Helpers.ModalityCosmos;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model.LiveChatSettings;
using CRM.ICon.Modality.Model.LiveChatSettings.Requests;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace CRM.ICon.Modality.Services.LiveChatSettings
{
    public class LiveChatSettingsService : ILiveChatSettingsService
    {
        private readonly IModalityCosmosDbClient cosmosDbClient;
        private readonly string containerId;
        private readonly ITelemetryService _telemetryService;

        private readonly string partitionKey = Constants.LiveChatPartitionKey;

        public LiveChatSettingsService(IModalityCosmosDbClient cosmosDbClient, ITelemetryService telemetryService)
        {
            this.cosmosDbClient =
                cosmosDbClient ?? throw new ArgumentNullException(nameof(cosmosDbClient));
            _telemetryService = telemetryService;
        }

        public async Task<LiveChatRule> CreateRuleAsync(LiveChatRule newRule, string user)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["NewRule"] = JsonConvert.SerializeObject(newRule);
            logProperties["User"] = user;
            _telemetryService.LogTrace<LiveChatSettingsService>("Received CreateRuleAsync request", logProperties);

            // 1. Ensure the rule name (ID) is unique
            if (await GetRuleByNameAsync(newRule.Name) != null)
            {
                throw new InvalidOperationException(
                    $"A rule with name '{newRule.Name}' already exists."
                );
            }

            // 2. Determine evaluation order
            if (newRule.EvaluationOrder is int evalOrder)
            {
                // Handle conflicts by shifting existing rules at or after this order
                var conflictingRules = await GetRulesAtOrAfterOrderAsync(evalOrder);
                foreach (var rule in conflictingRules)
                {
                    rule.EvaluationOrder++;
                    await cosmosDbClient.UpsertItemAsync(containerId, rule);
                }
            }
            else
            {
                // No value provided — assign next available order
                var max = await GetMaxEvaluationOrderAsync();
                newRule.EvaluationOrder = max + 1;
            }

            // 3. Set audit metadata and insert the rule
            newRule.SetAudit(user, true);
            _telemetryService.LogTrace<LiveChatSettingsService>("Creating new LiveChatRule", newRule.ToDictionary());
            await cosmosDbClient.UpsertItemAsync(containerId, newRule);
            return newRule;
        }

        public async Task<LiveChatRule> UpdateRuleAsync(LiveChatRule request, string user)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["UpdateRuleRequest"] = JsonConvert.SerializeObject(request);
            logProperties["User"] = user;
            _telemetryService.LogTrace<LiveChatSettingsService>("Received UpdateRuleAsync request", logProperties);

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
            _telemetryService.LogTrace<LiveChatSettingsService>("Updating LiveChatRule", request.ToDictionary());
            await cosmosDbClient.ReplaceItemAsync(containerId, request.Id, request);
            return request;
        }

        // TODO: Consider making param LiveChatRule
        // public async Task<LiveChatRule?> MatchUserAsync(MatchRuleRequest user)
        // {
        //     _telemetryService.LogTrace<LiveChatSettingsService>("Received MatchUserAsync request", user.ToDictionary());
        //     var query = new QueryDefinition(
        //         @"
        //         SELECT * FROM c
        //         WHERE c.partitionKey = @partitionKey
        //           AND ARRAY_CONTAINS(c.allowedServiceLevels, @serviceLevel)
        //           AND c.allowRestricted = @isRestricted
        //           AND ARRAY_CONTAINS(c.allowedSaps, @sapId)
        //           AND (NOT ARRAY_CONTAINS(c.excludedServiceIds, @serviceId))
        //         ORDER BY c.evaluationOrder ASC
        //     "
        //     ).WithParameter("@partitionKey", partitionKey).WithParameter("@serviceLevel", user.ServiceLevel).WithParameter("@isRestricted", user.IsRestricted).WithParameter("@sapId", user.SapId).WithParameter("@serviceId", user.ServiceId);

        //     var iterator = await cosmosDbClient.QueryItemsIteratorAsync<LiveChatRule>(
        //         containerId,
        //         query
        //     );
        //     while (iterator.HasMoreResults)
        //     {
        //         var response = await iterator.ReadNextAsync();
        //         if (response.Resource.FirstOrDefault() is { } matched)
        //             return matched;
        //     }

        //     return null;
        // }

        // TODO: Consider making param LiveChatRule
        public async Task<LiveChatRule?> MatchUserAsync(MatchRuleRequest user)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["MatchRuleRequest"] = JsonConvert.SerializeObject(user);
            logProperties["ContainerId"] = containerId;
            logProperties["PartitionKey"] = partitionKey;

            _telemetryService.LogTrace<LiveChatSettingsService>("Starting MatchUserAsync request", logProperties);

            try
            {
                var query = new QueryDefinition(
                    @"
            SELECT * FROM c
            WHERE c.partitionKey = @partitionKey
              AND ARRAY_CONTAINS(c.allowedServiceLevels, @serviceLevel)
              AND c.allowRestricted = @isRestricted
              AND ARRAY_CONTAINS(c.allowedSaps, @sapId)
              AND (NOT ARRAY_CONTAINS(c.excludedServiceIds, @serviceId))
            ORDER BY c.evaluationOrder ASC
        "
                ).WithParameter("@partitionKey", partitionKey)
                 .WithParameter("@serviceLevel", user.ServiceLevel)
                 .WithParameter("@isRestricted", user.IsRestricted)
                 .WithParameter("@sapId", user.SapId)
                 .WithParameter("@serviceId", user.ServiceId);

                logProperties["QueryText"] = query.QueryText;
                _telemetryService.LogTrace<LiveChatSettingsService>("Executing Cosmos DB query", logProperties);

                var iterator = await cosmosDbClient.QueryItemsIteratorAsync<LiveChatRule>(
                    containerId,
                    query
                );

                _telemetryService.LogTrace<LiveChatSettingsService>("Query iterator created successfully", logProperties);

                while (iterator.HasMoreResults)
                {
                    _telemetryService.LogTrace<LiveChatSettingsService>("Reading next batch from iterator", logProperties);

                    var response = await iterator.ReadNextAsync();

                    logProperties["ResponseStatusCode"] = response.StatusCode.ToString();
                    logProperties["ResponseRequestCharge"] = response.RequestCharge.ToString();
                    logProperties["ResultCount"] = response.Resource?.Count().ToString() ?? "0";

                    _telemetryService.LogTrace<LiveChatSettingsService>("Received response from Cosmos DB", logProperties);

                    if (response.Resource.FirstOrDefault() is { } matched)
                    {
                        logProperties["MatchedRuleName"] = matched.Name;
                        _telemetryService.LogTrace<LiveChatSettingsService>("Found matching rule", logProperties);
                        return matched;
                    }
                }

                _telemetryService.LogTrace<LiveChatSettingsService>("No matching rules found", logProperties);
                return null;
            }
            catch (CosmosException cosmosEx)
            {
                logProperties["CosmosException"] = cosmosEx.Message;
                logProperties["CosmosStatusCode"] = cosmosEx.StatusCode.ToString();
                logProperties["CosmosSubStatusCode"] = cosmosEx.SubStatusCode.ToString();
                logProperties["CosmosActivityId"] = cosmosEx.ActivityId;

                _telemetryService.LogException<LiveChatSettingsService>(cosmosEx, logProperties, "Cosmos DB error in MatchUserAsync");
                throw;
            }
            catch (Exception ex)
            {
                logProperties["Exception"] = ex.Message;
                _telemetryService.LogException<LiveChatSettingsService>(ex, logProperties, "General error in MatchUserAsync");
                throw;
            }
        }

        public async Task<List<LiveChatRule>> GetAllAsync()
        {
            var query = new QueryDefinition(
                @"
                SELECT * FROM c 
                WHERE c.partitionKey = @partitionKey
                ORDER BY c.evaluationOrder ASC
            "
            ).WithParameter("@partitionKey", partitionKey);

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
            var query = new QueryDefinition(
                @"
                SELECT VALUE MAX(c.evaluationOrder) FROM c 
                WHERE c.partitionKey = @partitionKey
            "
            ).WithParameter("@partitionKey", partitionKey);

            var iterator = await cosmosDbClient.QueryItemsIteratorAsync<int>(containerId, query);

            var response = await iterator.ReadNextAsync();
            return response.Resource.FirstOrDefault();
        }

        private async Task<List<LiveChatRule>> GetRulesAtOrAfterOrderAsync(int order)
        {
            var query = new QueryDefinition(
                @"
                SELECT * FROM c 
                WHERE c.partitionKey = @partitionKey AND c.evaluationOrder >= @order
            "
            ).WithParameter("@partitionKey", partitionKey).WithParameter("@order", order);

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
                _telemetryService.LogTrace<LiveChatSettingsService>($"Rule '{name}' not found.");
                throw;
            }
            catch (Exception ex)
            {
                _telemetryService.LogTrace<LiveChatSettingsService>($"Error getting rule '{name}'", ex.ToDictionary());
                throw;
            }
        }

        public async Task DeleteRuleAsync(LiveChatRule rule)
        {
            try
            {
                await cosmosDbClient.DeleteItemAsync<LiveChatRule>(containerId, rule.Id, partitionKey);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Item not found, nothing to delete
                _telemetryService.LogTrace<LiveChatSettingsService>($"Rule '{rule.Name}' with ID '{rule.Id}' not found for deletion.");
                throw;
            }
            catch (Exception ex)
            {
                _telemetryService.LogTrace<LiveChatSettingsService>($"Error deleting rule '{rule.Name}' with ID '{rule.Id}'", ex.ToDictionary());
                throw;
            }
        }
    }
}
