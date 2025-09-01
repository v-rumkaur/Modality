using CRM.ICon.Modality.Controllers;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model.LiveChatSettings;
using CRM.ICon.Modality.Model.LiveChatSettings.Requests;
using CRM.ICon.Modality.Model.LiveChatSettings.Responses;
using CRM.ICon.Modality.Services.LiveChatSettings;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace CRM.ICon.Modality.Tests.Controllers
{
    public class LiveChatSettingsControllerTests
    {
        private readonly ILiveChatSettingsService mockService;
        private readonly ITelemetryService mockTelemetryService;
        private readonly LiveChatSettingsController controller;

        public LiveChatSettingsControllerTests()
        {
            mockService = Substitute.For<ILiveChatSettingsService>();
            mockTelemetryService = Substitute.For<ITelemetryService>();
            controller = new LiveChatSettingsController(mockService, mockTelemetryService);
        }

        #region GetAllItems Tests

        [Fact]
        public async Task GetAllItems_ShouldReturnOk_WhenRulesExist()
        {
            // Arrange
            var rules = new List<LiveChatRule>
            {
                new LiveChatRule 
                { 
                    Name = "test-rule", 
                    IsChatForced = true,
                    AllowedServiceLevels = new List<string> { "Professional" },
                    AllowRestricted = false,
                    AllowedSaps = new List<Guid> { Guid.NewGuid() },
                    ExcludedServiceIds = new List<int> { 123 }
                }
            };
            mockService.GetAllRulesAsync().Returns(rules);

            // Act
            var result = await controller.GetAllItems();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            var returnedRules = okResult.Value.Should().BeAssignableTo<IEnumerable<LiveChatRule>>().Subject;
            returnedRules.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetAllItems_ShouldReturnEmptyList_WhenNoRulesExist()
        {
            // Arrange
            mockService.GetAllRulesAsync().Returns(new List<LiveChatRule>());

            // Act
            var result = await controller.GetAllItems();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedRules = okResult
                .Value.Should()
                .BeAssignableTo<IEnumerable<LiveChatRule>>()
                .Subject;
            returnedRules.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllItems_ShouldReturn500_WhenServiceThrows()
        {
            // Arrange
            mockService.GetAllRulesAsync().Throws(new Exception("Database error"));

            // Act
            var result = await controller.GetAllItems();

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region GetRuleByName Tests

        [Fact]
        public async Task GetRuleByName_ShouldReturnOk_WhenRuleExists()
        {
            // Arrange
            var rule = new LiveChatRule 
            { 
                Name = "test-rule", 
                IsChatForced = true,
                AllowedServiceLevels = new List<string> { "Profoessional" },
                AllowRestricted = false,
                AllowedSaps = new List<Guid> { Guid.NewGuid() },
                ExcludedServiceIds = new List<int> { 123 }
            };
            mockService.GetRuleByNameAsync("test-rule").Returns(rule);

            // Act
            var result = await controller.GetRuleByName("test-rule");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            var returnedRule = okResult.Value.Should().BeOfType<LiveChatRule>().Subject;
            returnedRule.Name.Should().Be("test-rule");
        }

        [Fact]
        public async Task GetRuleByName_ShouldReturnNotFound_WhenRuleDoesNotExist()
        {
            // Arrange
            mockService.GetRuleByNameAsync("nonexistent-rule").Returns((LiveChatRule?)null);

            // Act
            var result = await controller.GetRuleByName("nonexistent-rule");

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task GetRuleByName_ShouldReturnBadRequest_WhenNameIsInvalid(string invalidName)
        {
            // Act
            var result = await controller.GetRuleByName(invalidName);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            await mockService.DidNotReceive().GetRuleByNameAsync(Arg.Any<string>());
        }

        #endregion

        #region MatchRuleAsync Tests

        [Fact]
        public async Task MatchRuleAsync_ShouldReturnEligible_WhenRuleMatches()
        {
            // Arrange
            var request = new MatchRuleRequest
            {
                ServiceLevel = "Professional",
                SapId = Guid.NewGuid(),
                ServiceId = 123,
                IsRestricted = false,
            };
            var matchedRule = new LiveChatRule
            {
                Name = "professional-rule",
                IsChatForced = true,
                AllowedServiceLevels = new List<string> { "Professional" },
                AllowRestricted = false,
                AllowedSaps = new List<Guid> { request.SapId },
                ExcludedServiceIds = new List<int>()
            };
            mockService.MatchRuleAsync(request).Returns(matchedRule);

            // Act
            var result = await controller.MatchRuleAsync(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            var response = okResult.Value.Should().BeOfType<IsChatEligibleResponse>().Subject;
            response.IsChatEligible.Should().BeTrue();
            response.IsChatForced.Should().BeTrue();
        }

        [Fact]
        public async Task MatchRuleAsync_ShouldReturnNotEligible_WhenNoRuleMatches()
        {
            // Arrange
            var request = new MatchRuleRequest
            {
                ServiceLevel = "Basic",
                SapId = Guid.NewGuid(),
                ServiceId = 456,
                IsRestricted = true,
            };
            mockService.MatchRuleAsync(request).Returns((LiveChatRule?)null);

            // Act
            var result = await controller.MatchRuleAsync(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<IsChatEligibleResponse>().Subject;
            response.IsChatEligible.Should().BeFalse();
            response.IsChatForced.Should().BeFalse();
        }

        [Fact]
        public async Task MatchRuleAsync_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            // Act
            var result = await controller.MatchRuleAsync(null!);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
        }

        #endregion

        #region CreateItem Tests

        [Fact]
        public async Task CreateItem_ShouldReturnOk_WhenRuleCreatedSuccessfully()
        {
            // Arrange
            var request = new CreateRuleRequest
            {
                Name = "new-rule",
                AllowedServiceLevels = new List<string> { "Professional" },
                AllowRestricted = false,
                AllowedSaps = new List<Guid> { Guid.NewGuid() },
                ExcludedServiceIds = new List<int> { 123 },
                IsChatForced = true,
                CreatedBy = "test-user",
                EvaluationOrder = 10,
            };

            // Act
            var result = await controller.CreateItem(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().Be("Rule 'new-rule' created successfully.");

            // Verify service was called with correct parameters
            await mockService
                .Received(1)
                .CreateRuleAsync(Arg.Is<LiveChatRule>(r => r.Name == "new-rule"), "test-user");
        }

        [Fact]
        public async Task CreateItem_ShouldReturnConflict_WhenRuleAlreadyExists()
        {
            // Arrange
            var request = new CreateRuleRequest 
            { 
                Name = "existing-rule", 
                CreatedBy = "test-user",
                AllowedServiceLevels = new List<string> { "Professional" },
                AllowRestricted = false,
                AllowedSaps = new List<Guid> { Guid.NewGuid() },
                ExcludedServiceIds = new List<int>(),
                IsChatForced = false
            };
            mockService.CreateRuleAsync(Arg.Any<LiveChatRule>(), Arg.Any<string>())
                .Throws(new InvalidOperationException("A rule with name 'existing-rule' already exists."));

            // Act
            var result = await controller.CreateItem(request);

            // Assert
            var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
            conflictResult.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task CreateItem_ShouldReturnBadRequest_WhenRequestIsNull()
        {
            // Act
            var result = await controller.CreateItem(null!);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
        }

        #endregion

        #region UpdateRule Tests

        [Fact]
        public async Task UpdateRule_ShouldReturnOk_WhenRuleUpdatedSuccessfully()
        {
            // Arrange
            var request = new UpdateRuleRequest
            {
                Name = "existing-rule",
                UpdatedBy = "test-user",
                IsChatForced = false,
            };

            // Act
            var result = await controller.UpdateRule(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().Be("Rule 'existing-rule' updated successfully.");

            await mockService.Received(1).UpdateRuleAsync(request, "test-user");
        }

        [Fact]
        public async Task UpdateRule_ShouldReturnNotFound_WhenRuleDoesNotExist()
        {
            // Arrange
            var request = new UpdateRuleRequest { Name = "nonexistent-rule", UpdatedBy = "test-user" };
            mockService.UpdateRuleAsync(request, "test-user")
                .Throws(new InvalidOperationException("Rule 'nonexistent-rule' not found."));

            // Act
            var result = await controller.UpdateRule(request);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
        }

        #endregion

        #region DeleteRule Tests

        [Fact]
        public async Task DeleteRule_ShouldReturnOk_WhenRuleDeletedSuccessfully()
        {
            // Arrange
            var ruleName = "rule-to-delete";

            // Act
            var result = await controller.DeleteRule(ruleName);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().Be("Rule 'rule-to-delete' deleted successfully.");

            await mockService.Received(1).DeleteRuleByNameAsync("rule-to-delete");
        }

        [Fact]
        public async Task DeleteRule_ShouldReturnNotFound_WhenRuleDoesNotExist()
        {
            // Arrange
            mockService.DeleteRuleByNameAsync("nonexistent-rule")
                .Throws(new InvalidOperationException("Rule 'nonexistent-rule' not found."));

            // Act
            var result = await controller.DeleteRule("nonexistent-rule");

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task DeleteRule_ShouldReturnBadRequest_WhenNameIsInvalid(string? invalidName)
        {
            // Act
            var result = await controller.DeleteRule(invalidName!);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            await mockService.DidNotReceive().DeleteRuleByNameAsync(Arg.Any<string>());
        }

        #endregion
    }
}
