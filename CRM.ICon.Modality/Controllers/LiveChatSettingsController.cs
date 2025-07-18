using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model.LiveChatSettings.Requests;
using CRM.ICon.Modality.Services.LiveChatSettings;
using CRM.ICon.Modality.Services.LiveChatSettings.Responses;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("getAllRules")]
        public async Task<IActionResult> GetAllItems()
        {
            var rules = await liveChatSettingsService.GetAllRulesAsync();
            return Ok(rules);
        }

        [HttpPost("createRule")]
        public async Task<IActionResult> CreateItem([FromBody] CreateRuleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Name is required.");

            try
            {
                await liveChatSettingsService.CreateRuleAsync(request.ToDomainModel(), request.CreatedBy);
                return CreatedAtAction(nameof(GetAllItems), new { name = request.Name }, request);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpGet("isChatEligible")]
        public async Task<IActionResult> MatchRuleAsync([FromQuery] MatchRuleRequest request)
        {
            var match = await liveChatSettingsService.MatchRuleAsync(request);

            if (match == null)
            {
                return NotFound("No matching rule found.");
            }
            else
            {
                var isChatEligible = new IsChatEligibleResponse
                {
                    IsChatEligible = true,
                    IsChatForced = match.IsChatForced
                };
                return Ok(isChatEligible);
            }
        }

        [HttpPut("updateRule")]
        public async Task<IActionResult> UpdateRule([FromBody] UpdateRuleRequest request)
        {
            await liveChatSettingsService.UpdateRuleAsync(request, "system");
            return Ok("Rule updated successfully.");
        }

        [HttpDelete("deleteRule")]
        public async Task<IActionResult> DeleteRule([FromQuery] string name)
        {
            await liveChatSettingsService.DeleteRuleByNameAsync(name);
            return NoContent();
        }

        [HttpGet("getRuleByName")]
        public async Task<IActionResult> GetRuleByName([FromQuery] string name)
        {
            var rule = await liveChatSettingsService.GetRuleByNameAsync(name);
            return rule == null ? NotFound("Rule not found.") : Ok(rule);
        }
    }
}
