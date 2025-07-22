using CRM.ICon.Modality.Helpers.ModalityCosmos;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model.LiveChatSettings;
using CRM.ICon.Modality.Model.LiveChatSettings.Requests;
using CRM.ICon.Modality.Services.LiveChatSettings;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using FluentAssertions;
using System.Net;
using System.Reflection;

namespace CRM.ICon.Modality.Tests.Services
{
    /// <summary>
    /// Comprehensive unit tests for LiveChatSettingsService covering all CRUD operations and evaluation order management.
    /// Tests use mocked dependencies for isolated unit testing with complete coverage of business logic.
    /// </summary>
    public class LiveChatSettingsServiceTests
    {
        #region Test Infrastructure

        private readonly IModalityCosmosDbClient mockCosmosClient;
        private readonly ITelemetryService mockTelemetryService;
        private readonly IOptions<ModalityCosmosDbConfiguration> mockConfig;
        private readonly LiveChatSettingsService service;

        public LiveChatSettingsServiceTests()
        {
            mockCosmosClient = Substitute.For<IModalityCosmosDbClient>();
            mockTelemetryService = Substitute.For<ITelemetryService>();
            mockConfig = Substitute.For<IOptions<ModalityCosmosDbConfiguration>>();
            
            mockConfig.Value.Returns(new ModalityCosmosDbConfiguration
            {
                ContainerIds = new ContainerIds { LiveChatSettings = "livechat-container", WidgetMapping = "widget-container" },
            });

            service = new LiveChatSettingsService(mockConfig, mockCosmosClient, mockTelemetryService);
        }

        #endregion

        #region GetAllRulesAsync Tests

        [Fact]
        public async Task GetAllRulesAsync_ReturnsAllRules_WhenRulesExist()
        {
            // Arrange
            var expectedRules = new List<LiveChatRule>
            {
                CreateTestRule("rule-alpha", 1),
                CreateTestRule("rule-beta", 2)
            };
            mockCosmosClient.QueryItemsAsync<LiveChatRule>(Arg.Any<string>(), Arg.Any<QueryDefinition>())
                .Returns(expectedRules);

            // Act
            var result = await service.GetAllRulesAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(r => r.Name == "rule-alpha");
            result.Should().Contain(r => r.Name == "rule-beta");
        }

        [Fact]
        public async Task GetAllRulesAsync_ReturnsEmptyList_WhenNoRulesExist()
        {
            // Arrange
            mockCosmosClient.QueryItemsAsync<LiveChatRule>(Arg.Any<string>(), Arg.Any<QueryDefinition>())
                .Returns(new List<LiveChatRule>());

            // Act
            var result = await service.GetAllRulesAsync();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllRulesAsync_LogsErrorAndRethrows_WhenCosmosThrows()
        {
            // Arrange
            var cosmosException = new CosmosException("Query failed", HttpStatusCode.InternalServerError, 0, "", 0);
            mockCosmosClient.QueryItemsAsync<LiveChatRule>(Arg.Any<string>(), Arg.Any<QueryDefinition>())
                .Throws(cosmosException);

            // Act & Assert
            await Assert.ThrowsAsync<CosmosException>(() => service.GetAllRulesAsync());

            mockTelemetryService.Received(1).LogError<LiveChatSettingsService>(
                Arg.Is<string>(s => s.Contains("Error in GetAllRulesAsync")),
                Arg.Any<IDictionary<string, string>>()
            );
        }

        #endregion

        #region GetRuleByNameAsync Tests

        [Fact]
        public async Task GetRuleByNameAsync_ReturnsRule_WhenRuleExists()
        {
            // Arrange
            var expectedRule = CreateTestRule("existing-rule", 3);
            mockCosmosClient.GetItemByIdAsync<LiveChatRule>(Arg.Any<string>(), "existing-rule", Arg.Any<string>())
                .Returns(expectedRule);

            // Act
            var result = await service.GetRuleByNameAsync("existing-rule");

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("existing-rule");
            result.EvaluationOrder.Should().Be(3);
        }

        [Fact]
        public async Task GetRuleByNameAsync_ReturnsNull_WhenRuleDoesNotExist()
        {
            // Arrange
            mockCosmosClient.GetItemByIdAsync<LiveChatRule>(Arg.Any<string>(), "nonexistent-rule", Arg.Any<string>())
                .Returns((LiveChatRule?)null);

            // Act
            var result = await service.GetRuleByNameAsync("nonexistent-rule");

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task GetRuleByNameAsync_ThrowsArgumentException_WhenNameIsInvalid(string invalidName)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.GetRuleByNameAsync(invalidName));
        }

        #endregion

        #region MatchRuleAsync Tests

        [Fact]
        public async Task MatchRuleAsync_ReturnsMatchingRule_WhenUserMatches()
        {
            // Arrange
            var request = new MatchRuleRequest
            {
                ServiceLevel = "Premium",
                SapId = Guid.NewGuid(),
                ServiceId = 123,
                IsRestricted = false
            };

            var matchedRule = CreateTestRule("premium-rule", 10);
            SetupQueryIteratorResponse(new List<LiveChatRule> { matchedRule });

            // Act
            var result = await service.MatchRuleAsync(request);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("premium-rule");
        }

        [Fact]
        public async Task MatchRuleAsync_ReturnsNull_WhenNoRuleMatches()
        {
            // Arrange
            var request = new MatchRuleRequest
            {
                ServiceLevel = "Basic",
                SapId = Guid.NewGuid(),
                ServiceId = 456,
                IsRestricted = true
            };

            SetupQueryIteratorResponse(new List<LiveChatRule>());

            // Act
            var result = await service.MatchRuleAsync(request);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task MatchRuleAsync_ThrowsArgumentNullException_WhenRequestIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.MatchRuleAsync(null!));
        }

        #endregion

        #region CreateRuleAsync Tests

        /// <summary>
        /// Basic creation tests covering fundamental rule creation scenarios.
        /// </summary>

        [Fact]
        public async Task CreateRuleAsync_CreatesRule_WhenValidInputProvided()
        {
            // Arrange
            var newRule = CreateTestRule("new-rule", null);
            SetupRuleDoesNotExist("new-rule");
            SetupMaxEvaluationOrderQuery(4);

            // Act
            var result = await service.CreateRuleAsync(newRule, "test-user");

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("new-rule");
            result.EvaluationOrder.Should().Be(5); // max + 1
            await mockCosmosClient.Received(1).UpsertItemAsync(Arg.Any<string>(), newRule);
        }

        [Fact]
        public async Task CreateRuleAsync_ThrowsInvalidOperationException_WhenRuleAlreadyExists()
        {
            // Arrange
            var newRule = CreateTestRule("existing-rule", 1);
            var existingRule = CreateTestRule("existing-rule", 1);
            mockCosmosClient.GetItemByIdAsync<LiveChatRule>(Arg.Any<string>(), "existing-rule", Arg.Any<string>())
                .Returns(existingRule);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateRuleAsync(newRule, "test-user"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateRuleAsync_ThrowsArgumentException_WhenUserIsInvalid(string invalidUser)
        {
            // Arrange
            var newRule = CreateTestRule("test-rule", 1);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateRuleAsync(newRule, invalidUser));
        }

        /// <summary>
        /// Evaluation order assignment tests covering automatic order assignment logic.
        /// </summary>

        [Fact]
        public async Task CreateRuleAsync_AssignsMaxPlusOne_WhenNoEvaluationOrderProvided()
        {
            // Arrange
            var newRule = CreateTestRule("auto-order-rule", null);
            SetupRuleDoesNotExist("auto-order-rule");
            SetupMaxEvaluationOrderQuery(10);

            // Act
            var result = await service.CreateRuleAsync(newRule, "test-user");

            // Assert
            result.EvaluationOrder.Should().Be(11); // max + 1
        }

        [Fact]
        public async Task CreateRuleAsync_AssignsOrderOne_WhenNoExistingRules()
        {
            // Arrange
            var newRule = CreateTestRule("first-rule", null);
            SetupRuleDoesNotExist("first-rule");
            SetupMaxEvaluationOrderQuery(0); // No existing rules

            // Act
            var result = await service.CreateRuleAsync(newRule, "test-user");

            // Assert
            result.EvaluationOrder.Should().Be(1);
        }

        /// <summary>
        /// Conflict resolution tests covering evaluation order collision handling.
        /// </summary>

        [Fact]
        public async Task CreateRuleAsync_ShiftsSingleConflict_WhenOneRuleAtOrder()
        {
            // Arrange
            var newRule = CreateTestRule("new-rule", 3);
            var conflictingRule = CreateTestRule("existing-at-3", 3);
            
            SetupRuleDoesNotExist("new-rule");
            SetupGetRulesAtOrAfterOrderQuery(3, new List<LiveChatRule> { conflictingRule });

            // Act
            var result = await service.CreateRuleAsync(newRule, "test-user");

            // Assert
            result.EvaluationOrder.Should().Be(3);
            await mockCosmosClient.Received(1).UpsertItemAsync(Arg.Any<string>(), 
                Arg.Is<LiveChatRule>(r => r.Name == "existing-at-3" && r.EvaluationOrder == 4));
        }

        [Fact]
        public async Task CreateRuleAsync_ShiftsMultipleConflicts_WhenConsecutiveRulesAtOrders()
        {
            // Arrange
            var newRule = CreateTestRule("new-rule", 2);
            var conflictingRules = new List<LiveChatRule>
            {
                CreateTestRule("rule-at-2", 2),
                CreateTestRule("rule-at-3", 3),
                CreateTestRule("rule-at-4", 4)
            };
            
            SetupRuleDoesNotExist("new-rule");
            SetupGetRulesAtOrAfterOrderQuery(2, conflictingRules);

            // Act
            var result = await service.CreateRuleAsync(newRule, "test-user");

            // Assert
            result.EvaluationOrder.Should().Be(2);
            await mockCosmosClient.Received(1).UpsertItemAsync(Arg.Any<string>(), 
                Arg.Is<LiveChatRule>(r => r.Name == "rule-at-2" && r.EvaluationOrder == 3));
            await mockCosmosClient.Received(1).UpsertItemAsync(Arg.Any<string>(), 
                Arg.Is<LiveChatRule>(r => r.Name == "rule-at-3" && r.EvaluationOrder == 4));
            await mockCosmosClient.Received(1).UpsertItemAsync(Arg.Any<string>(), 
                Arg.Is<LiveChatRule>(r => r.Name == "rule-at-4" && r.EvaluationOrder == 5));
        }

        /// <summary>
        /// Boundary value tests covering edge cases and validation limits.
        /// </summary>

        [Fact]
        public async Task CreateRuleAsync_AcceptsMaximumValues_WhenLargeValidData()
        {
            // Arrange
            var newRule = new LiveChatRule
            {
                Name = "max-values-rule",
                EvaluationOrder = 1000,
                AllowedServiceLevels = Enumerable.Range(1, 50).Select(i => $"ServiceLevel{i}").ToList(),
                AllowedSaps = Enumerable.Range(1, 100).Select(_ => Guid.NewGuid()).ToList(),
                ExcludedServiceIds = Enumerable.Range(1, 200).ToList(),
                IsChatForced = true,
                AllowRestricted = true
            };
            
            SetupRuleDoesNotExist("max-values-rule");
            SetupGetRulesAtOrAfterOrderQuery(1000, new List<LiveChatRule>());

            // Act
            var result = await service.CreateRuleAsync(newRule, "test-user");

            // Assert
            result.Should().NotBeNull();
            result.AllowedServiceLevels.Should().HaveCount(50);
            result.AllowedSaps.Should().HaveCount(100);
            result.ExcludedServiceIds.Should().HaveCount(200);
        }

        [Fact]
        public async Task CreateRuleAsync_AcceptsMinimumViableRule_WhenMinimalData()
        {
            // Arrange
            var newRule = new LiveChatRule
            {
                Name = "minimal-rule",
                EvaluationOrder = 1,
                AllowedServiceLevels = new List<string> { "Basic" },
                AllowedSaps = new List<Guid> { Guid.NewGuid() },
                ExcludedServiceIds = new List<int> { 1 },
                IsChatForced = false,
                AllowRestricted = false
            };
            
            SetupRuleDoesNotExist("minimal-rule");
            SetupGetRulesAtOrAfterOrderQuery(1, new List<LiveChatRule>());

            // Act
            var result = await service.CreateRuleAsync(newRule, "test-user");

            // Assert
            result.Should().NotBeNull();
            result.AllowedServiceLevels.Should().HaveCount(1);
            result.AllowedSaps.Should().HaveCount(1);
            result.ExcludedServiceIds.Should().HaveCount(1);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-10)]
        [InlineData(-100)]
        public async Task CreateRuleAsync_ThrowsArgumentOutOfRangeException_WhenNegativeEvaluationOrder(int negativeOrder)
        {
            // Arrange
            var newRule = CreateTestRule("test-rule", negativeOrder);
            SetupRuleDoesNotExist("test-rule");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateRuleAsync(newRule, "test-user"));
        }

        #endregion

        #region UpdateRuleAsync Tests

        /// <summary>
        /// Field update tests covering selective property modification scenarios.
        /// </summary>

        [Fact]
        public async Task UpdateRuleAsync_UpdatesSingleField_WhenOnlyOneBooleanChanged()
        {
            // Arrange
            var request = new UpdateRuleRequest 
            { 
                Name = "update-single-rule", 
                UpdatedBy = "test-user",
                IsChatForced = true // Only this field updated
            };
            
            var existingRule = CreateTestRule("update-single-rule", 5);
            existingRule.IsChatForced = false; // Original value
            existingRule.AllowRestricted = true; // Should remain unchanged
            
            SetupRuleExists("update-single-rule", existingRule);

            // Act
            await service.UpdateRuleAsync(request, "test-user");

            // Assert
            existingRule.IsChatForced.Should().BeTrue(); // Updated
            existingRule.AllowRestricted.Should().BeTrue(); // Preserved
            existingRule.EvaluationOrder.Should().Be(5); // Preserved
        }

        [Fact]
        public async Task UpdateRuleAsync_UpdatesMultipleFields_WhenMultipleProvided()
        {
            // Arrange
            var originalSaps = new List<Guid> { Guid.NewGuid() };
            var newSaps = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            
            var request = new UpdateRuleRequest 
            { 
                Name = "update-multi-rule", 
                UpdatedBy = "test-user",
                IsChatForced = true,
                AllowedSaps = newSaps,
                EvaluationOrder = 10
            };
            
            var existingRule = CreateTestRule("update-multi-rule", 5);
            existingRule.IsChatForced = false;
            existingRule.AllowedSaps = originalSaps;
            existingRule.AllowRestricted = true; // Should remain unchanged
            
            SetupRuleExists("update-multi-rule", existingRule);
            SetupGetRulesAtOrAfterOrderQuery(10, new List<LiveChatRule>());

            // Act
            await service.UpdateRuleAsync(request, "test-user");

            // Assert
            existingRule.IsChatForced.Should().BeTrue(); // Updated
            existingRule.AllowedSaps.Should().BeEquivalentTo(newSaps); // Updated
            existingRule.EvaluationOrder.Should().Be(10); // Updated
            existingRule.AllowRestricted.Should().BeTrue(); // Preserved
        }

        [Fact]
        public async Task UpdateRuleAsync_PreservesName_WhenAllOtherFieldsUpdated()
        {
            // Arrange
            var request = new UpdateRuleRequest 
            { 
                Name = "preserve-name-rule", 
                UpdatedBy = "test-user",
                IsChatForced = true,
                AllowRestricted = false,
                AllowedServiceLevels = new List<string> { "Premium", "Enterprise" },
                AllowedSaps = new List<Guid> { Guid.NewGuid() },
                ExcludedServiceIds = new List<int> { 100, 200 },
                EvaluationOrder = 15
            };
            
            var existingRule = CreateTestRule("preserve-name-rule", 5);
            SetupRuleExists("preserve-name-rule", existingRule);
            SetupGetRulesAtOrAfterOrderQuery(15, new List<LiveChatRule>());

            // Act
            await service.UpdateRuleAsync(request, "test-user");

            // Assert
            existingRule.Name.Should().Be("preserve-name-rule"); // Name preserved
            existingRule.IsChatForced.Should().BeTrue(); // Updated
            existingRule.AllowRestricted.Should().BeFalse(); // Updated
            existingRule.AllowedServiceLevels.Should().BeEquivalentTo(new[] { "Premium", "Enterprise" }); // Updated
            existingRule.EvaluationOrder.Should().Be(15); // Updated
        }

        /// <summary>
        /// Evaluation order handling tests covering order modification scenarios.
        /// </summary>

        [Fact]
        public async Task UpdateRuleAsync_PreservesEvaluationOrder_WhenNotProvided()
        {
            // Arrange
            var request = new UpdateRuleRequest 
            { 
                Name = "preserve-order-rule", 
                UpdatedBy = "test-user",
                IsChatForced = true
                // EvaluationOrder not provided
            };
            
            var existingRule = CreateTestRule("preserve-order-rule", 7);
            SetupRuleExists("preserve-order-rule", existingRule);

            // Act
            await service.UpdateRuleAsync(request, "test-user");

            // Assert
            existingRule.EvaluationOrder.Should().Be(7); // Original value preserved
            // Should not trigger cascading shift
            await mockCosmosClient.DidNotReceive().UpsertItemAsync(Arg.Any<string>(), 
                Arg.Is<LiveChatRule>(r => r.Name != "preserve-order-rule"));
        }

        [Fact]
        public async Task UpdateRuleAsync_TriggersShiftResolution_WhenEvaluationOrderCausesConflict()
        {
            // Arrange
            var request = new UpdateRuleRequest 
            { 
                Name = "shift-trigger-rule", 
                UpdatedBy = "test-user",
                EvaluationOrder = 3
            };
            
            var existingRule = CreateTestRule("shift-trigger-rule", 7);
            var conflictingRule = CreateTestRule("rule-at-3", 3);
            
            SetupRuleExists("shift-trigger-rule", existingRule);
            SetupGetRulesAtOrAfterOrderQuery(3, new List<LiveChatRule> { conflictingRule });

            // Act
            await service.UpdateRuleAsync(request, "test-user");

            // Assert
            existingRule.EvaluationOrder.Should().Be(3);
            await mockCosmosClient.Received(1).UpsertItemAsync(Arg.Any<string>(), 
                Arg.Is<LiveChatRule>(r => r.Name == "rule-at-3" && r.EvaluationOrder == 4));
        }

        /// <summary>
        /// Error handling tests covering validation and exception scenarios.
        /// </summary>

        [Fact]
        public async Task UpdateRuleAsync_ThrowsInvalidOperationException_WhenRuleNotFound()
        {
            // Arrange
            var request = new UpdateRuleRequest 
            { 
                Name = "nonexistent-rule", 
                UpdatedBy = "test-user",
                IsChatForced = true
            };
            
            mockCosmosClient.GetItemByIdAsync<LiveChatRule>(Arg.Any<string>(), "nonexistent-rule", Arg.Any<string>())
                .Returns((LiveChatRule?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateRuleAsync(request, "test-user"));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-5)]
        [InlineData(-100)]
        public async Task UpdateRuleAsync_ThrowsArgumentOutOfRangeException_WhenNegativeEvaluationOrder(int negativeOrder)
        {
            // Arrange
            var request = new UpdateRuleRequest 
            { 
                Name = "negative-order-rule", 
                UpdatedBy = "test-user",
                EvaluationOrder = negativeOrder
            };
            
            var existingRule = CreateTestRule("negative-order-rule", 5);
            SetupRuleExists("negative-order-rule", existingRule);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.UpdateRuleAsync(request, "test-user"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateRuleAsync_ThrowsArgumentException_WhenUserIsInvalid(string invalidUser)
        {
            // Arrange
            var request = new UpdateRuleRequest 
            { 
                Name = "valid-rule", 
                UpdatedBy = "test-user",
                IsChatForced = true
            };
            
            var existingRule = CreateTestRule("valid-rule", 5);
            SetupRuleExists("valid-rule", existingRule);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateRuleAsync(request, invalidUser));
        }

        #endregion

        #region DeleteRuleByNameAsync Tests

        [Fact]
        public async Task DeleteRuleByNameAsync_DeletesRule_WhenRuleExists()
        {
            // Arrange
            var existingRule = CreateTestRule("delete-me", 5);
            SetupRuleExists("delete-me", existingRule);

            // Act
            await service.DeleteRuleByNameAsync("delete-me");

            // Assert
            await mockCosmosClient.Received(1).DeleteItemAsync(Arg.Any<string>(), "delete-me", Arg.Any<string>());
        }

        [Fact]
        public async Task DeleteRuleByNameAsync_ThrowsInvalidOperationException_WhenRuleNotFound()
        {
            // Arrange
            mockCosmosClient.GetItemByIdAsync<LiveChatRule>(Arg.Any<string>(), "nonexistent-rule", Arg.Any<string>())
                .Returns((LiveChatRule?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteRuleByNameAsync("nonexistent-rule"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DeleteRuleByNameAsync_ThrowsArgumentException_WhenNameIsInvalid(string invalidName)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteRuleByNameAsync(invalidName));
        }

        [Fact]
        public async Task DeleteRuleByNameAsync_LogsSuccess_WhenDeletionCompletes()
        {
            // Arrange
            var existingRule = CreateTestRule("log-delete", 3);
            SetupRuleExists("log-delete", existingRule);

            // Act
            await service.DeleteRuleByNameAsync("log-delete");

            // Assert
            mockTelemetryService.Received(1).LogTrace<LiveChatSettingsService>(
                Arg.Is<string>(s => s.Contains("log-delete") && s.Contains("deleted successfully"))
            );
        }

        [Fact]
        public async Task DeleteRuleByNameAsync_LogsErrorAndRethrows_WhenCosmosThrows()
        {
            // Arrange
            var existingRule = CreateTestRule("error-delete", 2);
            SetupRuleExists("error-delete", existingRule);
            
            var cosmosException = new CosmosException("Delete failed", HttpStatusCode.InternalServerError, 0, "", 0);
            mockCosmosClient.DeleteItemAsync(Arg.Any<string>(), "error-delete", Arg.Any<string>())
                .Throws(cosmosException);

            // Act & Assert
            await Assert.ThrowsAsync<CosmosException>(() => service.DeleteRuleByNameAsync("error-delete"));

            mockTelemetryService.Received(1).LogError<LiveChatSettingsService>(
                Arg.Is<string>(s => s.Contains("Error in DeleteRuleByNameAsync")),
                Arg.Any<IDictionary<string, string>>()
            );
        }

        #endregion

        #region GetMaxEvaluationOrderAsync Tests

        [Fact]
        public async Task GetMaxEvaluationOrderAsync_ReturnsMaxValue_WhenRulesExist()
        {
            // Arrange
            SetupMaxEvaluationOrderQuery(42);

            // Act
            var result = await InvokeGetMaxEvaluationOrderAsync();

            // Assert
            result.Should().Be(42);
        }

        [Fact]
        public async Task GetMaxEvaluationOrderAsync_ReturnsZero_WhenNoRulesExist()
        {
            // Arrange
            SetupMaxEvaluationOrderQuery(0);

            // Act
            var result = await InvokeGetMaxEvaluationOrderAsync();

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task GetMaxEvaluationOrderAsync_ReturnsLargeValue_WhenMaxValueIsLarge()
        {
            // Arrange
            SetupMaxEvaluationOrderQuery(9999);

            // Act
            var result = await InvokeGetMaxEvaluationOrderAsync();

            // Assert
            result.Should().Be(9999);
        }

        #endregion

        #region CascadingShiftEfficientAsync Tests

        [Fact]
        public async Task CascadingShiftEfficientAsync_ShiftsSingleRule_WhenOneRuleAtOrder()
        {
            // Arrange
            var ruleAtOrder5 = CreateTestRule("rule-at-5", 5);
            SetupGetRulesAtOrAfterOrderQuery(5, new List<LiveChatRule> { ruleAtOrder5 });

            // Act
            await InvokeCascadingShiftAsync(5, "test-user");

            // Assert
            ruleAtOrder5.EvaluationOrder.Should().Be(6); // 5 + 1
            await mockCosmosClient.Received(1).UpsertItemAsync(Arg.Any<string>(), ruleAtOrder5);
        }

        [Fact]
        public async Task CascadingShiftEfficientAsync_ShiftsConsecutiveRules_WhenMultipleRulesNeedShifting()
        {
            // Arrange
            var rule1 = CreateTestRule("rule-1", 3);
            var rule2 = CreateTestRule("rule-2", 4);
            var rule3 = CreateTestRule("rule-3", 5);
            var consecutiveRules = new List<LiveChatRule> { rule1, rule2, rule3 };
            
            SetupGetRulesAtOrAfterOrderQuery(3, consecutiveRules);

            // Act
            await InvokeCascadingShiftAsync(3, "test-user");

            // Assert
            rule1.EvaluationOrder.Should().Be(4); // 3 + 1
            rule2.EvaluationOrder.Should().Be(5); // 4 + 1
            rule3.EvaluationOrder.Should().Be(6); // 5 + 1
            await mockCosmosClient.Received(3).UpsertItemAsync(Arg.Any<string>(), Arg.Any<LiveChatRule>());
        }

        [Fact]
        public async Task CascadingShiftEfficientAsync_SkipsExcludedRule_WhenExcludeNameProvided()
        {
            // Arrange
            var rule1 = CreateTestRule("exclude-me", 3);
            var rule2 = CreateTestRule("shift-me", 4);
            var rulesWithExclusion = new List<LiveChatRule> { rule1, rule2 };
            
            SetupGetRulesAtOrAfterOrderQuery(3, rulesWithExclusion);

            // Act
            await InvokeCascadingShiftAsync(3, "test-user", "exclude-me");

            // Assert
            rule1.EvaluationOrder.Should().Be(3); // Unchanged
            rule2.EvaluationOrder.Should().Be(5); // 4 + 1
            await mockCosmosClient.Received(1).UpsertItemAsync(Arg.Any<string>(), 
                Arg.Is<LiveChatRule>(r => r.Name == "shift-me"));
            await mockCosmosClient.DidNotReceive().UpsertItemAsync(Arg.Any<string>(), 
                Arg.Is<LiveChatRule>(r => r.Name == "exclude-me"));
        }

        [Fact]
        public async Task CascadingShiftEfficientAsync_HandlesGapsInOrders_WhenNonConsecutiveRules()
        {
            // Arrange - Rules at 3, 7, 8 (gap between 3 and 7)
            var rule1 = CreateTestRule("rule-at-3", 3);
            var rule2 = CreateTestRule("rule-at-7", 7);
            var rule3 = CreateTestRule("rule-at-8", 8);
            var rulesWithGaps = new List<LiveChatRule> { rule1, rule2, rule3 };
            
            SetupGetRulesAtOrAfterOrderQuery(3, rulesWithGaps);

            // Act
            await InvokeCascadingShiftAsync(3, "test-user");

            // Assert
            rule1.EvaluationOrder.Should().Be(4); // 3 + 1
            rule2.EvaluationOrder.Should().Be(7); // No change (gap)
            rule3.EvaluationOrder.Should().Be(8); // No change (gap)
            await mockCosmosClient.Received(1).UpsertItemAsync(Arg.Any<string>(), rule1);
        }

        [Fact]
        public async Task CascadingShiftEfficientAsync_DoesNothing_WhenNoRulesAtOrAfterOrder()
        {
            // Arrange
            SetupGetRulesAtOrAfterOrderQuery(10, new List<LiveChatRule>());

            // Act
            await InvokeCascadingShiftAsync(10, "test-user");

            // Assert
            await mockCosmosClient.DidNotReceive().UpsertItemAsync(Arg.Any<string>(), Arg.Any<LiveChatRule>());
        }

        #endregion

        #region Test Helper Methods

        /// <summary>
        /// Helper method to invoke private CascadingShiftEfficientAsync method via reflection.
        /// </summary>
        private async Task InvokeCascadingShiftAsync(int startOrder, string user, string? excludeName = null)
        {
            var method = typeof(LiveChatSettingsService).GetMethod("CascadingShiftEfficientAsync", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task)method!.Invoke(service, new object?[] { startOrder, user, excludeName })!;
            await task;
        }

        /// <summary>
        /// Helper method to invoke private GetMaxEvaluationOrderAsync method via reflection.
        /// </summary>
        private async Task<int> InvokeGetMaxEvaluationOrderAsync()
        {
            var method = typeof(LiveChatSettingsService).GetMethod("GetMaxEvaluationOrderAsync", 
                BindingFlags.Public | BindingFlags.Instance);
            var task = (Task<int>)method!.Invoke(service, Array.Empty<object>())!;
            return await task;
        }

        /// <summary>
        /// Sets up mock for rules at or after specified evaluation order.
        /// </summary>
        private void SetupGetRulesAtOrAfterOrderQuery(int order, List<LiveChatRule> rules)
        {
            var mockIterator = Substitute.For<FeedIterator<LiveChatRule>>();
            var mockResponse = Substitute.For<FeedResponse<LiveChatRule>>();
            mockResponse.Resource.Returns(rules);
            mockIterator.HasMoreResults.Returns(true, false);
            mockIterator.ReadNextAsync().Returns(mockResponse);

            mockCosmosClient.QueryItemsIteratorAsync<LiveChatRule>(
                Arg.Any<string>(), 
                Arg.Is<QueryDefinition>(q => q.QueryText.Contains("evaluationOrder"))
            ).Returns(mockIterator);
        }

        /// <summary>
        /// Sets up mock for maximum evaluation order query.
        /// </summary>
        private void SetupMaxEvaluationOrderQuery(int maxValue)
        {
            var mockIterator = Substitute.For<FeedIterator<int>>();
            var mockResponse = Substitute.For<FeedResponse<int>>();
            mockResponse.Resource.Returns(new List<int> { maxValue });
            mockIterator.HasMoreResults.Returns(true, false);
            mockIterator.ReadNextAsync().Returns(mockResponse);

            mockCosmosClient.QueryItemsIteratorAsync<int>(Arg.Any<string>(), Arg.Any<QueryDefinition>())
                .Returns(mockIterator);
        }

        /// <summary>
        /// Sets up mock for query iterator responses (used by MatchRuleAsync).
        /// </summary>
        private void SetupQueryIteratorResponse(List<LiveChatRule> rules)
        {
            var mockIterator = Substitute.For<FeedIterator<LiveChatRule>>();
            var mockResponse = Substitute.For<FeedResponse<LiveChatRule>>();
            mockResponse.Resource.Returns(rules);
            mockIterator.HasMoreResults.Returns(true, false);
            mockIterator.ReadNextAsync().Returns(mockResponse);

            mockCosmosClient.QueryItemsIteratorAsync<LiveChatRule>(Arg.Any<string>(), Arg.Any<QueryDefinition>())
                .Returns(mockIterator);
        }

        /// <summary>
        /// Sets up mock for rule existence check (rule does not exist).
        /// </summary>
        private void SetupRuleDoesNotExist(string ruleName)
        {
            mockCosmosClient.GetItemByIdAsync<LiveChatRule>(Arg.Any<string>(), ruleName, Arg.Any<string>())
                .Returns((LiveChatRule?)null);
        }

        /// <summary>
        /// Sets up mock for rule existence check (rule exists).
        /// </summary>
        private void SetupRuleExists(string ruleName, LiveChatRule rule)
        {
            mockCosmosClient.GetItemByIdAsync<LiveChatRule>(Arg.Any<string>(), ruleName, Arg.Any<string>())
                .Returns(rule);
        }

        /// <summary>
        /// Creates a test LiveChatRule with specified name and evaluation order.
        /// </summary>
        private static LiveChatRule CreateTestRule(string name, int? evaluationOrder)
        {
            return new LiveChatRule
            {
                Name = name,
                EvaluationOrder = evaluationOrder,
                AllowedServiceLevels = new List<string> { "Standard" },
                IsChatForced = false,
                AllowRestricted = false,
                AllowedSaps = new List<Guid> { Guid.NewGuid() },
                ExcludedServiceIds = new List<int>()
            };
        }

        #endregion
    }
}