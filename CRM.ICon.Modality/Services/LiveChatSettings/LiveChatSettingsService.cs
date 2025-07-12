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
        private readonly HttpClient httpClient;

        private readonly IModalityCosmosDbClient cosmosDbClient;
        private readonly string containerId;
        private readonly ITelemetryService _telemetryService;

        private readonly string partitionKey = Constants.LiveChatPartitionKey;

        public LiveChatSettingsService(
            IOptions<ModalityCosmosDbConfiguration> cosmosDbOptions,
            IModalityCosmosDbClient cosmosDbClient,
            HttpClient httpClient,
            ITelemetryService telemetryService
        )
        {
            var cosmosConfig = cosmosDbOptions.Value;
            containerId = cosmosConfig.ContainerIds.LiveChatSettings;
            this.httpClient = httpClient;

            _telemetryService = telemetryService;
            this.cosmosDbClient =
                cosmosDbClient ?? throw new ArgumentNullException(nameof(cosmosDbClient));
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
            await cosmosDbClient.UpsertItemAsync(containerId, newRule);
            return newRule;
        }

        public async Task<LiveChatRule> UpdateRuleAsync(LiveChatRule request, string user)
        {
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

            await cosmosDbClient.ReplaceItemAsync(containerId, request.Id, request);
            return request;
        }
        
        // TODO: Consider making param LiveChatRule
        public async Task<LiveChatRule?> MatchUserAsync(MatchRuleRequest user)
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
            ).WithParameter("@partitionKey", partitionKey).WithParameter("@serviceLevel", user.ServiceLevel).WithParameter("@isRestricted", user.IsRestricted).WithParameter("@sapId", user.SapId).WithParameter("@serviceId", user.ServiceId);

            var iterator = await cosmosDbClient.QueryItemsIteratorAsync<LiveChatRule>(
                containerId,
                query
            );
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                if (response.Resource.FirstOrDefault() is { } matched)
                    return matched;
            }

            return null;
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
                return null;
            }
            catch (Exception ex)
            {
                _telemetryService.LogException<LiveChatSettingsService>(
                    ex,
                    new Dictionary<string, string> { { "RuleName", name } },
                    "Error getting rule by name"
                );
                throw;
            }
        }
    }
}
