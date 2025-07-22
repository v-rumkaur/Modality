using System.Data;
using CRM.ICon.Modality.Helpers;
using CRM.ICon.Modality.Helpers.ModalityCosmos;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model.LiveChatSettings;
using CRM.ICon.Modality.Model.LiveChatSettings.Requests;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using static CRM.ICon.Modality.LiveChatConstants;

namespace CRM.ICon.Modality.Services.LiveChatSettings
{
    public class LiveChatSettingsService : ILiveChatSettingsService
    {
        private readonly ModalityCosmosDbConfiguration cosmosDbConfiguration;
        private readonly IModalityCosmosDbClient cosmosDbClient;
        private readonly string containerId;
        private readonly ITelemetryService logger;

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
            this.logger =
                telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            this.containerId = this.cosmosDbConfiguration.ContainerIds.LiveChatSettings;

            logger.LogTrace<LiveChatSettingsService>(
                $"LiveChatSettingsService initialized with containerId: {containerId}, partitionKey: {PartitionKeyValue}, and partitionPath: {PartitionKeyPath}"
            );
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LiveChatRule>> GetAllRulesAsync()
        {
            logger.LogTrace<LiveChatSettingsService>("Starting GetAllRulesAsync");

            var query = new QueryDefinition(Queries.GetAllRules).WithParameter(
                "@partitionKey",
                PartitionKeyValue
            );

            logger.LogTrace<LiveChatSettingsService>(
                $"GetAllRulesAsync query: {query.QueryText}, parameters: partitionKey={PartitionKeyValue}"
            );

            try
            {
                var results = await cosmosDbClient.QueryItemsAsync<LiveChatRule>(
                    containerId,
                    query
                );
                var resultsList = results.ToList();
                logger.LogTrace<LiveChatSettingsService>(
                    $"GetAllRulesAsync completed. Found {resultsList.Count} rules"
                );
                return resultsList;
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>(
                    $"Error in GetAllRulesAsync: {ex.Message}",
                    ex.ToDictionary()
                );
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<LiveChatRule?> GetRuleByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Rule name cannot be null or empty.", nameof(name));

            logger.LogTrace<LiveChatSettingsService>(
                $"Starting GetRuleByNameAsync for rule name: {name}"
            );
            logger.LogTrace<LiveChatSettingsService>(
                $"Using containerId: {containerId}, partitionKey: {PartitionKeyValue}"
            );

            try
            {
                var response = await cosmosDbClient.GetItemByIdAsync<LiveChatRule>(
                    containerId,
                    name,
                    PartitionKeyValue
                );

                if (response != null)
                {
                    logger.LogTrace<LiveChatSettingsService>(
                        $"Rule found: {JsonConvert.SerializeObject(response)}"
                    );
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
                logger.LogTrace<LiveChatSettingsService>(
                    $"Rule with name '{name}' not found (CosmosException.NotFound)"
                );
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>(
                    $"Error in GetRuleByNameAsync for rule '{name}': {ex.Message}",
                    ex.ToDictionary()
                );
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<LiveChatRule?> MatchRuleAsync(MatchRuleRequest user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            logger.LogTrace<LiveChatSettingsService>(
                $"Starting MatchRuleAsync for user: {JsonConvert.SerializeObject(user)}"
            );

            var query = new QueryDefinition(Queries.MatchRule)
                .WithParameter("@partitionKey", PartitionKeyValue)
                .WithParameter("@serviceLevel", user.ServiceLevel)
                .WithParameter("@isRestricted", user.IsRestricted)
                .WithParameter("@sapId", user.SapId)
                .WithParameter("@serviceId", user.ServiceId);

            // make into obj for easier parsing..?
            logger.LogTrace<LiveChatSettingsService>(
                $"MatchRuleAsync query: {query.QueryText}, parameters: partitionKey={PartitionKeyValue}, serviceLevel={user.ServiceLevel}, isRestricted={user.IsRestricted}, sapId={user.SapId}, serviceId={user.ServiceId}"
            );

            try
            {
                var iterator = await cosmosDbClient.QueryItemsIteratorAsync<LiveChatRule>(
                    containerId,
                    query
                );
                logger.LogTrace<LiveChatSettingsService>(
                    $"Query iterator created successfully for containerId: {containerId}"
                );

                while (iterator.HasMoreResults)
                {
                    logger.LogTrace<LiveChatSettingsService>(
                        "Reading next batch from query iterator"
                    );
                    var response = await iterator.ReadNextAsync();
                    logger.LogTrace<LiveChatSettingsService>(
                        $"Query response received with {response.Resource.Count()} items"
                    );

                    if (response.Resource.FirstOrDefault() is { } matched)
                    {
                        logger.LogTrace<LiveChatSettingsService>(
                            $"Match found: {JsonConvert.SerializeObject(matched)}"
                        );
                        return matched;
                    }
                }

                logger.LogTrace<LiveChatSettingsService>("No matching rule found for user request");
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>(
                    $"Error in MatchRuleAsync: {ex.Message}",
                    ex.ToDictionary()
                );
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<LiveChatRule> CreateRuleAsync(LiveChatRule newRule, string user)
        {
            if (newRule == null)
                throw new ArgumentNullException(nameof(newRule));
            if (string.IsNullOrWhiteSpace(user))
                throw new ArgumentException("User cannot be null or empty.", nameof(user));

            logger.LogTrace<LiveChatSettingsService>(
                $"Starting CreateRuleAsync for rule: {JsonConvert.SerializeObject(newRule)} by user: {user}"
            );

            try
            {
                // Ensure the rule name (ID) does not already exist
                logger.LogTrace<LiveChatSettingsService>(
                    $"Checking if rule with name '{newRule.Name}' already exists"
                );
                var existing = await GetRuleByNameAsync(newRule.Name);
                if (existing != null)
                {
                    logger.LogTrace<LiveChatSettingsService>(
                        $"Rule with name '{newRule.Name}' already exists"
                    );
                    throw new InvalidOperationException(
                        $"A rule with name '{newRule.Name}' already exists."
                    );
                }

                // Determine evaluation order
                if (newRule.EvaluationOrder is int evalOrder)
                {
                    // Validate evaluation order before processing
                    ValidateEvaluationOrder(evalOrder);
                    
                    logger.LogTrace<LiveChatSettingsService>(
                        $"Rule has specified evaluation order: {evalOrder}"
                    );
                    logger.LogTrace<LiveChatSettingsService>(
                        $"Performing cascading shift for evaluation order: {evalOrder}"
                    );
                    await CascadingShiftEfficientAsync(evalOrder, user);
                    newRule.EvaluationOrder = evalOrder;
                }
                else
                {
                    logger.LogTrace<LiveChatSettingsService>(
                        "No evaluation order specified, getting max order"
                    );
                    var max = await GetMaxEvaluationOrderAsync();
                    newRule.EvaluationOrder = max + EvaluationOrderIncrement;
                    logger.LogTrace<LiveChatSettingsService>(
                        $"Assigned evaluation order: {newRule.EvaluationOrder}"
                    );
                }

                // 3. Set audit metadata and insert the rule
                logger.LogTrace<LiveChatSettingsService>(
                    $"Setting audit metadata for user: {user}"
                );
                newRule.SetAudit(user, true);

                logger.LogTrace<LiveChatSettingsService>(
                    $"Upserting rule to containerId: {containerId}"
                );
                await cosmosDbClient.UpsertItemAsync(containerId, newRule);

                logger.LogTrace<LiveChatSettingsService>(
                    $"Rule created successfully with final data: {JsonConvert.SerializeObject(newRule)}"
                );
                return newRule;
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>(
                    $"Error in CreateRuleAsync: {ex.Message}",
                    ex.ToDictionary()
                );
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task UpdateRuleAsync(UpdateRuleRequest request, string updatedBy)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(updatedBy))
                throw new ArgumentException(
                    "UpdatedBy cannot be null or empty.",
                    nameof(updatedBy)
                );

            logger.LogTrace<LiveChatSettingsService>(
                $"Starting UpdateRuleAsync for rule: {request.Name} by user: {updatedBy}"
            );
            logger.LogTrace<LiveChatSettingsService>(
                $"Update request: {JsonConvert.SerializeObject(request)}"
            );

            try
            {
                // 1. Retrieve existing rule by name
                logger.LogTrace<LiveChatSettingsService>(
                    $"Retrieving existing rule: {request.Name}"
                );
                var existing = await GetRuleByNameAsync(request.Name);
                if (existing == null)
                {
                    logger.LogTrace<LiveChatSettingsService>(
                        $"Rule '{request.Name}' not found for update"
                    );
                    throw new InvalidOperationException($"Rule '{request.Name}' not found.");
                }

                // 2. Keep track of the original evaluation order
                int? originalOrder = existing.EvaluationOrder;
                logger.LogTrace<LiveChatSettingsService>(
                    $"Original evaluation order: {originalOrder}"
                );

                // 3. Apply patch values from the request
                logger.LogTrace<LiveChatSettingsService>("Applying patch values from request");
                request.PatchToDomainModel(existing);
                logger.LogTrace<LiveChatSettingsService>(
                    $"Rule after patching: {JsonConvert.SerializeObject(existing)}"
                );

                // 4. Resolve evaluation order conflicts if the value changed
                if (
                    existing.EvaluationOrder is int newOrder
                    && originalOrder is int oldOrder
                    && newOrder != oldOrder
                )
                {
                    // No validation needed here - PatchToDomainModel already prevents negative values
                    
                    logger.LogTrace<LiveChatSettingsService>(
                        $"Evaluation order changed from {oldOrder} to {newOrder}"
                    );
                    logger.LogTrace<LiveChatSettingsService>(
                        $"Performing cascading shift for new order: {newOrder}"
                    );
                    await CascadingShiftEfficientAsync(
                        newOrder,
                        updatedBy,
                        excludeName: existing.Name
                    );
                }

                // 5. Set audit fields
                logger.LogTrace<LiveChatSettingsService>(
                    $"Setting audit fields for user: {updatedBy}"
                );
                existing.SetAudit(updatedBy, isNew: false);

                // 6. Save the updated rule
                logger.LogTrace<LiveChatSettingsService>(
                    $"Upserting updated rule to containerId: {containerId}"
                );
                await cosmosDbClient.UpsertItemAsync(containerId, existing);

                logger.LogTrace<LiveChatSettingsService>(
                    $"Rule '{request.Name}' updated successfully"
                );
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>(
                    $"Error in UpdateRuleAsync for rule '{request.Name}': {ex.Message}",
                    ex.ToDictionary()
                );
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DeleteRuleByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Rule name cannot be null or empty.", nameof(name));

            logger.LogTrace<LiveChatSettingsService>(
                $"Starting DeleteRuleByNameAsync for rule name: {name}"
            );
            logger.LogTrace<LiveChatSettingsService>(
                $"Using containerId: {containerId}, PartitionKeyValue: {PartitionKeyValue}"
            );

            try
            {
                // Check if rule exists before attempting to delete
                var existing = await GetRuleByNameAsync(name);
                if (existing == null)
                {
                    logger.LogTrace<LiveChatSettingsService>(
                        $"Rule '{name}' not found for deletion"
                    );
                    throw new InvalidOperationException($"Rule '{name}' not found.");
                }

                await cosmosDbClient.DeleteItemAsync(containerId, name, PartitionKeyValue);
                logger.LogTrace<LiveChatSettingsService>($"Rule '{name}' deleted successfully");
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>(
                    $"Error in DeleteRuleByNameAsync for rule '{name}': {ex.Message}",
                    ex.ToDictionary()
                );
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetMaxEvaluationOrderAsync()
        {
            logger.LogTrace<LiveChatSettingsService>("Starting GetMaxEvaluationOrderAsync");

            var query = new QueryDefinition(Queries.GetMaxEvaluationOrder).WithParameter(
                "@partitionKey",
                PartitionKeyValue
            );

            logger.LogTrace<LiveChatSettingsService>(
                $"GetMaxEvaluationOrderAsync query: {query.QueryText}, parameters: partitionKey={PartitionKeyValue}"
            );

            try
            {
                var iterator = await cosmosDbClient.QueryItemsIteratorAsync<int>(
                    containerId,
                    query
                );
                var response = await iterator.ReadNextAsync();
                var maxOrder = response.Resource.FirstOrDefault();

                logger.LogTrace<LiveChatSettingsService>(
                    $"GetMaxEvaluationOrderAsync completed. Max order: {maxOrder}"
                );
                return maxOrder;
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>(
                    $"Error in GetMaxEvaluationOrderAsync: {ex.Message}",
                    ex.ToDictionary()
                );
                throw;
            }
        }

        /// <summary>
        /// Shifts existing rules to accommodate a new rule at the specified evaluation order.
        /// Rules at or after the target order are moved up by one position to prevent conflicts.
        /// </summary>
        /// <param name="startOrder">The evaluation order where a new rule will be inserted.</param>
        /// <param name="user">The user performing the operation (for audit purposes).</param>
        /// <param name="excludeName">Optional rule name to exclude from shifting (used during updates).</param>
        private async Task CascadingShiftEfficientAsync(
            int startOrder,
            string user,
            string? excludeName = null
        )
        {
            if (string.IsNullOrWhiteSpace(user))
                throw new ArgumentException("User cannot be null or empty.", nameof(user));

            logger.LogTrace<LiveChatSettingsService>(
                $"Starting CascadingShiftEfficientAsync with startOrder: {startOrder}, user: {user}, excludeName: {excludeName}"
            );

            try
            {
                var candidates = (await GetRulesAtOrAfterOrderAsync(startOrder))
                    .Where(rule => rule.Name != excludeName)
                    .OrderBy(rule => rule.EvaluationOrder)
                    .ToList();

                logger.LogTrace<LiveChatSettingsService>(
                    $"Found {candidates.Count} candidate rules for shifting"
                );

                if (!candidates.Any())
                {
                    logger.LogTrace<LiveChatSettingsService>(
                        "No candidates found for shifting, returning"
                    );
                    return;
                }

                // Shift rules starting from the conflict position and handle consecutive conflicts
                var shifts = new List<LiveChatRule>();
                int currentShiftPosition = startOrder;

                while (true)
                {
                    // Find if there's a rule at the current position that needs to be shifted
                    var ruleToShift = candidates.FirstOrDefault(r => 
                        r.EvaluationOrder.HasValue && 
                        r.EvaluationOrder.Value == currentShiftPosition);
                    
                    if (ruleToShift == null)
                    {
                        // No rule at this position, we can stop the cascade
                        break;
                    }

                    // This rule conflicts with the current position, shift it
                    int newOrder = ruleToShift.EvaluationOrder.Value + EvaluationOrderIncrement;
                    
                    logger.LogTrace<LiveChatSettingsService>(
                        $"Shifting rule '{ruleToShift.Name}' from order {ruleToShift.EvaluationOrder.Value} to {newOrder}"
                    );

                    ruleToShift.EvaluationOrder = newOrder;
                    ruleToShift.SetAudit(user, isNew: false);
                    shifts.Add(ruleToShift);
                    
                    // Update the next position we need to check for conflicts
                    currentShiftPosition = newOrder;
                }

                logger.LogTrace<LiveChatSettingsService>(
                    $"Shift planning completed. {shifts.Count} rules need to be updated"
                );

                if (shifts.Count > 0)
                {
                    logger.LogTrace<LiveChatSettingsService>(
                        $"Updating {shifts.Count} rules in Cosmos DB"
                    );

                    foreach (var rule in shifts)
                    {
                        logger.LogTrace<LiveChatSettingsService>(
                            $"Updating rule: {rule.Name} with new order: {rule.EvaluationOrder}"
                        );
                        await cosmosDbClient.UpsertItemAsync(containerId, rule);
                    }

                    logger.LogTrace<LiveChatSettingsService>("All rules updated successfully");
                }
                else
                {
                    logger.LogTrace<LiveChatSettingsService>("No rules required shifting");
                }
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>(
                    $"Error in CascadingShiftEfficientAsync: {ex.Message}",
                    ex.ToDictionary()
                );
                throw;
            }
        }

        /// <summary>
        /// Retrieves all live chat rules that have an evaluation order greater than or equal to the specified value.
        /// </summary>
        /// <param name="evaluationOrder">The minimum evaluation order to filter by.</param>
        /// <returns>A list of rules with evaluation order >= the specified value.</returns>
        private async Task<List<LiveChatRule>> GetRulesAtOrAfterOrderAsync(int evaluationOrder)
        {
            logger.LogTrace<LiveChatSettingsService>(
                $"Starting GetRulesAtOrAfterOrderAsync for order >= {evaluationOrder}"
            );

            var query = new QueryDefinition(Queries.GetRulesByEvaluationOrder)
                .WithParameter("@partitionKey", PartitionKeyValue)
                .WithParameter("@evaluationOrder", evaluationOrder);

            logger.LogTrace<LiveChatSettingsService>(
                $"GetRulesAtOrAfterOrderAsync query: {query.QueryText}, parameters: partitionKey={PartitionKeyValue}, evaluationOrder={evaluationOrder}"
            );
            try
            {
                var iterator = await cosmosDbClient.QueryItemsIteratorAsync<LiveChatRule>(
                    containerId,
                    query
                );
                if (iterator == null)
                {
                    logger.LogTrace<LiveChatSettingsService>(
                        "Query iterator returned null, returning empty list"
                    );
                    return new List<LiveChatRule>();
                }

                var results = new List<LiveChatRule>();
                while (iterator.HasMoreResults)
                {
                    var batch = (await iterator.ReadNextAsync())?.Resource;
                    if (batch != null)
                    {
                        results.AddRange(batch);
                        logger.LogTrace<LiveChatSettingsService>(
                            $"Added {batch.Count()} rules from batch"
                        );
                    }
                }

                logger.LogTrace<LiveChatSettingsService>(
                    $"GetRulesAtOrAfterOrderAsync completed. Found {results.Count} rules"
                );
                return results;
            }
            catch (Exception ex)
            {
                logger.LogError<LiveChatSettingsService>(
                    $"Error in GetRulesAtOrAfterOrderAsync: {ex.Message}",
                    ex.ToDictionary()
                );
                throw;
            }
        }

        private async Task<int> DetermineEvaluationOrderAsync(LiveChatRule newRule, string user)
        {
            if (newRule.EvaluationOrder is int evalOrder)
            {
                ValidateEvaluationOrder(evalOrder);
                await CascadingShiftEfficientAsync(evalOrder, user);
                return evalOrder;
            }

            var maxOrder = await GetMaxEvaluationOrderAsync();
            return maxOrder + EvaluationOrderIncrement;
        }

        private static void ValidateEvaluationOrder(int evalOrder)
        {
            if (evalOrder < MinEvaluationOrder)
                throw new ArgumentOutOfRangeException(
                    nameof(evalOrder),
                    $"EvaluationOrder must be ≥ {MinEvaluationOrder}."
                );
        }
    }
}
