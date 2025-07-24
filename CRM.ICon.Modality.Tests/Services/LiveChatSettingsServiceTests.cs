using System.Net;
using CRM.ICon.Modality.Helpers.ModalityCosmos;
using CRM.ICon.Modality.Model.LiveChatSettings;
using CRM.ICon.Modality.Model.LiveChatSettings.Requests;
using CRM.ICon.Modality.Services.LiveChatSettings;
using FluentAssertions;
using LiveChatSettings.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LiveChatSettings.Tests.Services
{
    public class LiveChatSettingsServiceTests
    {
        private readonly Mock<IModalityCosmosDbClient> _mockCosmosDbClient;
        private readonly Mock<IOptions<ModalityCosmosDbConfiguration>> _mockCosmosDbOptions;
        private readonly ModalityCosmosDbConfiguration _cosmosDbConfig;
        private readonly LiveChatSettingsService _liveChatService;

        public LiveChatSettingsServiceTests()
        {
            _mockCosmosDbClient = new Mock<IModalityCosmosDbClient>();
            _cosmosDbConfig = new ModalityCosmosDbConfiguration
            {
                CosmosDbEndpoint = "https://test.documents.azure.com:443/",
                DatabaseId = "test-database",
                LiveChatContainerId = "test-container",
                RequestTimeout = 2,
            };
            _mockCosmosDbOptions = new Mock<IOptions<ModalityCosmosDbConfiguration>>();
            _mockCosmosDbOptions.Setup(x => x.Value).Returns(_cosmosDbConfig);

            _liveChatService = new LiveChatSettingsService(
                _mockCosmosDbOptions.Object,
                _mockCosmosDbClient.Object
            );
        }

        #region GetAllRulesAsync Tests

        [Fact]
        public async Task GetAllRulesAsync_WhenRulesExist_ShouldReturnAllRules()
        {
            var expectedRules = new List<LiveChatRule>
            {
                CreateTestRule("rule1", evaluationOrder: 1),
                CreateTestRule("rule2", evaluationOrder: 2),
            };

            _mockCosmosDbClient
                .Setup(x =>
                    x.QueryItemsAsync<LiveChatRule>(
                        _cosmosDbConfig.LiveChatContainerId,
                        It.IsAny<QueryDefinition>()
                    )
                )
                .ReturnsAsync(expectedRules);

            var result = await _liveChatService.GetAllRulesAsync();

            result.Should().BeEquivalentTo(expectedRules);
        }

        [Fact]
        public async Task GetAllRulesAsync_WhenNoRulesExist_ShouldReturnEmptyCollection()
        {
            _mockCosmosDbClient
                .Setup(x =>
                    x.QueryItemsAsync<LiveChatRule>(
                        _cosmosDbConfig.LiveChatContainerId,
                        It.IsAny<QueryDefinition>()
                    )
                )
                .ReturnsAsync(new List<LiveChatRule>());

            var result = await _liveChatService.GetAllRulesAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllRulesAsync_WhenCosmosDbThrowsException_ShouldPropagateException()
        {
            var expectedException = new CosmosException(
                "Database error",
                HttpStatusCode.InternalServerError,
                0,
                "test",
                1
            );

            _mockCosmosDbClient
                .Setup(x =>
                    x.QueryItemsAsync<LiveChatRule>(
                        _cosmosDbConfig.LiveChatContainerId,
                        It.IsAny<QueryDefinition>()
                    )
                )
                .ThrowsAsync(expectedException);

            var exception = await Assert.ThrowsAsync<CosmosException>(() =>
                _liveChatService.GetAllRulesAsync()
            );

            exception.Should().Be(expectedException);
        }

        #endregion

        #region GetRuleByNameAsync Tests

        [Fact]
        public async Task GetRuleByNameAsync_WhenRuleExists_ShouldReturnRule()
        {
            var expectedRule = CreateTestRule("test-rule");

            _mockCosmosDbClient
                .Setup(x =>
                    x.GetItemByIdAsync<LiveChatRule>(
                        _cosmosDbConfig.LiveChatContainerId,
                        "test-rule",
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(expectedRule);

            var result = await _liveChatService.GetRuleByNameAsync("test-rule");

            result.Should().BeEquivalentTo(expectedRule);
        }

        [Fact]
        public async Task GetRuleByNameAsync_WhenRuleNotFound_ShouldReturnNull()
        {
            _mockCosmosDbClient
                .Setup(x =>
                    x.GetItemByIdAsync<LiveChatRule>(
                        _cosmosDbConfig.LiveChatContainerId,
                        "nonexistent-rule",
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((LiveChatRule?)null);

            var result = await _liveChatService.GetRuleByNameAsync("nonexistent-rule");

            result.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetRuleByNameAsync_WhenNameIsInvalid_ShouldThrowArgumentException(
            string invalidName
        )
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _liveChatService.GetRuleByNameAsync(invalidName)
            );
        }

        #endregion

        #region MatchRuleAsync Tests

        [Fact]
        public async Task MatchRuleAsync_WhenRuleMatches_ShouldReturnFirstMatchingRule()
        {
            var matchRequest = new MatchRuleRequest
            {
                ServiceLevel = "professional",
                IsRestricted = false,
                SapId = Guid.Parse("12345678-1234-1234-1234-123456789012"),
                ServiceId = 123,
            };
            var expectedRule = CreateTestRule("matching-rule");
            var mockIterator = CreateMockIterator(new[] { expectedRule });

            _mockCosmosDbClient
                .Setup(x =>
                    x.QueryItemsIteratorAsync<LiveChatRule>(
                        _cosmosDbConfig.LiveChatContainerId,
                        It.IsAny<QueryDefinition>()
                    )
                )
                .ReturnsAsync(mockIterator);

            var result = await _liveChatService.MatchRuleAsync(matchRequest);

            result.Should().BeEquivalentTo(expectedRule);
        }

        [Fact]
        public async Task MatchRuleAsync_WhenNoRuleMatches_ShouldReturnNull()
        {
            var matchRequest = new MatchRuleRequest
            {
                ServiceLevel = "basic",
                IsRestricted = true,
                SapId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                ServiceId = 999,
            };
            var mockIterator = CreateMockIterator(Array.Empty<LiveChatRule>());

            _mockCosmosDbClient
                .Setup(x =>
                    x.QueryItemsIteratorAsync<LiveChatRule>(
                        _cosmosDbConfig.LiveChatContainerId,
                        It.IsAny<QueryDefinition>()
                    )
                )
                .ReturnsAsync(mockIterator);

            var result = await _liveChatService.MatchRuleAsync(matchRequest);

            result.Should().BeNull();
        }

        [Fact]
        public async Task MatchRuleAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _liveChatService.MatchRuleAsync(null)
            );
        }

        #endregion

        #region CreateRuleAsync Tests

        [Fact]
        public async Task CreateRuleAsync_WhenRuleAlreadyExists_ShouldThrowInvalidOperationException()
        {
            var newRule = CreateTestRule("existing-rule");
            var existingRule = CreateTestRule("existing-rule");

            _mockCosmosDbClient
                .Setup(x =>
                    x.GetItemByIdAsync<LiveChatRule>(
                        _cosmosDbConfig.LiveChatContainerId,
                        "existing-rule",
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(existingRule);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _liveChatService.CreateRuleAsync(newRule, "test-user")
            );
        }

        [Fact]
        public async Task CreateRuleAsync_WithValidRule_ShouldCreateRuleSuccessfully()
        {
            var newRule = CreateTestRule("new-rule", evaluationOrder: 5);

            _mockCosmosDbClient
                .Setup(x =>
                    x.UpsertItemAsync(_cosmosDbConfig.LiveChatContainerId, It.IsAny<LiveChatRule>())
                )
                .ReturnsAsync(newRule);

            var result = await _liveChatService.CreateRuleAsync(newRule, "test-user");

            result.Name.Should().Be("new-rule");
        }

        [Fact]
        public async Task CreateRuleAsync_WithNullEvaluationOrder_ShouldAddToEnd()
        {
            var newRule = CreateTestRule("new-rule", evaluationOrder: null);
            var mockIterator = CreateMockIterator(new[] { 5 });

            _mockCosmosDbClient
                .Setup(x =>
                    x.QueryItemsIteratorAsync<int>(
                        _cosmosDbConfig.LiveChatContainerId,
                        It.IsAny<QueryDefinition>()
                    )
                )
                .ReturnsAsync(mockIterator);
            _mockCosmosDbClient
                .Setup(x =>
                    x.UpsertItemAsync(_cosmosDbConfig.LiveChatContainerId, It.IsAny<LiveChatRule>())
                )
                .ReturnsAsync((string _, LiveChatRule rule) => rule);

            var result = await _liveChatService.CreateRuleAsync(newRule, "test-user");

            result.EvaluationOrder.Should().Be(6);
        }

        #endregion

        #region UpdateRuleAsync Tests

        [Fact]
        public async Task UpdateRuleAsync_WithValidRequest_ShouldUpdateRule()
        {
            var existingRule = CreateTestRule("existing-rule", evaluationOrder: 5);
            var updateRequest = new UpdateRuleRequest
            {
                Name = "existing-rule",
                EvaluationOrder = 10,
                UpdatedBy = "test-user",
            };

            _mockCosmosDbClient
                .Setup(x => x.GetItemByIdAsync<LiveChatRule>(_cosmosDbConfig.LiveChatContainerId, "existing-rule", It.IsAny<string>()))
                .ReturnsAsync(existingRule);

            LiveChatRule updatedRule = null;
            _mockCosmosDbClient
                .Setup(x => x.UpsertItemAsync(_cosmosDbConfig.LiveChatContainerId, It.IsAny<LiveChatRule>()))
                .Callback<string, LiveChatRule>((_, rule) => updatedRule = rule)
                .ReturnsAsync((string _, LiveChatRule rule) => rule);

            await _liveChatService.UpdateRuleAsync(updateRequest, "test-user");

            // Verify the rule was actually updated
            updatedRule.Should().NotBeNull();
            updatedRule.EvaluationOrder.Should().Be(10); // This would fail if PatchToDomainModel is commented out
            updatedRule.Name.Should().Be("existing-rule");
        }

        [Fact]
        public async Task UpdateRuleAsync_WhenRuleDoesNotExist_ShouldThrowInvalidOperationException()
        {
            var updateRequest = new UpdateRuleRequest
            {
                Name = "nonexistent-rule",
                UpdatedBy = "test-user",
            };

            _mockCosmosDbClient
                .Setup(x =>
                    x.GetItemByIdAsync<LiveChatRule>(
                        _cosmosDbConfig.LiveChatContainerId,
                        "nonexistent-rule",
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((LiveChatRule?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _liveChatService.UpdateRuleAsync(updateRequest, "test-user")
            );
        }

        [Fact]
        public async Task UpdateRuleAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _liveChatService.UpdateRuleAsync(null, "test-user")
            );
        }

        #endregion

        #region DeleteRuleByNameAsync Tests

        [Fact]
        public async Task DeleteRuleByNameAsync_WhenRuleExists_ShouldDeleteRule()
        {
            var existingRule = CreateTestRule("test-rule");

            _mockCosmosDbClient
                .Setup(x =>
                    x.GetItemByIdAsync<LiveChatRule>(
                        _cosmosDbConfig.LiveChatContainerId,
                        "test-rule",
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(existingRule);
            _mockCosmosDbClient
                .Setup(x =>
                    x.DeleteItemAsync(
                        _cosmosDbConfig.LiveChatContainerId,
                        "test-rule",
                        It.IsAny<string>()
                    )
                )
                .Returns(Task.CompletedTask);

            await _liveChatService.DeleteRuleByNameAsync("test-rule");
        }

        [Fact]
        public async Task DeleteRuleByNameAsync_WhenRuleDoesNotExist_ShouldThrowInvalidOperationException()
        {
            _mockCosmosDbClient
                .Setup(x =>
                    x.GetItemByIdAsync<LiveChatRule>(
                        _cosmosDbConfig.LiveChatContainerId,
                        "nonexistent-rule",
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((LiveChatRule?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _liveChatService.DeleteRuleByNameAsync("nonexistent-rule")
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DeleteRuleByNameAsync_WhenNameIsInvalid_ShouldThrowArgumentException(
            string invalidName
        )
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _liveChatService.DeleteRuleByNameAsync(invalidName)
            );
        }

        #endregion

        #region GetMaxEvaluationOrderAsync Tests

        [Fact]
        public async Task GetMaxEvaluationOrderAsync_WhenRulesExist_ShouldReturnMaxEvaluationOrder()
        {
            var mockIterator = CreateMockIterator(new[] { 42 });

            _mockCosmosDbClient
                .Setup(x =>
                    x.QueryItemsIteratorAsync<int>(
                        _cosmosDbConfig.LiveChatContainerId,
                        It.IsAny<QueryDefinition>()
                    )
                )
                .ReturnsAsync(mockIterator);

            var result = await _liveChatService.GetMaxEvaluationOrderAsync();

            result.Should().Be(42);
        }

        [Fact]
        public async Task GetMaxEvaluationOrderAsync_WhenNoRulesExist_ShouldReturnZero()
        {
            var mockIterator = CreateMockIterator(new[] { 0 });

            _mockCosmosDbClient
                .Setup(x =>
                    x.QueryItemsIteratorAsync<int>(
                        _cosmosDbConfig.LiveChatContainerId,
                        It.IsAny<QueryDefinition>()
                    )
                )
                .ReturnsAsync(mockIterator);

            var result = await _liveChatService.GetMaxEvaluationOrderAsync();

            result.Should().Be(0);
        }

        #endregion

        #region Cascading Shift Tests

        [Fact]
        public async Task CreateRuleAsync_InsertInGap_ShouldNotShiftExistingRules()
        {
            var newRule = CreateTestRule("new-rule", evaluationOrder: 2);
            var existingRules = new List<LiveChatRule>
            {
                CreateTestRule("rule3", evaluationOrder: 3),
                CreateTestRule("rule4", evaluationOrder: 4),
            };

            SetupCascadingShiftMocks(existingRules);

            var result = await _liveChatService.CreateRuleAsync(newRule, "test-user");

            result.EvaluationOrder.Should().Be(2);
        }

        [Fact]
        public async Task CreateRuleAsync_ConsecutiveConflicts_ShouldCascadeShiftUntilGap()
        {
            var newRule = CreateTestRule("new-rule", evaluationOrder: 3);
            var existingRules = new List<LiveChatRule>
            {
                CreateTestRule("rule3", evaluationOrder: 3),
                CreateTestRule("rule4", evaluationOrder: 4),
                CreateTestRule("rule5", evaluationOrder: 5),
                CreateTestRule("rule7", evaluationOrder: 7), // Gap at 6
            };

            // Track which rules get upserted
            var upsertedRules = new List<LiveChatRule>();
            
            _mockCosmosDbClient
                .Setup(x => x.GetItemByIdAsync<LiveChatRule>(_cosmosDbConfig.LiveChatContainerId, "new-rule", It.IsAny<string>()))
                .ReturnsAsync((LiveChatRule?)null);

            SetupCascadingShiftMocks(existingRules);
            
            _mockCosmosDbClient
                .Setup(x => x.UpsertItemAsync(_cosmosDbConfig.LiveChatContainerId, It.IsAny<LiveChatRule>()))
                .Callback<string, LiveChatRule>((_, rule) => upsertedRules.Add(rule))
                .ReturnsAsync((string _, LiveChatRule rule) => rule);

            var result = await _liveChatService.CreateRuleAsync(newRule, "test-user");

            // Verify the new rule
            result.EvaluationOrder.Should().Be(3);
            
            // Verify that shifted rules were actually saved to database
            upsertedRules.Should().HaveCount(4, "because 3 existing rules should be shifted + 1 new rule");
            
            // Verify specific shifted rules were saved with correct new orders
            var shiftedRule3 = upsertedRules.FirstOrDefault(r => r.Name == "rule3");
            shiftedRule3?.EvaluationOrder.Should().Be(4, "because rule3 should be shifted from 3 to 4");
            
            var shiftedRule4 = upsertedRules.FirstOrDefault(r => r.Name == "rule4");
            shiftedRule4?.EvaluationOrder.Should().Be(5, "because rule4 should be shifted from 4 to 5");
            
            var shiftedRule5 = upsertedRules.FirstOrDefault(r => r.Name == "rule5");
            shiftedRule5?.EvaluationOrder.Should().Be(6, "because rule5 should be shifted from 5 to 6");
            
            // Rule7 should NOT be shifted (gap at 6)
            var rule7 = upsertedRules.FirstOrDefault(r => r.Name == "rule7");
            rule7.Should().BeNull("because rule7 should not be shifted due to gap at position 6");
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WhenCosmosDbConfigurationIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new LiveChatSettingsService(null, _mockCosmosDbClient.Object)
            );
        }

        [Fact]
        public void Constructor_WhenCosmosDbClientIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new LiveChatSettingsService(_mockCosmosDbOptions.Object, null)
            );
        }

        #endregion

        #region Test Helpers

        private FeedIterator<T> CreateMockIterator<T>(IEnumerable<T> data)
        {
            var mockIterator = new Mock<FeedIterator<T>>();
            var mockResponse = new Mock<FeedResponse<T>>();
            mockResponse.Setup(x => x.Resource).Returns(data);
            mockIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            mockIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(mockResponse.Object);
            return mockIterator.Object;
        }

        private void SetupCascadingShiftMocks(List<LiveChatRule> existingRules)
        {
            var mockIterator = CreateMockIterator(existingRules);
            _mockCosmosDbClient
                .Setup(x =>
                    x.QueryItemsIteratorAsync<LiveChatRule>(
                        _cosmosDbConfig.LiveChatContainerId,
                        It.IsAny<QueryDefinition>()
                    )
                )
                .ReturnsAsync(mockIterator);
            _mockCosmosDbClient
                .Setup(x => x.UpsertItemAsync(It.IsAny<string>(), It.IsAny<LiveChatRule>()))
                .ReturnsAsync((string _, LiveChatRule rule) => rule); // Fixed: using It.IsAny for both parameters
        }

        private static int _ruleCounter = 0;

        private static LiveChatRule CreateTestRule(
            string? name = null,
            int? evaluationOrder = null,
            List<string>? allowedServiceLevels = null,
            bool? allowRestricted = null,
            List<Guid>? allowedSaps = null,
            List<int>? excludedServiceIds = null,
            bool? isChatForced = null
        )
        {
            return new LiveChatRule
            {
                Name = name ?? $"rule-{Interlocked.Increment(ref _ruleCounter)}",
                EvaluationOrder = evaluationOrder,
                AllowedServiceLevels = allowedServiceLevels ?? new List<string> { "professional" },
                AllowRestricted = allowRestricted ?? false,
                AllowedSaps =
                    allowedSaps
                    ?? new List<Guid> { Guid.Parse("00000000-0000-0000-0000-000000000001") },
                ExcludedServiceIds = excludedServiceIds ?? new List<int>(),
                IsChatForced = isChatForced ?? true,
            };
        }

        #endregion
    }
}
