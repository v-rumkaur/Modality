using CRM.ICon.Modality.Helpers;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model;
using CRM.ICon.Modality.Services.Omnichannel;
using CRM.ICon.Modality.Services.VDM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.Common;
using System.Globalization;

namespace CRM.ICon.Modality.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/v1/modality")]
    public class ModalityController : ControllerBase
    {

        private readonly IVDMService vdmService;
        private readonly IOmnichannelService omnichannelService;
        private readonly ITelemetryService _telemetryService;
        public ModalityController(IVDMService vdmService, IOmnichannelService omnichannelService, ITelemetryService telemetryService)
        {
            this.vdmService = vdmService;
            this.omnichannelService = omnichannelService;
            this._telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
        }

        [HttpPost]
        [Route("getAvailableModalities")]
        public async Task<ActionResult<ModalityResponse>> GetAvailableModalities([FromBody] ModalityRequest modalityRequest)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            if (modalityRequest == null)
            {
                this._telemetryService.LogTrace<ModalityController>("Request body for GetAvailableModalities is null", logProperties);
                return BadRequest();
            }

            ModalityResponse modalityResponse = new ModalityResponse();

            ModalityInfo modalityInfoEmail = new ModalityInfo();
            modalityInfoEmail.Modality = 1;
            modalityInfoEmail.WaitTime = 0;
            modalityInfoEmail.IsAgentAvailable = true;
            modalityInfoEmail.InHoops = true;

            ModalityInfo modalityInfoPhone = new ModalityInfo();
            modalityInfoPhone.Modality = 2;
            modalityInfoPhone.WaitTime = 0;
            modalityInfoPhone.IsAgentAvailable = true;
            modalityInfoPhone.InHoops = true;

            (modalityResponse.Modalities ??= new List<ModalityInfo>()).Add(modalityInfoEmail);
            modalityResponse.Modalities.Add(modalityInfoPhone);

            SupportTicketAttribute supportTicketAttribute = modalityRequest.SupportTicketAttributes;

            string languageCode = "en";
            string locale = modalityRequest.Locale;
            try 
            { 
                CultureInfo cultureInfo = new CultureInfo(locale);
                languageCode = cultureInfo.TwoLetterISOLanguageName;
            } 
            catch (Exception exception)
            {
                this._telemetryService.LogException<ModalityController>(exception, logProperties, "Get language code from locale Failure");
                // fallback locale is always en-us
                locale = "en-us";
            }

            VDMResponse vdmResponse = new VDMResponse();
            vdmResponse.skillValue = "8643644c-ed5d-ee11-8143-000d3af8897c";
            vdmResponse.skillName = "Cust Eng: CIJ Administration";

            vdmResponse = await vdmService.GetVDMSkill(new VDMRequest { Text = supportTicketAttribute.Description, Boundary = "public", SapId = supportTicketAttribute.SapId, PredictionPurposes = "crmee_ml_skill_model" });

            if (vdmResponse == null || vdmResponse.skillValue == null)
            {
                this._telemetryService.LogTrace<ModalityController>("VDM response is null", logProperties);
                // If VDM response is null, then chat modality and skills are not returned. 
                return Ok(modalityResponse);
            }

            //language characteristic 
            string languageCharacteristicId = omnichannelService.GetSkillCharacteristicId(languageCode);
            SkillObject languageSkillObject = new SkillObject();
            languageSkillObject.characteristicId = languageCharacteristicId ?? omnichannelService.GetSkillCharacteristicId("en"); ;

            //region characteristic 
            string region = LocaleRegionalMapping.LocaleRegionalData[locale];
            string regionCharacteristicId = omnichannelService.GetSkillCharacteristicId(region ?? "Americas");
            SkillObject regionSkillObject = new SkillObject();
            regionSkillObject.characteristicId = regionCharacteristicId ?? omnichannelService.GetSkillCharacteristicId("Americas");

            //vdm characteristic
            SkillObject vdmSkillObject = new SkillObject();
            vdmSkillObject.characteristicId = vdmResponse.skillValue;

            List<SkillObject> skillObjects = new List<SkillObject>();
            skillObjects.Add(vdmSkillObject);
            skillObjects.Add(languageSkillObject);
            skillObjects.Add(regionSkillObject);

            CustomContext customContext = new CustomContext();
            customContext.EnrichRoutingContext = new EnrichRoutingContext { value = skillObjects, isDisplayable = true };
            customContext.Entitlement = new ServiceLevel { value = supportTicketAttribute.EntitlementInformation.ServiceLevel, isDisplayable = true };
            customContext.Skill = new Skill { value = vdmResponse.skillName, isDisplayable = true };

            OmnichannelResponse omnichannelResponse = new OmnichannelResponse();

            OmnichannelRequest omnichannelRequest = new OmnichannelRequest();

            omnichannelRequest.customContext = customContext;
            //OmnichannelResponse omnichannelResponse = await omnichannelService.GetAgentAvailability(omnichannelRequest);

            if (omnichannelResponse == null)
            {
                this._telemetryService.LogTrace<ModalityController>("Omnichannel response is null", logProperties);
                return Ok(modalityResponse);
            }

            SkillInfo vdmSkill = new SkillInfo();
            vdmSkill.SkillValue = vdmResponse.skillValue;
            vdmSkill.SkillLabel = vdmResponse.skillName;
            vdmSkill.SkillType = "Skill";

            SkillInfo languageSkill = new SkillInfo();
            languageSkill.SkillValue = languageSkillObject.characteristicId;
            languageSkill.SkillLabel = languageCode;
            languageSkill.SkillType = "Language";

            SkillInfo regionSkill = new SkillInfo();
            regionSkill.SkillValue = regionSkillObject.characteristicId;
            regionSkill.SkillLabel = region ?? "Americas";
            regionSkill.SkillType = "Region";

            (modalityResponse.Skills ??= new List<SkillInfo>()).Add(languageSkill);
            modalityResponse.Skills.Add(vdmSkill);
            modalityResponse.Skills.Add(regionSkill);

            WidgetDetails widgetDetails = omnichannelService.GetWidgetDetails(languageCode);

            widgetDetails.Theme = modalityRequest.Theme;

            ModalityInfo modalityInfo = new ModalityInfo();
            modalityInfo.Modality = 4;
            modalityInfo.WaitTime = 0;
            modalityInfo.IsAgentAvailable = true;
            modalityInfo.InHoops = true;

            modalityInfo.WidgetDetails = widgetDetails;

            modalityInfo.CustomContext = customContext;

            (modalityResponse.Modalities ??= new List<ModalityInfo>()).Add(modalityInfo);

            return Ok(modalityResponse);
        }
    }
}