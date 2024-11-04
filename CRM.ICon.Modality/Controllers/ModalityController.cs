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
    [Authorize]
    [ApiController]
    [Route("api/v1/modality")]
    public class ModalityController : ControllerBase
    {

        private readonly IVDMService vdmService;
        private readonly IOmnichannelService omnichannelService;
        private readonly ITelemetryService _telemetryService;
        private readonly IOmnichannelEUService omnichannelEUService;
        public ModalityController(IVDMService vdmService, IOmnichannelService omnichannelService, ITelemetryService telemetryService, IOmnichannelEUService omnichannelEUService)
        {
            this.vdmService = vdmService;
            this.omnichannelService = omnichannelService;
            this.omnichannelEUService = omnichannelEUService;
            this._telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
        }

        [HttpPost]
        [Route("getAvailableModalities")]
        public async Task<ActionResult<ModalityResponse>> GetAvailableModalities([FromBody] ModalityRequest modalityRequest, [FromHeader] HeaderDictionary headers)
        {
            headers.TryGetValue(Constants.Target, out var target);// Determines whether its a dfm/dfc request
            var logProperties = ModalityExtensions.GetRequestProperties();
            if (modalityRequest == null)
            {
                this._telemetryService.LogTrace<ModalityController>("Request body for GetAvailableModalities is null", logProperties);
                return BadRequest();
            }

            var locale = modalityRequest.Locale.ToLowerInvariant();
            var source = modalityRequest.Source.ToLowerInvariant();
            var userType = modalityRequest.UserType;

            ModalityResponse modalityResponse = new ModalityResponse();

            ModalityInfo modalityInfoEmail = new ModalityInfo();
            modalityInfoEmail.Modality = 1;
            modalityInfoEmail.WaitTime = "0";
            modalityInfoEmail.IsAgentAvailable = true;
            modalityInfoEmail.InHoops = true;

            ModalityInfo modalityInfoPhone = new ModalityInfo();
            modalityInfoPhone.Modality = 2;
            modalityInfoPhone.WaitTime = "0";
            modalityInfoPhone.IsAgentAvailable = true;
            modalityInfoPhone.InHoops = true;

            (modalityResponse.Modalities ??= new List<ModalityInfo>()).Add(modalityInfoEmail);
            modalityResponse.Modalities.Add(modalityInfoPhone);

            CustomerAttribute customerAttributes = modalityRequest.CustomerAttributes;
            bool isACE = customerAttributes?.IsACE ?? false;
            if (isACE)
            {

                this._telemetryService.LogTrace<ModalityController>("ACE customer is true", logProperties);
                return Ok(modalityResponse);
            }

            SupportTicketAttribute supportTicketAttribute = modalityRequest.SupportTicketAttributes;

            string languageCode = "en";
            try 
            { 
                CultureInfo cultureInfo = new CultureInfo(locale);
                languageCode = cultureInfo.TwoLetterISOLanguageName;
                if (string.IsNullOrEmpty(languageCode))
                {
                    languageCode = "en";
                }
                else
                {
                    languageCode = languageCode.ToLowerInvariant();
                }
            } 
            catch (Exception exception)
            {
                this._telemetryService.LogException<ModalityController>(exception, logProperties, "Get language code from locale Failure");
                // fallback locale is always en-us
                locale = "en-us";
            }

            var requestId = modalityRequest.RequestId;

            var vdmResponse = await vdmService.GetVDMSkill(new VDMRequest { Text = supportTicketAttribute.Description, Boundary = "public", SapId = supportTicketAttribute.SapId, PredictionPurposes = "crmee_ml_skill_model" }, requestId);

            if (vdmResponse == null || vdmResponse.skillValue == null)
            {
                this._telemetryService.LogTrace<ModalityController>("VDM response is null", logProperties);
                // If VDM response is null, then chat modality and skills are not returned. 
                return Ok(modalityResponse);
            }

            if (string.IsNullOrEmpty(userType))
            {
                userType = "commercial"; //default value is commercial
            }
            userType = userType.ToLowerInvariant();

            var omnichannelService = userType.Equals("eu") ? this.omnichannelEUService : this.omnichannelService;

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

            Skills skills = new Skills();

            skills.skills = skillObjects;

            CustomContext customContext = new CustomContext();
            customContext.EnrichRoutingContext = new EnrichRoutingContext { value = skills, isDisplayable = true };
            customContext.Entitlement = new ServiceLevel { value = supportTicketAttribute.EntitlementInformation?.ServiceLevel, isDisplayable = true };
            customContext.Skill = new Skill { value = vdmResponse.skillName, isDisplayable = true };
            customContext.ACE = new ACE { value = isACE.ToString(), isDisplayable = true };
            OmnichannelRequest omnichannelRequest = new OmnichannelRequest();

            omnichannelRequest.CustomContext = customContext;

            var omnichannelResponse = await omnichannelService.GetAgentAvailability(omnichannelRequest, source, userType, requestId);

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

            WidgetDetails widgetDetails = omnichannelService.GetWidgetDetails(languageCode, source, userType);

            widgetDetails.Theme = modalityRequest.Theme;

            ModalityInfo modalityInfo = new ModalityInfo();
            modalityInfo.Modality = 4;
            modalityInfo.WaitTime = omnichannelResponse.AverageWaitTime;
            modalityInfo.IsAgentAvailable = omnichannelResponse.IsAgentAvailable;
            modalityInfo.InHoops = omnichannelResponse.IsQueueAvailable;

            modalityInfo.WidgetDetails = widgetDetails;

            skills.queueid = omnichannelResponse.QueueId;
            modalityInfo.CustomContext = customContext;

            (modalityResponse.Modalities ??= new List<ModalityInfo>()).Add(modalityInfo);

            return Ok(modalityResponse);
        }

        [HttpPost]
        [Route("getWidgetDetails")]
        public async Task<ActionResult<WidgetDetails>> GetWidgetDetails([FromBody] WidgetRequest widgetRequest)
        {
            if (widgetRequest == null)
            {
                return BadRequest();
            }

            var source = widgetRequest.Source.ToLowerInvariant();

            string locale = widgetRequest.Locale.ToLowerInvariant();
            string languageCode = "en";
            try
            {
                CultureInfo cultureInfo = new CultureInfo(locale);
                languageCode = cultureInfo.TwoLetterISOLanguageName;
                if (string.IsNullOrEmpty(languageCode))
                {
                    languageCode = "en";
                }
                else
                {
                    languageCode = languageCode.ToLowerInvariant();
                }
            }
            catch (Exception exception)
            {
                // fallback locale is always en-us
                locale = "en-us";
            }

            var userType = widgetRequest.UserType;

            if (string.IsNullOrEmpty(userType))
            {
                userType = "commercial"; //default value is commercial
            }

            userType = userType.ToLowerInvariant();

            var omnichannelService = userType.Equals("eu") ? this.omnichannelEUService : this.omnichannelService;

            WidgetDetails widgetDetails = omnichannelService.GetWidgetDetails(languageCode, source, userType);
            if (widgetDetails != null)
            {
                widgetDetails.Theme = widgetRequest.Theme;
            }
            return widgetDetails;
        }
    }
}