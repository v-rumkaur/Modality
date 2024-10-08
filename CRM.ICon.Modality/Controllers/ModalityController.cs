using CRM.ICon.Modality.Model;
using CRM.ICon.Modality.Services.VDM;
using Microsoft.AspNetCore.Mvc;
using System.Data.Common;

namespace CRM.ICon.Modality.Controllers
{
    [ApiController]
    [Route("api/v1/modality")]
    public class ModalityController : ControllerBase
    {

        private readonly IVDMService vdmService;
        public ModalityController(IVDMService vdmService)
        {
            this.vdmService = vdmService;
        }

        [HttpPost]
        [Route("getAvailableModalities")]
        public async Task<ActionResult<ModalityResponse>> GetAvailableModalities([FromBody] ModalityRequest modalityRequest)
        {
            if (modalityRequest == null)
            {
                return BadRequest();
            }
            ModalityResponse modalityResponse = new ModalityResponse();
            ModalityInfo modalityInfo = new ModalityInfo();
            modalityInfo.Modality = 4;
            modalityInfo.WaitTime = 0;
            modalityInfo.IsAgentAvailable = true;
            modalityInfo.InHoops = true;
            
            WidgetDetails widgetDetails = new WidgetDetails();
            widgetDetails.OrgId = "";
            widgetDetails.OrgUrl = "";
            widgetDetails.WidgetId = "";
            widgetDetails.Theme = "Dark";

            modalityInfo.WidgetDetails = widgetDetails;

            CustomContext customContext = new CustomContext();
            customContext.EnrichRoutingContext = new EnrichRoutingContext { value = "skills\": [{\"characteristicid\":\"27b98065-5c1f-ee11-8128-000d3af89ec9\"}]", isDisplayable = true };
            customContext.Entitlement = new Entitlement { value = "BroadCommercial", isDisplayable = true };
            customContext.Skill = new Skill { value = "vdm skill", isDisplayable = true };

            modalityInfo.CustomContext = customContext;

            (modalityResponse.Modalities ??= new List<ModalityInfo>()).Add(modalityInfo);


            SupportTicketAttribute supportTicketAttribute = modalityRequest.SupportTicketAttributes;

            VDMResponse vdmResponse = await vdmService.GetVDMSkill(new VDMRequest { Text = supportTicketAttribute.Description, Boundary = "public", SapId = supportTicketAttribute.SapId, PredictionPurposes = "crmee_ml_skill_model" });
            
            SkillInfo skill = new SkillInfo();
            skill.SkillValue = vdmResponse.skillValue;
            skill.SkillLabel = vdmResponse.skillName;
            skill.SkillType = "Skill";

            (modalityResponse.Skills ??= new List<SkillInfo>()).Add(skill);

            return Ok(modalityResponse);
        }
    }
}