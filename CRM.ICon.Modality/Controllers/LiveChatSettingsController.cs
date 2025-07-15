using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model.LiveChatSettings.Requests;
using CRM.ICon.Modality.Services.LiveChatSettings;
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
        public async Task<IActionResult> CreateItem([FromBody] CreateRuleRequest rule)
        {
            if (string.IsNullOrWhiteSpace(rule.Name))
                return BadRequest("Name is required.");

            try
            {
                await liveChatSettingsService.CreateRuleAsync(rule.ToDomainModel(), rule.CreatedBy);
                return CreatedAtAction(nameof(GetAllItems), new { id = rule.Name }, rule);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpGet("isChatEligible")]
        public async Task<IActionResult> MatchRuleAsync([FromQuery] MatchRuleRequest userContext)
        {
            var match = await liveChatSettingsService.MatchRuleAsync(userContext);

            if (match == null)
                return NotFound("No matching rule found.");

            return Ok(match);
        }
    }
}