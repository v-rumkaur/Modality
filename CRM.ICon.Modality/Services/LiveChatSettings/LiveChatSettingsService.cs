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
            this.logger = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            this.containerId = this.cosmosDbConfiguration.ContainerIds.LiveChatSettings;

            logger.LogTrace<LiveChatSettingsService>($"LiveChatSettingsService initialized with containerId: {containerId}, partitionKey: {partitionKey}");
        }

        public async Task<LiveChatRule?> MatchRuleAsync(MatchRuleRequest user)
        {
            logger.LogTrace<LiveChatSettingsService>($"Starting MatchRuleAsync for user: {JsonConvert.SerializeObject(user)}");

            var queryText = $@"
                SELECT * FROM c
                WHERE c.{partitionPath} = @partitionKey
                  AND ARRAY_CONTAINS(c.AllowedServiceLevels, @serviceLevel)
                  AND c.AllowRestricted = @isRestricted
                  AND ARRAY_CONTAINS(c.AllowedSaps, @sapId)
                  AND (NOT ARRAY_CONTAINS(c.ExcludedServiceIds, @serviceId))
                ORDER BY c.evaluationOrder ASC
            ";

            logger.LogTrace<LiveChatSettingsService>($"MatchRuleAsync query: {queryText}");
            logger.LogTrace<LiveChatSettingsService>($"Query parameters: partitionKey={partitionKey}, serviceLevel={user.ServiceLevel}, isRestricted={user.IsRestricted}, sapId={user.SapId}, serviceId={user.ServiceId}");

            var query = new QueryDefinition(queryText)
                .WithParameter("@partitionKey", partitionKey)
                .WithParameter("@serviceLevel", user.ServiceLevel)
                .WithParameter("@isRestricted", user.IsRestricted)
                .WithParameter("@sapId", user.SapId)
                .WithParameter("@serviceId", user.ServiceId);

            try
            {
                var iterator = await cosmosDbClient.QueryItemsIteratorAsync<LiveChatRule>(containerId, query);
                logger.LogTrace<LiveChatSettingsService>($"Query iterator created successfully for containerId: {containerId}");

                while (iterator.HasMoreResults)
                {
                    logger.LogTrace<LiveChatSettingsService>("Reading next batch from query iterator");
                    var response = await iterator.ReadNextAsync();
                    logger.LogTrace<LiveChatSettingsService>($"Query response received with {response.Resource.Count()} items");

                    if (response.Resource.FirstOrDefault() is { } matched)
                    {
                        logger.LogTrace<LiveChatSettingsService>($"Match found: {JsonConvert.SerializeObject(matched)}");
                        return matched;
                    }
                }

                logger.LogTrace<LiveChatSettingsService>("No matching rule found for user request");
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>($"Error in MatchRuleAsync: {ex.Message}", ex.ToDictionary());
                throw;
            }
        }

        public async Task<IEnumerable<LiveChatRule>> GetAllRulesAsync()
        {
            logger.LogTrace<LiveChatSettingsService>("Starting GetAllRulesAsync");

            var queryText = $"SELECT * FROM c WHERE c.{partitionPath} = @partitionKey ORDER BY c.EvaluationOrder";
            logger.LogTrace<LiveChatSettingsService>($"GetAllRulesAsync query: {queryText}");
            logger.LogTrace<LiveChatSettingsService>($"Query parameters: partitionKey={partitionKey}");

            var query = new QueryDefinition(queryText).WithParameter("@partitionKey", partitionKey);

            try
            {
                var results = await cosmosDbClient.QueryItemsAsync<LiveChatRule>(containerId, query);
                var resultsList = results.ToList();
                logger.LogTrace<LiveChatSettingsService>($"GetAllRulesAsync completed. Found {resultsList.Count} rules");
                return resultsList;
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>($"Error in GetAllRulesAsync: {ex.Message}", ex.ToDictionary());
                throw;
            }
        }

        public async Task<LiveChatRule?> GetRuleByNameAsync(string name)
        {
            logger.LogTrace<LiveChatSettingsService>($"Starting GetRuleByNameAsync for rule name: {name}");
            logger.LogTrace<LiveChatSettingsService>($"Using containerId: {containerId}, partitionKey: {partitionKey}");

            try
            {
                var response = await cosmosDbClient.GetItemByIdAsync<LiveChatRule>(
                    containerId,
                    name,
                    partitionKey
                );

                if (response != null)
                {
                    logger.LogTrace<LiveChatSettingsService>($"Rule found: {JsonConvert.SerializeObject(response)}");
                    return response;
                }
                else
                {
                    logger.LogTrace<LiveChatSettingsService>($"Rule with name '{name}' not found");
                    return null;
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogTrace<LiveChatSettingsService>($"Rule with name '{name}' not found (CosmosException.NotFound)");
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>($"Error in GetRuleByNameAsync for rule '{name}': {ex.Message}", ex.ToDictionary());
                throw;
            }
        }

        public async Task DeleteRuleByNameAsync(string name)
        {
            logger.LogTrace<LiveChatSettingsService>($"Starting DeleteRuleByNameAsync for rule name: {name}");
            logger.LogTrace<LiveChatSettingsService>($"Using containerId: {containerId}, partitionKey: {partitionKey}");

            try
            {
                await cosmosDbClient.DeleteItemAsync(containerId, name, partitionKey);
                logger.LogTrace<LiveChatSettingsService>($"Rule '{name}' deleted successfully");
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>($"Error in DeleteRuleByNameAsync for rule '{name}': {ex.Message}", ex.ToDictionary());
                throw;
            }
        }

        public async Task<LiveChatRule> CreateRuleAsync(LiveChatRule newRule, string user)
        {
            logger.LogTrace<LiveChatSettingsService>($"Starting CreateRuleAsync for rule: {JsonConvert.SerializeObject(newRule)} by user: {user}");

            try
            {
                // 1. Ensure the rule name (ID) is unique
                logger.LogTrace<LiveChatSettingsService>($"Checking if rule with name '{newRule.Name}' already exists");
                var existing = await GetRuleByNameAsync(newRule.Name);
                if (existing != null)
                {
                    logger.LogTrace<LiveChatSettingsService>($"Rule with name '{newRule.Name}' already exists");
                    throw new InvalidOperationException(
                        $"A rule with name '{newRule.Name}' already exists."
                    );
                }

                // 2. Determine evaluation order
                if (newRule.EvaluationOrder is int evalOrder)
                {
                    logger.LogTrace<LiveChatSettingsService>($"Rule has specified evaluation order: {evalOrder}");
                    if (evalOrder < 0)
                        throw new ArgumentOutOfRangeException(
                            nameof(newRule.EvaluationOrder),
                            "EvaluationOrder must be ≥ 0."
                        );

                    logger.LogTrace<LiveChatSettingsService>($"Performing cascading shift for evaluation order: {evalOrder}");
                    await CascadingShiftEfficientAsync(evalOrder, user);
                    newRule.EvaluationOrder = evalOrder;
                }
                else
                {
                    logger.LogTrace<LiveChatSettingsService>("No evaluation order specified, getting max order");
                    var max = await GetMaxEvaluationOrderAsync();
                    newRule.EvaluationOrder = max + 1;
                    logger.LogTrace<LiveChatSettingsService>($"Assigned evaluation order: {newRule.EvaluationOrder}");
                }

                // 3. Set audit metadata and insert the rule
                logger.LogTrace<LiveChatSettingsService>($"Setting audit metadata for user: {user}");
                newRule.SetAudit(user, true);
                
                logger.LogTrace<LiveChatSettingsService>($"Upserting rule to containerId: {containerId}");
                await cosmosDbClient.UpsertItemAsync(containerId, newRule);
                
                logger.LogTrace<LiveChatSettingsService>($"Rule created successfully with final data: {JsonConvert.SerializeObject(newRule)}");
                return newRule;
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>($"Error in CreateRuleAsync: {ex.Message}", ex.ToDictionary());
                throw;
            }
        }

        private async Task CascadingShiftEfficientAsync(
            int startOrder,
            string user,
            string? excludeName = null
        )
        {
            logger.LogTrace<LiveChatSettingsService>($"Starting CascadingShiftEfficientAsync with startOrder: {startOrder}, user: {user}, excludeName: {excludeName}");

            try
            {
                var candidates = (await GetRulesAtOrAfterOrderAsync(startOrder))
                    .Where(r => r.Name != excludeName)
                    .OrderBy(r => r.EvaluationOrder)
                    .ToList();

                logger.LogTrace<LiveChatSettingsService>($"Found {candidates.Count} candidate rules for shifting");

                if (!candidates.Any())
                {
                    logger.LogTrace<LiveChatSettingsService>("No candidates found for shifting, returning");
                    return;
                }

                // Build a map for fast lookups
                var occupied = new Dictionary<int, LiveChatRule>();
                foreach (var r in candidates)
                {
                    if (r.EvaluationOrder.HasValue)
                    {
                        occupied[r.EvaluationOrder.Value] = r;
                        logger.LogTrace<LiveChatSettingsService>($"Added rule '{r.Name}' with order {r.EvaluationOrder.Value} to occupied map");
                    }
                    else
                        throw new InvalidOperationException($"Rule '{r.Name}' has null EvaluationOrder.");
                }

                var shifts = new List<LiveChatRule>();
                int current = startOrder;

                logger.LogTrace<LiveChatSettingsService>($"Starting shift loop from order: {current}");

                while (occupied.ContainsKey(current))
                {
                    var rule = occupied[current];
                    logger.LogTrace<LiveChatSettingsService>($"Processing rule '{rule.Name}' at order {current}");

                    int next = current + 1;

                    // If someone is already at the next slot, we'll have to shift them too
                    if (!occupied.ContainsKey(next))
                    {
                        logger.LogTrace<LiveChatSettingsService>($"Next slot {next} is free, moving rule '{rule.Name}' to order {next}");
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
                        logger.LogTrace<LiveChatSettingsService>($"Next slot {next} is occupied, cascading shift for rule '{rule.Name}'");
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

                logger.LogTrace<LiveChatSettingsService>($"Shift planning completed. {shifts.Count} rules need to be updated");

                if (shifts.Count > 0)
                {
                    logger.LogTrace<LiveChatSettingsService>("Executing parallel upserts for shifted rules");
                    await Task.WhenAll(
                        shifts.Select(r => cosmosDbClient.UpsertItemAsync(containerId, r))
                    );
                    logger.LogTrace<LiveChatSettingsService>("All shifted rules updated successfully");
                }
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>($"Error in CascadingShiftEfficientAsync: {ex.Message}", ex.ToDictionary());
                throw;
            }
        }

        private async Task<List<LiveChatRule>> GetRulesAtOrAfterOrderAsync(int order)
        {
            logger.LogTrace<LiveChatSettingsService>($"Starting GetRulesAtOrAfterOrderAsync for order >= {order}");

            var queryText = $"SELECT * FROM c WHERE c.{partitionPath} = @partitionKey AND c.EvaluationOrder >= @order";
            logger.LogTrace<LiveChatSettingsService>($"GetRulesAtOrAfterOrderAsync query: {queryText}");
            logger.LogTrace<LiveChatSettingsService>($"Query parameters: partitionKey={partitionKey}, order={order}");

            var query = new QueryDefinition(queryText)
                .WithParameter("@partitionKey", partitionKey)
                .WithParameter("@order", order);

            try
            {
                var iterator = await cosmosDbClient.QueryItemsIteratorAsync<LiveChatRule>(
                    containerId,
                    query
                );
                var results = new List<LiveChatRule>();
                while (iterator.HasMoreResults)
                {
                    var batch = (await iterator.ReadNextAsync()).Resource;
                    results.AddRange(batch);
                    logger.LogTrace<LiveChatSettingsService>($"Added {batch.Count()} rules from batch");
                }

                logger.LogTrace<LiveChatSettingsService>($"GetRulesAtOrAfterOrderAsync completed. Found {results.Count} rules");
                return results;
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>($"Error in GetRulesAtOrAfterOrderAsync: {ex.Message}", ex.ToDictionary());
                throw;
            }
        }

        public async Task<int> GetMaxEvaluationOrderAsync()
        {
            logger.LogTrace<LiveChatSettingsService>("Starting GetMaxEvaluationOrderAsync");

            var queryText = $"SELECT VALUE MAX(c.EvaluationOrder) FROM c WHERE c.{partitionPath} = @partitionKey";
            logger.LogTrace<LiveChatSettingsService>($"GetMaxEvaluationOrderAsync query: {queryText}");
            logger.LogTrace<LiveChatSettingsService>($"Query parameters: partitionKey={partitionKey}");

            var query = new QueryDefinition(queryText).WithParameter("@partitionKey", partitionKey);

            try
            {
                var iterator = await cosmosDbClient.QueryItemsIteratorAsync<int>(containerId, query);
                var response = await iterator.ReadNextAsync();
                var maxOrder = response.Resource.FirstOrDefault();
                
                logger.LogTrace<LiveChatSettingsService>($"GetMaxEvaluationOrderAsync completed. Max order: {maxOrder}");
                return maxOrder;
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>($"Error in GetMaxEvaluationOrderAsync: {ex.Message}", ex.ToDictionary());
                throw;
            }
        }

        public async Task UpdateRuleAsync(UpdateRuleRequest request, string updatedBy)
        {
            logger.LogTrace<LiveChatSettingsService>($"Starting UpdateRuleAsync for rule: {request.Name} by user: {updatedBy}");
            logger.LogTrace<LiveChatSettingsService>($"Update request: {JsonConvert.SerializeObject(request)}");

            try
            {
                // 1. Retrieve existing rule by name
                logger.LogTrace<LiveChatSettingsService>($"Retrieving existing rule: {request.Name}");
                var existing = await GetRuleByNameAsync(request.Name);
                if (existing == null)
                {
                    logger.LogTrace<LiveChatSettingsService>($"Rule '{request.Name}' not found for update");
                    throw new InvalidOperationException($"Rule '{request.Name}' not found.");
                }

                // 2. Keep track of the original evaluation order
                int? originalOrder = existing.EvaluationOrder;
                logger.LogTrace<LiveChatSettingsService>($"Original evaluation order: {originalOrder}");

                // 3. Apply patch values from the request
                logger.LogTrace<LiveChatSettingsService>("Applying patch values from request");
                request.PatchToDomainModel(existing);
                logger.LogTrace<LiveChatSettingsService>($"Rule after patching: {JsonConvert.SerializeObject(existing)}");

                // 4. Resolve evaluation order conflicts if the value changed
                if (existing.EvaluationOrder is int newOrder &&
                    originalOrder is int oldOrder &&
                    newOrder != oldOrder)
                {
                    logger.LogTrace<LiveChatSettingsService>($"Evaluation order changed from {oldOrder} to {newOrder}");
                    if (newOrder < 0)
                        throw new ArgumentOutOfRangeException(nameof(existing.EvaluationOrder), "EvaluationOrder must be ≥ 0.");

                    logger.LogTrace<LiveChatSettingsService>($"Performing cascading shift for new order: {newOrder}");
                    await CascadingShiftEfficientAsync(newOrder, updatedBy, excludeName: existing.Name);
                }

                // 5. Set audit fields
                logger.LogTrace<LiveChatSettingsService>($"Setting audit fields for user: {updatedBy}");
                existing.SetAudit(updatedBy, isNew: false);

                // 6. Save the updated rule
                logger.LogTrace<LiveChatSettingsService>($"Upserting updated rule to containerId: {containerId}");
                await cosmosDbClient.UpsertItemAsync(containerId, existing);
                
                logger.LogTrace<LiveChatSettingsService>($"Rule '{request.Name}' updated successfully");
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>($"Error in UpdateRuleAsync for rule '{request.Name}': {ex.Message}", ex.ToDictionary());
                throw;
            }
        }
    }
}