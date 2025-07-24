// using CRM.ICon.Modality.Controllers;
// using CRM.ICon.Modality.Model.LiveChatSettings;
// using CRM.ICon.Modality.Model.LiveChatSettings.Requests;
// using CRM.ICon.Modality.Model.LiveChatSettings.Responses;
// using CRM.ICon.Modality.Services.LiveChatSettings;
// using FluentAssertions;
// using Microsoft.AspNetCore.Mvc;
// using Moq;
// using Xunit;

// namespace LiveChatSettings.Tests.Controllers
// {
//     public class LiveChatSettingsControllerTests
//     {
//         private readonly Mock<ILiveChatSettingsService> _mockLiveChatService;
//         private readonly LiveChatSettingsController _controller;

//         public LiveChatSettingsControllerTests()
//         {
//             _mockLiveChatService = new Mock<ILiveChatSettingsService>();
//             _controller = new LiveChatSettingsController(_mockLiveChatService.Object);
//         }

//         #region GetAllRules Tests

//         // Test: Returns 200 OK with populated list when rules exist in the database
//         [Fact]
//         public async Task GetAllRules_WhenRulesExist_ShouldReturnOkWithPopulatedList()
//         {
//             // Arrange
//             var expectedRules = new List<LiveChatRule>
//             {
//                 new LiveChatRule
//                 {
//                     Name = "rule1",
//                     EvaluationOrder = 1,
//                     AllowedServiceLevels = new List<string> { "professional" },
//                     AllowRestricted = true,
//                     AllowedSaps = new List<Guid>
//                     {
//                         Guid.Parse("12345678-1234-1234-1234-123456789012"),
//                     },
//                     ExcludedServiceIds = new List<int> { 123 },
//                     IsChatForced = false,
//                 },
//                 new LiveChatRule
//                 {
//                     Name = "rule2",
//                     EvaluationOrder = 2,
//                     AllowedServiceLevels = new List<string> { "premier" },
//                     AllowRestricted = false,
//                     AllowedSaps = new List<Guid>
//                     {
//                         Guid.Parse("87654321-4321-4321-4321-210987654321"),
//                     },
//                     ExcludedServiceIds = new List<int> { 456 },
//                     IsChatForced = true,
//                 },
//             };

//             _mockLiveChatService.Setup(x => x.GetAllRulesAsync()).ReturnsAsync(expectedRules);

//             // Act
//             var result = await _controller.GetAllItems();

//             // Assert
//             result.Should().BeOfType<OkObjectResult>();
//             var okResult = result as OkObjectResult;
//             okResult!.Value.Should().BeEquivalentTo(expectedRules);

//             _mockLiveChatService.Verify(x => x.GetAllRulesAsync(), Times.Once);
//         }

//         // Test: Returns 200 OK with empty list when no rules exist in the database
//         [Fact]
//         public async Task GetAllRules_WhenNoRulesExist_ShouldReturnOkWithEmptyList()
//         {
//             // Arrange
//             var emptyRules = new List<LiveChatRule>();
//             _mockLiveChatService.Setup(x => x.GetAllRulesAsync()).ReturnsAsync(emptyRules);

//             // Act
//             var result = await _controller.GetAllItems();

//             // Assert
//             result.Should().BeOfType<OkObjectResult>();
//             var okResult = result as OkObjectResult;
//             okResult!.Value.Should().BeEquivalentTo(emptyRules);
//         }

//         // Test: Returns 500 Internal Server Error when service throws an exception
//         [Fact]
//         public async Task GetAllRules_WhenServiceThrowsException_ShouldReturn500()
//         {
//             // Arrange
//             _mockLiveChatService
//                 .Setup(x => x.GetAllRulesAsync())
//                 .ThrowsAsync(new Exception("Database error"));

//             // Act
//             var result = await _controller.GetAllItems();

//             // Assert
//             result.Should().BeOfType<ObjectResult>();
//             var objectResult = result as ObjectResult;
//             objectResult!.StatusCode.Should().Be(500);
//             objectResult.Value.Should().Be("An unexpected error occurred while retrieving rules.");
//         }

//         #endregion

//         #region GetRuleByName Tests

//         // Test: Returns 200 OK with rule when valid name is provided (tests case conversion)
//         [Fact]
//         public async Task GetRuleByName_WithValidName_ShouldReturnOkWithRule()
//         {
//             // Arrange
//             var expectedRule = new LiveChatRule
//             {
//                 Name = "test-rule",
//                 EvaluationOrder = 1,
//                 AllowedServiceLevels = new List<string> { "professional" },
//                 AllowRestricted = true,
//                 AllowedSaps = new List<Guid> { Guid.Parse("12345678-1234-1234-1234-123456789012") },
//                 ExcludedServiceIds = new List<int> { 123 },
//                 IsChatForced = false,
//             };

//             _mockLiveChatService
//                 .Setup(x => x.GetRuleByNameAsync("test-rule"))
//                 .ReturnsAsync(expectedRule);

//             // Act
//             var result = await _controller.GetRuleByName("TEST-RULE"); // Test case conversion

//             // Assert
//             result.Should().BeOfType<OkObjectResult>();
//             var okResult = result as OkObjectResult;
//             okResult!.Value.Should().BeEquivalentTo(expectedRule);

//             // Verify that the name was converted to lowercase
//             _mockLiveChatService.Verify(x => x.GetRuleByNameAsync("test-rule"), Times.Once);
//         }

//         // Test: Returns 400 Bad Request when null name is provided
//         [Fact]
//         public async Task GetRuleByName_WithNullName_ShouldReturnBadRequest()
//         {
//             // Act
//             var result = await _controller.GetRuleByName(null);

//             // Assert
//             result.Should().BeOfType<BadRequestObjectResult>();
//             var badRequestResult = result as BadRequestObjectResult;
//             badRequestResult!.Value.Should().Be("Rule name cannot be null or empty.");

//             _mockLiveChatService.Verify(x => x.GetRuleByNameAsync(It.IsAny<string>()), Times.Never);
//         }

//         // Test: Returns 400 Bad Request when empty string name is provided
//         [Fact]
//         public async Task GetRuleByName_WithEmptyName_ShouldReturnBadRequest()
//         {
//             // Act
//             var result = await _controller.GetRuleByName("");

//             // Assert
//             result.Should().BeOfType<BadRequestObjectResult>();
//             var badRequestResult = result as BadRequestObjectResult;
//             badRequestResult!.Value.Should().Be("Rule name cannot be null or empty.");
//         }

//         // Test: Returns 404 Not Found when rule with given name doesn't exist
//         [Fact]
//         public async Task GetRuleByName_WhenRuleNotFound_ShouldReturnNotFound()
//         {
//             // Arrange
//             _mockLiveChatService
//                 .Setup(x => x.GetRuleByNameAsync("nonexistent"))
//                 .ReturnsAsync((LiveChatRule?)null);

//             // Act
//             var result = await _controller.GetRuleByName("nonexistent");

//             // Assert
//             result.Should().BeOfType<NotFoundObjectResult>();
//             var notFoundResult = result as NotFoundObjectResult;
//             notFoundResult!.Value.Should().Be("Rule 'nonexistent' not found.");
//         }

//         // Test: Returns 500 Internal Server Error when service throws an exception
//         [Fact]
//         public async Task GetRuleByName_WhenServiceThrowsException_ShouldReturn500()
//         {
//             // Arrange
//             _mockLiveChatService
//                 .Setup(x => x.GetRuleByNameAsync(It.IsAny<string>()))
//                 .ThrowsAsync(new Exception("Database error"));

//             // Act
//             var result = await _controller.GetRuleByName("test-rule");

//             // Assert
//             result.Should().BeOfType<ObjectResult>();
//             var objectResult = result as ObjectResult;
//             objectResult!.StatusCode.Should().Be(500);
//             objectResult
//                 .Value.Should()
//                 .Be("An unexpected error occurred while retrieving the rule.");
//         }

//         #endregion

//         #region IsChatEligible Tests

//         // Test: Returns 200 OK with eligible=true when a matching rule is found
//         [Fact]
//         public async Task IsChatEligible_WithValidRequest_WhenRuleMatches_ShouldReturnEligibleTrue()
//         {
//             // Arrange
//             var matchRequest = new MatchRuleRequest
//             {
//                 ServiceLevel = "professional",
//                 IsRestricted = false,
//                 SapId = Guid.Parse("12345678-1234-1234-1234-123456789012"),
//                 ServiceId = 123,
//             };

//             var matchedRule = new LiveChatRule
//             {
//                 Name = "matching-rule",
//                 IsChatForced = true,
//                 AllowedServiceLevels = new List<string> { "professional" },
//                 AllowRestricted = false,
//                 AllowedSaps = new List<Guid> { Guid.Parse("12345678-1234-1234-1234-123456789012") },
//                 ExcludedServiceIds = new List<int> { 456 },
//             };

//             _mockLiveChatService
//                 .Setup(x => x.MatchRuleAsync(It.IsAny<MatchRuleRequest>()))
//                 .ReturnsAsync(matchedRule);

//             // Act
//             var result = await _controller.MatchRuleAsync(matchRequest);

//             // Assert
//             result.Should().BeOfType<OkObjectResult>();
//             var okResult = result as OkObjectResult;
//             var response = okResult!.Value as IsChatEligibleResponse;

//             response.Should().NotBeNull();
//             response!.IsChatEligible.Should().BeTrue();
//             response.IsChatForced.Should().BeTrue();
//         }

//         // Test: Returns 200 OK with eligible=false when no matching rule is found
//         [Fact]
//         public async Task IsChatEligible_WithValidRequest_WhenNoRuleMatches_ShouldReturnEligibleFalse()
//         {
//             // Arrange
//             var matchRequest = new MatchRuleRequest
//             {
//                 ServiceLevel = "basic",
//                 IsRestricted = true,
//                 SapId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
//                 ServiceId = 999,
//             };

//             _mockLiveChatService
//                 .Setup(x => x.MatchRuleAsync(It.IsAny<MatchRuleRequest>()))
//                 .ReturnsAsync((LiveChatRule?)null);

//             // Act
//             var result = await _controller.MatchRuleAsync(matchRequest);

//             // Assert
//             result.Should().BeOfType<OkObjectResult>();
//             var okResult = result as OkObjectResult;
//             var response = okResult!.Value as IsChatEligibleResponse;

//             response.Should().NotBeNull();
//             response!.IsChatEligible.Should().BeFalse();
//             response.IsChatForced.Should().BeFalse();
//         }

//         // Test: Returns 400 Bad Request when request object is null
//         [Fact]
//         public async Task IsChatEligible_WithNullRequest_ShouldReturnBadRequest()
//         {
//             // Act
//             var result = await _controller.MatchRuleAsync(null);

//             // Assert
//             result.Should().BeOfType<BadRequestObjectResult>();
//             var badRequestResult = result as BadRequestObjectResult;
//             badRequestResult!.Value.Should().Be("Match request cannot be null.");
//         }

//         // Test: Returns 400 Bad Request when model validation fails (invalid service level format)
//         [Fact]
//         public async Task IsChatEligible_WithInvalidModelState_ShouldReturnBadRequest()
//         {
//             // Arrange
//             var matchRequest = new MatchRuleRequest
//             {
//                 ServiceLevel = "invalid service level with spaces", // Invalid format
//                 IsRestricted = false,
//                 SapId = Guid.Parse("12345678-1234-1234-1234-123456789012"),
//                 ServiceId = 123,
//             };

//             // Simulate ModelState validation error
//             _controller.ModelState.AddModelError(
//                 "ServiceLevel",
//                 "Service level can only contain letters, numbers, underscores, and hyphens."
//             );

//             // Act
//             var result = await _controller.MatchRuleAsync(matchRequest);

//             // Assert
//             result.Should().BeOfType<BadRequestObjectResult>();
//             var badRequestResult = result as BadRequestObjectResult;
//             badRequestResult!.Value.Should().BeOfType<SerializableError>();
//         }

//         // Test: Returns 500 Internal Server Error when service throws an exception
//         [Fact]
//         public async Task IsChatEligible_WhenServiceThrowsException_ShouldReturn500()
//         {
//             // Arrange
//             var matchRequest = new MatchRuleRequest
//             {
//                 ServiceLevel = "professional",
//                 IsRestricted = false,
//                 SapId = Guid.Parse("12345678-1234-1234-1234-123456789012"),
//                 ServiceId = 123,
//             };

//             _mockLiveChatService
//                 .Setup(x => x.MatchRuleAsync(It.IsAny<MatchRuleRequest>()))
//                 .ThrowsAsync(new Exception("Database error"));

//             // Act
//             var result = await _controller.MatchRuleAsync(matchRequest);

//             // Assert
//             result.Should().BeOfType<ObjectResult>();
//             var objectResult = result as ObjectResult;
//             objectResult!.StatusCode.Should().Be(500);
//             objectResult
//                 .Value.Should()
//                 .Be("An unexpected error occurred while checking chat eligibility.");
//         }

//         #endregion

//         #region CreateRule Tests

//         // Test: Returns 200 OK with success message when valid rule is created
//         [Fact]
//         public async Task CreateRule_WithValidRequest_ShouldReturnOkWithSuccessMessage()
//         {
//             // Arrange
//             var createRequest = new CreateRuleRequest
//             {
//                 Name = "new-rule",
//                 AllowedServiceLevels = new List<string> { "professional" },
//                 AllowRestricted = false,
//                 AllowedSaps = new List<Guid> { Guid.Parse("12345678-1234-1234-1234-123456789012") },
//                 ExcludedServiceIds = new List<int> { 123 },
//                 IsChatForced = true,
//                 CreatedBy = "test-user",
//                 EvaluationOrder = 5,
//             };

//             var createdRule = new LiveChatRule
//             {
//                 Name = "new-rule",
//                 AllowedServiceLevels = createRequest.AllowedServiceLevels,
//                 AllowRestricted = createRequest.AllowRestricted,
//                 AllowedSaps = createRequest.AllowedSaps,
//                 ExcludedServiceIds = createRequest.ExcludedServiceIds,
//                 IsChatForced = createRequest.IsChatForced,
//                 EvaluationOrder = createRequest.EvaluationOrder,
//             };

//             _mockLiveChatService
//                 .Setup(x => x.CreateRuleAsync(It.IsAny<LiveChatRule>(), "test-user"))
//                 .ReturnsAsync(createdRule);

//             // Act
//             var result = await _controller.CreateItem(createRequest);

//             // Assert
//             result.Should().BeOfType<OkObjectResult>();
//             var okResult = result as OkObjectResult;
//             okResult!.Value.Should().Be("Rule 'new-rule' created successfully.");

//             _mockLiveChatService.Verify(
//                 x => x.CreateRuleAsync(It.IsAny<LiveChatRule>(), "test-user"),
//                 Times.Once
//             );
//         }

//         // Test: Returns 400 Bad Request when request body is null
//         [Fact]
//         public async Task CreateRule_WithNullRequest_ShouldReturnBadRequest()
//         {
//             // Act
//             var result = await _controller.CreateItem(null);

//             // Assert
//             result.Should().BeOfType<BadRequestObjectResult>();
//             var badRequestResult = result as BadRequestObjectResult;
//             badRequestResult!.Value.Should().Be("Request body cannot be null.");
//         }

//         // Test: Returns 400 Bad Request when evaluation order is negative (validation failure)
//         [Fact]
//         public async Task CreateRule_WithNegativeEvaluationOrder_ShouldReturnBadRequest()
//         {
//             // Arrange
//             var createRequest = new CreateRuleRequest
//             {
//                 Name = "test-rule",
//                 AllowedServiceLevels = new List<string> { "professional" },
//                 AllowRestricted = false,
//                 AllowedSaps = new List<Guid> { Guid.Parse("12345678-1234-1234-1234-123456789012") },
//                 ExcludedServiceIds = new List<int> { 123 },
//                 IsChatForced = true,
//                 CreatedBy = "test-user",
//                 EvaluationOrder = -1, // Invalid negative value
//             };

//             // Simulate validation error for negative evaluation order
//             _controller.ModelState.AddModelError("EvaluationOrder", "EvaluationOrder must be >= 0");

//             // Act
//             var result = await _controller.CreateItem(createRequest);

//             // Assert
//             result.Should().BeOfType<BadRequestObjectResult>();
//             var badRequestResult = result as BadRequestObjectResult;
//             badRequestResult!.Value.Should().BeOfType<SerializableError>();
//         }

//         // Test: Returns 400 Bad Request when rule name contains spaces (validation failure)
//         [Fact]
//         public async Task CreateRule_WithInvalidNameContainingSpaces_ShouldReturnBadRequest()
//         {
//             // Arrange
//             var createRequest = new CreateRuleRequest
//             {
//                 Name = "rule with spaces", // Invalid - contains spaces
//                 AllowedServiceLevels = new List<string> { "professional" },
//                 AllowRestricted = false,
//                 AllowedSaps = new List<Guid> { Guid.Parse("12345678-1234-1234-1234-123456789012") },
//                 ExcludedServiceIds = new List<int> { 123 },
//                 IsChatForced = true,
//                 CreatedBy = "test-user",
//             };

//             // Simulate validation error for invalid name
//             _controller.ModelState.AddModelError(
//                 "Name",
//                 "Rule name can only contain letters, numbers, hyphens, and underscores (no spaces)."
//             );

//             // Act
//             var result = await _controller.CreateItem(createRequest);

//             // Assert
//             result.Should().BeOfType<BadRequestObjectResult>();
//             var badRequestResult = result as BadRequestObjectResult;
//             badRequestResult!.Value.Should().BeOfType<SerializableError>();
//         }

//         // Test: Returns 409 Conflict when attempting to create a rule that already exists
//         [Fact]
//         public async Task CreateRule_WhenRuleAlreadyExists_ShouldReturnConflict()
//         {
//             // Arrange
//             var createRequest = new CreateRuleRequest
//             {
//                 Name = "existing-rule",
//                 AllowedServiceLevels = new List<string> { "professional" },
//                 AllowRestricted = false,
//                 AllowedSaps = new List<Guid> { Guid.Parse("12345678-1234-1234-1234-123456789012") },
//                 ExcludedServiceIds = new List<int> { 123 },
//                 IsChatForced = true,
//                 CreatedBy = "test-user",
//             };

//             _mockLiveChatService
//                 .Setup(x => x.CreateRuleAsync(It.IsAny<LiveChatRule>(), It.IsAny<string>()))
//                 .ThrowsAsync(
//                     new InvalidOperationException(
//                         "A rule with name 'existing-rule' already exists."
//                     )
//                 );

//             // Act
//             var result = await _controller.CreateItem(createRequest);

//             // Assert
//             result.Should().BeOfType<ConflictObjectResult>();
//             var conflictResult = result as ConflictObjectResult;
//             conflictResult!.Value.Should().Be("A rule with name 'existing-rule' already exists.");
//         }

//         // Test: Returns 500 Internal Server Error when service throws a general exception
//         [Fact]
//         public async Task CreateRule_WhenServiceThrowsGeneralException_ShouldReturn500()
//         {
//             // Arrange
//             var createRequest = new CreateRuleRequest
//             {
//                 Name = "new-rule",
//                 AllowedServiceLevels = new List<string> { "professional" },
//                 AllowRestricted = false,
//                 AllowedSaps = new List<Guid> { Guid.Parse("12345678-1234-1234-1234-123456789012") },
//                 ExcludedServiceIds = new List<int> { 123 },
//                 IsChatForced = true,
//                 CreatedBy = "test-user",
//             };

//             _mockLiveChatService
//                 .Setup(x => x.CreateRuleAsync(It.IsAny<LiveChatRule>(), It.IsAny<string>()))
//                 .ThrowsAsync(new Exception("Database connection failed"));

//             // Act
//             var result = await _controller.CreateItem(createRequest);

//             // Assert
//             result.Should().BeOfType<ObjectResult>();
//             var objectResult = result as ObjectResult;
//             objectResult!.StatusCode.Should().Be(500);
//             objectResult.Value.Should().Be("An unexpected error occurred while creating the rule.");
//         }

//         #endregion

//         #region UpdateRule Tests

//         // Test: Returns 200 OK with success message when rule is successfully updated
//         [Fact]
//         public async Task UpdateRule_WithValidRequest_ShouldReturnOkWithSuccessMessage()
//         {
//             // Arrange
//             var updateRequest = new UpdateRuleRequest
//             {
//                 Name = "existing-rule",
//                 AllowedServiceLevels = new List<string> { "premier" },
//                 AllowRestricted = true,
//                 EvaluationOrder = 10,
//                 UpdatedBy = "test-user",
//             };

//             _mockLiveChatService
//                 .Setup(x => x.UpdateRuleAsync(It.IsAny<UpdateRuleRequest>(), "test-user"))
//                 .Returns(Task.CompletedTask);

//             // Act
//             var result = await _controller.UpdateRule(updateRequest);

//             // Assert
//             result.Should().BeOfType<OkObjectResult>();
//             var okResult = result as OkObjectResult;
//             okResult!.Value.Should().Be("Rule 'existing-rule' updated successfully.");

//             _mockLiveChatService.Verify(
//                 x => x.UpdateRuleAsync(It.IsAny<UpdateRuleRequest>(), "test-user"),
//                 Times.Once
//             );
//         }

//         // Test: Returns 400 Bad Request when request body is null
//         [Fact]
//         public async Task UpdateRule_WithNullRequest_ShouldReturnBadRequest()
//         {
//             // Act
//             var result = await _controller.UpdateRule(null);

//             // Assert
//             result.Should().BeOfType<BadRequestObjectResult>();
//             var badRequestResult = result as BadRequestObjectResult;
//             badRequestResult!.Value.Should().Be("Request body cannot be null.");
//         }

//         // Test: Returns 400 Bad Request when model validation fails (invalid name and negative evaluation order)
//         [Fact]
//         public async Task UpdateRule_WithInvalidModelState_ShouldReturnBadRequest()
//         {
//             // Arrange
//             var updateRequest = new UpdateRuleRequest
//             {
//                 Name = "test rule", // Invalid - contains space
//                 EvaluationOrder = -5, // Invalid - negative
//                 UpdatedBy = "test-user",
//             };

//             // Simulate validation errors
//             _controller.ModelState.AddModelError(
//                 "Name",
//                 "Rule name can only contain letters, numbers, hyphens, and underscores (no spaces)."
//             );
//             _controller.ModelState.AddModelError(
//                 "EvaluationOrder",
//                 "EvaluationOrder must be ≥ 0 if provided."
//             );

//             // Act
//             var result = await _controller.UpdateRule(updateRequest);

//             // Assert
//             result.Should().BeOfType<BadRequestObjectResult>();
//             var badRequestResult = result as BadRequestObjectResult;
//             badRequestResult!.Value.Should().BeOfType<SerializableError>();
//         }

//         // Test: Returns 404 Not Found when attempting to update a rule that doesn't exist
//         [Fact]
//         public async Task UpdateRule_WhenRuleDoesNotExist_ShouldReturnNotFound()
//         {
//             // Arrange
//             var updateRequest = new UpdateRuleRequest
//             {
//                 Name = "nonexistent-rule",
//                 AllowedServiceLevels = new List<string> { "premier" },
//                 UpdatedBy = "test-user",
//             };

//             _mockLiveChatService
//                 .Setup(x => x.UpdateRuleAsync(It.IsAny<UpdateRuleRequest>(), It.IsAny<string>()))
//                 .ThrowsAsync(new InvalidOperationException("Rule 'nonexistent-rule' not found."));

//             // Act
//             var result = await _controller.UpdateRule(updateRequest);

//             // Assert
//             result.Should().BeOfType<NotFoundObjectResult>();
//             var notFoundResult = result as NotFoundObjectResult;
//             notFoundResult!.Value.Should().Be("Rule 'nonexistent-rule' not found.");
//         }

//         // Test: Returns 500 Internal Server Error when service throws a general exception
//         [Fact]
//         public async Task UpdateRule_WhenServiceThrowsGeneralException_ShouldReturn500()
//         {
//             // Arrange
//             var updateRequest = new UpdateRuleRequest
//             {
//                 Name = "existing-rule",
//                 AllowedServiceLevels = new List<string> { "premier" },
//                 UpdatedBy = "test-user",
//             };

//             _mockLiveChatService
//                 .Setup(x => x.UpdateRuleAsync(It.IsAny<UpdateRuleRequest>(), It.IsAny<string>()))
//                 .ThrowsAsync(new Exception("Database connection failed"));

//             // Act
//             var result = await _controller.UpdateRule(updateRequest);

//             // Assert
//             result.Should().BeOfType<ObjectResult>();
//             var objectResult = result as ObjectResult;
//             objectResult!.StatusCode.Should().Be(500);
//             objectResult.Value.Should().Be("An unexpected error occurred while updating the rule.");
//         }

//         #endregion

//         #region DeleteRule Tests

//         // Test: Returns 200 OK with success message when rule is successfully deleted (tests case conversion)
//         [Fact]
//         public async Task DeleteRule_WithValidName_ShouldReturnOkWithSuccessMessage()
//         {
//             // Arrange
//             _mockLiveChatService
//                 .Setup(x => x.DeleteRuleByNameAsync("test-rule"))
//                 .Returns(Task.CompletedTask);

//             // Act
//             var result = await _controller.DeleteRule("TEST-RULE");

//             // Assert
//             result.Should().BeOfType<OkObjectResult>();
//             var okResult = result as OkObjectResult;
//             okResult!.Value.Should().Be("Rule 'test-rule' deleted successfully.");

//             _mockLiveChatService.Verify(x => x.DeleteRuleByNameAsync("test-rule"), Times.Once);
//         }

//         // Test: Returns 400 Bad Request when rule name is null
//         [Fact]
//         public async Task DeleteRule_WithNullName_ShouldReturnBadRequest()
//         {
//             // Act
//             var result = await _controller.DeleteRule(null);

//             // Assert
//             result.Should().BeOfType<BadRequestObjectResult>();
//             var badRequestResult = result as BadRequestObjectResult;
//             badRequestResult!.Value.Should().Be("Rule name cannot be null or empty.");
//         }

//         // Test: Returns 400 Bad Request when rule name is empty string
//         [Fact]
//         public async Task DeleteRule_WithEmptyName_ShouldReturnBadRequest()
//         {
//             // Act
//             var result = await _controller.DeleteRule("");

//             // Assert
//             result.Should().BeOfType<BadRequestObjectResult>();
//             var badRequestResult = result as BadRequestObjectResult;
//             badRequestResult!.Value.Should().Be("Rule name cannot be null or empty.");
//         }

//         // Test: Returns 404 Not Found when attempting to delete a rule that doesn't exist
//         [Fact]
//         public async Task DeleteRule_WhenRuleDoesNotExist_ShouldReturnNotFound()
//         {
//             // Arrange
//             _mockLiveChatService
//                 .Setup(x => x.DeleteRuleByNameAsync("nonexistent"))
//                 .ThrowsAsync(new InvalidOperationException("Rule 'nonexistent' not found."));

//             // Act
//             var result = await _controller.DeleteRule("nonexistent");

//             // Assert
//             result.Should().BeOfType<NotFoundObjectResult>();
//             var notFoundResult = result as NotFoundObjectResult;
//             notFoundResult!.Value.Should().Be("Rule 'nonexistent' not found.");
//         }

//         // Test: Returns 500 Internal Server Error when service throws a general exception
//         [Fact]
//         public async Task DeleteRule_WhenServiceThrowsGeneralException_ShouldReturn500()
//         {
//             // Arrange
//             _mockLiveChatService
//                 .Setup(x => x.DeleteRuleByNameAsync(It.IsAny<string>()))
//                 .ThrowsAsync(new Exception("Database connection failed"));

//             // Act
//             var result = await _controller.DeleteRule("test-rule");

//             // Assert
//             result.Should().BeOfType<ObjectResult>();
//             var objectResult = result as ObjectResult;
//             objectResult!.StatusCode.Should().Be(500);
//             objectResult.Value.Should().Be("An unexpected error occurred while deleting the rule.");
//         }

//         #endregion
//     }
// }
