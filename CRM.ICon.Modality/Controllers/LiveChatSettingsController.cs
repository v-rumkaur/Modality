// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LiveChatSettingsController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model.LiveChatSettings;
using CRM.ICon.Modality.Model.LiveChatSettings.Requests;
using CRM.ICon.Modality.Model.LiveChatSettings.Responses;
using CRM.ICon.Modality.Services.LiveChatSettings;
using Microsoft.AspNetCore.Mvc;
using static CRM.ICon.Modality.Helpers.StringHelper;

namespace CRM.ICon.Modality.Controllers
{
    [ApiController]
    [Route("api/v1/livechatsettings")]
    public class LiveChatSettingsController : ControllerBase
    {
        private readonly ITelemetryService telemetryService;
        private readonly ILiveChatSettingsService liveChatSettingsService;

        public LiveChatSettingsController(
            ILiveChatSettingsService service,
            ITelemetryService telemetryService
        )
        {
            liveChatSettingsService = service ?? throw new ArgumentNullException(nameof(service));
            this.telemetryService =
                telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
        }

        /// <summary>
        /// Gets all live chat rules.
        /// </summary>
        /// <returns> A collection of all live chat rules. </returns>
        /// <response code="200">Returns all rules.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpGet("getAllRules")]
        [ProducesResponseType(typeof(IEnumerable<LiveChatRule>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAllItems()
        {
            try
            {
                var rules = await liveChatSettingsService.GetAllRulesAsync();
                return Ok(rules);
            }
            catch (Exception ex)
            {
                telemetryService.LogError<LiveChatSettingsController>($"Unexpected error getting all rules: {ex.Message}", ex.ToDictionary());
                return StatusCode(500, "An unexpected error occurred while retrieving rules.");
            }
        }

        /// <summary>
        /// Gets a live chat rule by name.
        /// </summary>
        /// <param name="name">The name of the rule to retrieve (case-insensitive).</param>
        /// <returns>The requested rule or NotFound if it doesn't exist.</returns>
        /// <response code="200">Returns the requested rule.</response>
        /// <response code="400">If the rule name is invalid.</response>
        /// <response code="404">If the rule is not found.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpGet("getRuleByName")]
        [ProducesResponseType(typeof(LiveChatRule), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRuleByName([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Rule name cannot be null or empty.");

            try
            {
                StringToLower(ref name);
                var rule = await liveChatSettingsService.GetRuleByNameAsync(name);
                return rule == null ? NotFound($"Rule '{name}' not found.") : Ok(rule);
            }
            catch (Exception ex)
            {
                telemetryService.LogError<LiveChatSettingsController>($"Unexpected error getting rule by name: {ex.Message}", ex.ToDictionary());
                return StatusCode(500, "An unexpected error occurred while retrieving the rule.");
            }
        }

        /// <summary>
        /// Checks if a user is eligible for live chat based on the provided user context.
        /// </summary>
        /// <param name="request">The match criteria for determining chat eligibility.</param>
        /// <returns>Chat eligibility response indicating availability and forced status.</returns>
        /// <response code="200">Returns chat eligibility status.</response>
        /// <response code="400">If the request is invalid.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpGet("isChatEligible")]
        [ProducesResponseType(typeof(IsChatEligibleResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> MatchRuleAsync([FromQuery] MatchRuleRequest request)
        {
            if (request == null)
                return BadRequest("Match request cannot be null.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                ObjectToLower(request);
                var match = await liveChatSettingsService.MatchRuleAsync(request);

                var isChatEligible = new IsChatEligibleResponse
                {
                    IsChatEligible = match != null,
                    IsChatForced = match?.IsChatForced ?? false,
                };
                return Ok(isChatEligible);
            }
            catch (Exception ex)
            {
                telemetryService.LogError<LiveChatSettingsController>($"Unexpected error checking chat eligibility: {ex.Message}", ex.ToDictionary());
                return StatusCode(500, "An unexpected error occurred while checking chat eligibility.");
            }
        }

        /// <summary>
        /// Creates a new live chat rule.
        /// </summary>
        /// <param name="request">The rule creation request containing rule details.</param>
        /// <returns>Success confirmation or error details.</returns>
        /// <response code="200">Rule created successfully.</response>
        /// <response code="400">If the request is invalid.</response>
        /// <response code="409">If a rule with the same name already exists.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpPost("createRule")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateItem([FromBody] CreateRuleRequest request)
        {
            if (request == null)
                return BadRequest("Request body cannot be null.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                ObjectToLower(request);
                await liveChatSettingsService.CreateRuleAsync(request.ToDomainModel(), request.CreatedBy);
                return Ok($"Rule '{request.Name}' created successfully.");
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                telemetryService.LogError<LiveChatSettingsController>($"Unexpected error creating rule: {ex.Message}", ex.ToDictionary());
                return StatusCode(500, "An unexpected error occurred while creating the rule.");
            }
        }

        /// <summary>
        /// Updates an existing live chat rule.
        /// </summary>
        /// <param name="request">The rule update request containing updated rule details.</param>
        /// <returns>Success confirmation or error details.</returns>
        /// <response code="200">Rule updated successfully.</response>
        /// <response code="400">If the request is invalid.</response>
        /// <response code="404">If the rule is not found.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpPut("updateRule")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateRule([FromBody] UpdateRuleRequest request)
        {
            if (request == null)
                return BadRequest("Request body cannot be null.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                ObjectToLower(request);
                await liveChatSettingsService.UpdateRuleAsync(request, request.UpdatedBy);
                return Ok($"Rule '{request.Name}' updated successfully.");
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                telemetryService.LogError<LiveChatSettingsController>($"Unexpected error updating rule: {ex.Message}", ex.ToDictionary());
                return StatusCode(500, "An unexpected error occurred while updating the rule.");
            }
        }

        /// <summary>
        /// Deletes a live chat rule by name.
        /// </summary>
        /// <param name="name">The name of the rule to delete (case-insensitive).</param>
        /// <returns>Success confirmation or error details.</returns>
        /// <response code="200">Rule deleted successfully.</response>
        /// <response code="400">If the rule name is invalid.</response>
        /// <response code="404">If the rule is not found.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpDelete("deleteRule")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteRule([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Rule name cannot be null or empty.");

            try
            {
                StringToLower(ref name);
                await liveChatSettingsService.DeleteRuleByNameAsync(name);
                return Ok($"Rule '{name}' deleted successfully.");
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                telemetryService.LogError<LiveChatSettingsController>($"Unexpected error deleting rule: {ex.Message}", ex.ToDictionary());
                return StatusCode(500, "An unexpected error occurred while deleting the rule.");
            }
        }
    }
}
