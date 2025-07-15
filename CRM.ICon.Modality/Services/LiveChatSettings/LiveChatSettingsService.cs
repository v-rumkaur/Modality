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
            this.logger = telemetryService;
            this.containerId = this.cosmosDbConfiguration.ContainerIds.LiveChatSettings;
        }

        public async Task<IEnumerable<LiveChatRule>> GetAllRulesAsync()
        {
            logger.LogTrace<LiveChatSettingsService>("Starting GetAllRulesAsync");
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.partitionKey = @partitionKey ORDER BY c.evaluationOrder"
            ).WithParameter("@partitionKey", partitionKey);

            return await cosmosDbClient.QueryItemsAsync<LiveChatRule>(containerId, query);
        }

        public async Task CreateRuleAsync(LiveChatRule rule, string createdBy)
        {
            logger.LogTrace<LiveChatSettingsService>(
                $"Starting CreateRuleAsync for rule: {rule.Name}", rule.ToDictionary()
            );
            var nameQuery = new QueryDefinition(
                "SELECT VALUE c.id FROM c WHERE c.name = @name"
            ).WithParameter("@name", rule.Name);

            var existing = await cosmosDbClient.QueryItemsAsync<string>(containerId, nameQuery);
            if (existing.Any())
                throw new InvalidOperationException(
                    $"A rule with name '{rule.Name}' already exists."
                );

            var conflictQuery = new QueryDefinition(
                "SELECT VALUE c.id FROM c WHERE c.evaluationOrder = @eval"
            ).WithParameter("@eval", rule.EvaluationOrder ?? -1);
            var conflicts = await cosmosDbClient.QueryItemsAsync<string>(
                containerId,
                conflictQuery
            );
            logger.LogTrace<LiveChatSettingsService>(
                $"Found {conflicts.Count()} conflicts with evaluation order: {rule.EvaluationOrder}", rule.ToDictionary()
            );
            if (!rule.EvaluationOrder.HasValue || conflicts.Any())
            {
                logger.LogTrace<LiveChatSettingsService>(
                    $"Setting evaluation order for rule: {rule.Name} to next available value", rule.ToDictionary()
                );
                var maxEvalQuery = new QueryDefinition(
                    "SELECT VALUE MAX(c.evaluationOrder) FROM c WHERE c.partitionKey = @partitionKey AND IS_DEFINED(c.evaluationOrder)"
                ).WithParameter("@partitionKey", partitionKey);
                var maxEval = await cosmosDbClient.GetScalarValueAsync<int?>(
                    containerId,
                    maxEvalQuery
                );
                rule.EvaluationOrder = (maxEval ?? 0) + 1;
            }

            rule.SetAudit(createdBy ?? "createdBy", isNew: true);
            logger.LogTrace<LiveChatSettingsService>(
                $"Creating rule: {rule.Name}", rule.ToDictionary()
            );
            await cosmosDbClient.CreateItemAsync(containerId, rule, rule.PartitionKey);
        }

        public async Task<LiveChatRule?> MatchRuleAsync(MatchRuleRequest request)
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
            ).WithParameter("@partitionKey", partitionKey).WithParameter("@serviceLevel", request.ServiceLevel).WithParameter("@isRestricted", request.IsRestricted).WithParameter("@sapId", request.SapId).WithParameter("@serviceId", request.ServiceId);

            var matches = await cosmosDbClient.QueryItemsAsync<LiveChatRule>(containerId, query);
            return matches.FirstOrDefault();
        }
    }
}
