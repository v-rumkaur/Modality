using CRM.ICon.Modality.Helpers.Cosmos;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model.LiveChatSettings.Requests;
using CRM.ICon.Modality.Model.LiveChatSettings.Responses;
using CRM.ICon.Modality.Services.LiveChatSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.ICon.Modality.Controllers
{
    [ApiController]
    [Route("api/v1/livechatsettings")]
    public class LiveChatSettingsController : ControllerBase
    {
        private readonly ITelemetryService telemetryService;
        private readonly ILiveChatSettingsService liveChatSettingsService;

        public LiveChatSettingsController(ILiveChatSettingsService service, ITelemetryService telemetryService)
        {
            liveChatSettingsService = service ?? throw new ArgumentNullException(nameof(service));
            this.telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
        }

        [Authorize]
        [HttpPost("isChatEligible")]
        public async Task<IActionResult> IsChatEligible([FromBody] MatchRuleRequest user)
        {
            var match = await liveChatSettingsService.MatchUserAsync(user);
            return match != null
                ? Ok(LiveChatRuleResponse.FromDomainModel(match))
                : NotFound("No matching rule found.");
        }

        [Authorize]
        [HttpPost("createLiveChatRule")]
        public async Task<IActionResult> CreateLiveChatRule([FromBody] CreateRuleRequest request)
        {
            // TODO: Get the current user from context/token if available
            var currentUser = "example@domain.com";

            var fallbackEvalOrder =
                (await liveChatSettingsService.GetMaxEvaluationOrderAsync()) + 1;
            var rule = request.ToDomainModel(fallbackEvalOrder);
            await liveChatSettingsService.CreateRuleAsync(rule, currentUser);

            return Ok("Rule created successfully.");
        }

        [Authorize]
        [HttpPatch("updateLiveChatRule")]
        public async Task<IActionResult> UpdateLiveChatRule([FromBody] UpdateRuleRequest request)
        {
            var currentUser = "example@domain.com"; // TODO: Replace with real auth context

            var existingRule = await liveChatSettingsService.GetRuleByNameAsync(request.Name);
            if (existingRule == null)
                return NotFound($"No rule found with name '{request.Name}'.");

            request.ApplyUpdatesTo(existingRule);
            await liveChatSettingsService.UpdateRuleAsync(existingRule, currentUser);

            return Ok("Rule updated successfully.");
        }

        [Authorize]
        [HttpGet("getLiveChatRuleByName")]
        public async Task<IActionResult> GetLiveChatRuleByName(string name)
        {
            var rule = await liveChatSettingsService.GetRuleByNameAsync(name);
            return rule != null
                ? Ok(LiveChatRuleResponse.FromDomainModel(rule))
                : NotFound($"No rule found with name '{name}'.");
        }

        [Authorize]
        [HttpGet("getAllLiveChatRules")]
        public async Task<IActionResult> GetAll()
        {
            var rules = await liveChatSettingsService.GetAllAsync();
            var responses = rules.Select(LiveChatRuleResponse.FromDomainModel);
            return Ok(responses);
        }

        [Authorize]
        [HttpDelete("deleteLiveChatRule")]
        public async Task<IActionResult> DeleteRuleByName(string name)
        {
            var rule = await liveChatSettingsService.GetRuleByNameAsync(name);
            if (rule == null)
                return NotFound($"No rule found with name '{name}'.");

            await liveChatSettingsService.DeleteRuleAsync(rule);
            return Ok("Rule deleted successfully.");
        }
    }
}
