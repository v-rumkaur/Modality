using CRM.ICon.Modality.Model;
using Microsoft.AspNetCore.Mvc;
using System.Data.Common;

namespace CRM.ICon.Modality.Controllers
{
    [ApiController]
    [Route("api/v1/modality")]
    public class ModalityController : ControllerBase
    {
        [HttpPost]
        [Route("getAvailableModalities")]
        public async Task<ActionResult> GetAvailableModalities([FromBody] ModalityRequest modalityRequest)
        {
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

            SkillInfo skill = new SkillInfo();
            skill.SkillValue = "27b98065-5c1f-ee11-8128-000d3af89ec9";
            skill.SkillLabel = "VDM Skill";
            skill.SkillType = "Skill";
            (modalityResponse.Skills ??= new List<SkillInfo>()).Add(skill);

            return Ok(modalityResponse);
        }
    }
}
