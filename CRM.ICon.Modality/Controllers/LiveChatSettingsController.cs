using CRM.ICon.Modality.Helpers;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model.LiveChatSettings.Requests;
using CRM.ICon.Modality.Model.LiveChatSettings.Responses;
using CRM.ICon.Modality.Services.LiveChatSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

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

        [Authorize]
        [HttpPost]
        [Route("isChatEligible")]
        public async Task<IActionResult> IsChatEligible([FromBody] MatchRuleRequest request)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["RequestBody"] = JsonConvert.SerializeObject(request);
            telemetryService.LogTrace<LiveChatSettingsController>(
                "Received IsChatEligible request",
                logProperties
            );

            if (request == null)
            {
                telemetryService.LogTrace<LiveChatSettingsController>(
                    "IsChatEligible request body is null"
                );
                return BadRequest("Request body is required.");
            }

            try
            {
                var match = await liveChatSettingsService.MatchUserAsync(request);
                return match != null
                    ? Ok(LiveChatRuleResponse.FromDomainModel(match))
                    : NotFound("No matching rule found.");
            }
            catch (Exception ex)
            {
                logProperties["Error"] = ex.ToString();
                telemetryService.LogTrace<LiveChatSettingsController>(
                    "Error in IsChatEligible",
                    logProperties
                );
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        [Authorize]
        [HttpPost]
        [Route("createRule")]
        public async Task<IActionResult> CreateLiveChatRule([FromBody] CreateRuleRequest request)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["RequestBody"] = JsonConvert.SerializeObject(request);
            telemetryService.LogTrace<LiveChatSettingsController>(
                "Received CreateLiveChatRule request",
                logProperties
            );

            if (request == null)
            {
                telemetryService.LogTrace<LiveChatSettingsController>(
                    "CreateLiveChatRule request body is null"
                );
                return BadRequest("Request body is required.");
            }

            try
            {
                // TODO: Get the current user from context/token if available
                var currentUser = "example@domain.com";

                var fallbackEvalOrder =
                    (await liveChatSettingsService.GetMaxEvaluationOrderAsync()) + 1;
                var rule = request.ToDomainModel(fallbackEvalOrder);

                await liveChatSettingsService.CreateRuleAsync(rule, currentUser);
                return Ok("Rule created successfully.");
            }
            catch (Exception ex)
            {
                logProperties["Error"] = ex.ToString();
                telemetryService.LogTrace<LiveChatSettingsController>(
                    "Error in CreateLiveChatRule",
                    logProperties
                );
                return StatusCode(500, "An error occurred while creating the rule.");
            }
        }

        [Authorize]
        [HttpPatch]
        [Route("updateRule")]
        public async Task<IActionResult> UpdateLiveChatRule([FromBody] UpdateRuleRequest request)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["RequestBody"] = JsonConvert.SerializeObject(request);
            telemetryService.LogTrace<LiveChatSettingsController>(
                "Received UpdateLiveChatRule request",
                logProperties
            );

            if (request == null)
            {
                telemetryService.LogTrace<LiveChatSettingsController>(
                    "UpdateLiveChatRule request body is null"
                );
                return BadRequest("Request body is required.");
            }

            try
            {
                // TODO: Get the current user from context/token if available
                var currentUser = "example@domain.com";

                var existingRule = await liveChatSettingsService.GetRuleByNameAsync(request.Name);
                if (existingRule == null)
                    return NotFound($"No rule found with name '{request.Name}'.");

                request.ApplyUpdatesTo(existingRule);
                await liveChatSettingsService.UpdateRuleAsync(existingRule, currentUser);

                return Ok("Rule updated successfully.");
            }
            catch (Exception ex)
            {
                logProperties["Error"] = ex.ToString();
                telemetryService.LogTrace<LiveChatSettingsController>(
                    "Error in UpdateLiveChatRule",
                    logProperties
                );
                return StatusCode(500, "An error occurred while updating the rule.");
            }
        }

        [Authorize]
        [HttpGet]
        [Route("getRuleByName")]
        public async Task<IActionResult> GetLiveChatRuleByName([FromQuery] string name)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["RuleName"] = name ?? "null";
            telemetryService.LogTrace<LiveChatSettingsController>(
                "Received GetLiveChatRuleByName request",
                logProperties
            );

            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Rule name is required.");
            }

            try
            {
                var rule = await liveChatSettingsService.GetRuleByNameAsync(name);
                return rule != null
                    ? Ok(LiveChatRuleResponse.FromDomainModel(rule))
                    : NotFound($"No rule found with name '{name}'.");
            }
            catch (Exception ex)
            {
                logProperties["Error"] = ex.ToString();
                telemetryService.LogTrace<LiveChatSettingsController>(
                    "Error in GetLiveChatRuleByName",
                    logProperties
                );
                return StatusCode(500, "An error occurred while retrieving the rule.");
            }
        }

        [Authorize]
        [HttpGet]
        [Route("getAllRules")]
        public async Task<IActionResult> GetAll()
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            telemetryService.LogTrace<LiveChatSettingsController>(
                "Received GetAllLiveChatRules request",
                logProperties
            );

            try
            {
                var rules = await liveChatSettingsService.GetAllAsync();
                var responses = rules.Select(LiveChatRuleResponse.FromDomainModel);
                return Ok(responses);
            }
            catch (Exception ex)
            {
                logProperties["Error"] = ex.ToString();
                telemetryService.LogTrace<LiveChatSettingsController>(
                    "Error in GetAllRules",
                    logProperties
                );
                return StatusCode(500, "An error occurred while retrieving rules.");
            }
        }

        [Authorize]
        [HttpDelete]
        [Route("deleteRuleByName")]
        public async Task<IActionResult> DeleteRuleByName([FromQuery] string name)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["RuleName"] = name ?? "null"; // Fixed: removed unnecessary JsonConvert.SerializeObject
            telemetryService.LogTrace<LiveChatSettingsController>(
                "Received DeleteLiveChatRule request",
                logProperties
            );

            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Rule name is required.");
            }

            try
            {
                var rule = await liveChatSettingsService.GetRuleByNameAsync(name);
                if (rule == null)
                    return NotFound($"No rule found with name '{name}'.");

                await liveChatSettingsService.DeleteRuleAsync(rule);
                return Ok("Rule deleted successfully.");
            }
            catch (Exception ex)
            {
                logProperties["Error"] = ex.ToString();
                telemetryService.LogTrace<LiveChatSettingsController>(
                    "Error in DeleteRuleByName",
                    logProperties
                );
                return StatusCode(500, "An error occurred while deleting the rule.");
            }
        }
    }
}
