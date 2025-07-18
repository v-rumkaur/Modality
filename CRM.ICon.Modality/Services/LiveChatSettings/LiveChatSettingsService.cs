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
        private readonly ITelemetryService logger;
        private readonly string partitionKey = Constants.LiveChat.PartitionKeyValue;
        private readonly string partitionPath = Constants.LiveChat.PartitionKeyPath;

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
            this.logger = telemetryService;
            this.containerId = this.cosmosDbConfiguration.ContainerIds.LiveChatSettings;
        }

        public async Task<LiveChatRule?> MatchRuleAsync(MatchRuleRequest user)
        {
            var query = new QueryDefinition(
                $@"
                SELECT * FROM c
                WHERE c.{partitionPath} = @partitionKey
                  AND ARRAY_CONTAINS(c.AllowedServiceLevels, @serviceLevel)
                  AND c.AllowRestricted = @isRestricted
                  AND ARRAY_CONTAINS(c.AllowedSaps, @sapId)
                  AND (NOT ARRAY_CONTAINS(c.ExcludedServiceIds, @serviceId))
                ORDER BY c.evaluationOrder ASC
            "
            ).WithParameter(
                "@partitionKey",
                partitionKey
            ).WithParameter("@serviceLevel", user.ServiceLevel).WithParameter("@isRestricted", user.IsRestricted).WithParameter("@sapId", user.SapId).WithParameter("@serviceId", user.ServiceId);

            var iterator = await cosmosDbClient.QueryItemsIteratorAsync<LiveChatRule>(containerId, query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                if (response.Resource.FirstOrDefault() is { } matched)
                    return matched;
            }
            return null;
        }

        public async Task<IEnumerable<LiveChatRule>> GetAllRulesAsync()
        {
            var query = new QueryDefinition(
                $"SELECT * FROM c WHERE c.{partitionPath} = @partitionKey ORDER BY c.EvaluationOrder"
            ).WithParameter("@partitionKey", partitionKey);

            return await cosmosDbClient.QueryItemsAsync<LiveChatRule>(containerId, query);
        }

        public async Task<LiveChatRule?> GetRuleByNameAsync(string name)
        {
            try
            {
                var response = await cosmosDbClient.GetItemByIdAsync<LiveChatRule>(
                    containerId,
                    name,
                    partitionKey
                );
                return response ?? null;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task DeleteRuleByNameAsync(string name)
        {
            await cosmosDbClient.DeleteItemAsync(containerId, name, partitionKey);
        }

        public async Task<LiveChatRule> CreateRuleAsync(LiveChatRule newRule, string user)
        {
            // 1. Ensure the rule name (ID) is unique
            var existing = await GetRuleByNameAsync(newRule.Name);
            if (existing != null)
                throw new InvalidOperationException(
                    $"A rule with name '{newRule.Name}' already exists."
                );

            // 2. Determine evaluation order
            if (newRule.EvaluationOrder is int evalOrder)
            {
                if (evalOrder < 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(newRule.EvaluationOrder),
                        "EvaluationOrder must be ≥ 0."
                    );

                await CascadingShiftEfficientAsync(evalOrder, user);
                newRule.EvaluationOrder = evalOrder;
            }
            else
            {
                var max = await GetMaxEvaluationOrderAsync();
                newRule.EvaluationOrder = max + 1;
            }

            // 3. Set audit metadata and insert the rule
            newRule.SetAudit(user, true);
            await cosmosDbClient.UpsertItemAsync(containerId, newRule);
            return newRule;
        }

        private async Task CascadingShiftEfficientAsync(
            int startOrder,
            string user,
            string? excludeName = null
        )
        {
            var candidates = (await GetRulesAtOrAfterOrderAsync(startOrder))
                .Where(r => r.Name != excludeName)
                .OrderBy(r => r.EvaluationOrder)
                .ToList();

            if (!candidates.Any())
                return;

            // Build a map for fast lookups
            var occupied = new Dictionary<int, LiveChatRule>();
            foreach (var r in candidates)
            {
                if (r.EvaluationOrder.HasValue)
                    occupied[r.EvaluationOrder.Value] = r;
                else
                    throw new InvalidOperationException($"Rule '{r.Name}' has null EvaluationOrder.");
            }

            var shifts = new List<LiveChatRule>();
            int current = startOrder;

            while (occupied.ContainsKey(current))
            {
                var rule = occupied[current];

                int next = current + 1;

                // If someone is already at the next slot, we'll have to shift them too
                if (!occupied.ContainsKey(next))
                {
                    // Assign new order and add to shifts
                    rule.EvaluationOrder = next;
                    rule.SetAudit(user, isNew: false);

                    shifts.Add(rule);

                    // Move it in the map
                    occupied.Remove(current);
                    occupied[next] = rule;

                    // Done! No more cascading conflict
                    break;
                }
                else
                {
                    // Shift this rule, and keep cascading
                    rule.EvaluationOrder = next;
                    rule.SetAudit(user, isNew: false);

                    shifts.Add(rule);

                    // Move it in the map
                    occupied.Remove(current);
                    occupied[next] = rule;

                    current = next; // Continue shifting forward
                }
            }

            if (shifts.Count > 0)
                await Task.WhenAll(
                    shifts.Select(r => cosmosDbClient.UpsertItemAsync(containerId, r))
                );
        }



        private async Task<List<LiveChatRule>> GetRulesAtOrAfterOrderAsync(int order)
        {
            var query = new QueryDefinition(
                $"SELECT * FROM c WHERE c.{partitionPath} = @partitionKey AND c.EvaluationOrder >= @order            "
            )
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

        public async Task<int> GetMaxEvaluationOrderAsync()
        {
            var query = new QueryDefinition(
                $"SELECT VALUE MAX(c.EvaluationOrder) FROM c WHERE c.{partitionPath} = @partitionKey"
            ).WithParameter("@partitionKey", partitionKey);

            var iterator = await cosmosDbClient.QueryItemsIteratorAsync<int>(containerId, query);

            var response = await iterator.ReadNextAsync();
            return response.Resource.FirstOrDefault();
        }

        public async Task UpdateRuleAsync(UpdateRuleRequest request, string updatedBy)
        {
            // 1. Retrieve existing rule by name
            var existing = await GetRuleByNameAsync(request.Name);
            if (existing == null)
                throw new InvalidOperationException($"Rule '{request.Name}' not found.");

            // 2. Keep track of the original evaluation order
            int? originalOrder = existing.EvaluationOrder;

            // 3. Apply patch values from the request
            request.PatchToDomainModel(existing);

            // 4. Resolve evaluation order conflicts if the value changed
            if (existing.EvaluationOrder is int newOrder &&
                originalOrder is int oldOrder &&
                newOrder != oldOrder)
            {
                if (newOrder < 0)
                    throw new ArgumentOutOfRangeException(nameof(existing.EvaluationOrder), "EvaluationOrder must be ≥ 0.");

                await CascadingShiftEfficientAsync(newOrder, updatedBy, excludeName: existing.Name);
            }

            // 5. Set audit fields
            existing.SetAudit(updatedBy, isNew: false);

            // 6. Save the updated rule
            await cosmosDbClient.UpsertItemAsync(containerId, existing);
        }


    }
}
