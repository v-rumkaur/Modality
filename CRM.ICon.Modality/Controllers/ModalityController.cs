using Azure.Core;
using CRM.ICon.Modality.Helpers;
using CRM.ICon.Modality.Helpers.Cosmos;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model;
using CRM.ICon.Modality.Services.Omnichannel;
using CRM.ICon.Modality.Services.VDM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTelemetry.Resources;
using System.Data.Common;
using System.Globalization;

namespace CRM.ICon.Modality.Controllers
{
    [ApiController]
    [Route("api/v1/modality")]
    public class ModalityController : ControllerBase
    {

        private readonly IVDMService vdmService;
        private readonly IOmnichannelService omnichannelService;
        private readonly ITelemetryService _telemetryService;
        private readonly IOmnichannelEUService omnichannelEUService;
        private readonly ICosmosDbClient cosmosDbClient;
        public ModalityController(IVDMService vdmService, IOmnichannelService omnichannelService, ITelemetryService telemetryService, IOmnichannelEUService omnichannelEUService, ICosmosDbClient cosmosDbClient)
        {
            this.vdmService = vdmService;
            this.omnichannelService = omnichannelService;
            this.omnichannelEUService = omnichannelEUService;
            this._telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            this.cosmosDbClient = cosmosDbClient;
        }

        [Authorize]
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
            var requestId = modalityRequest.RequestId;

            logProperties.AddObjectAsString("Source", source);
            logProperties.AddObjectAsString("RequestId", modalityRequest.RequestId);

            this._telemetryService.LogTrace<ModalityController>("GetAvailableModalities", logProperties);

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

            string country = modalityRequest.Country;
            if (string.IsNullOrEmpty(country) || !CountryToRegionMapping.CountryToRegionData.ContainsKey(country))
            {
                country = "US";
            }

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

            var vdmResponse = await vdmService.GetVDMSkill(new VDMRequest { Text = supportTicketAttribute.Description, Boundary = "public", SapId = supportTicketAttribute.SapId, PredictionPurposes = "crmee_ml_skill_model" }, requestId);

            if (vdmResponse == null || vdmResponse.skillValue == null)
            {
                //modalityResponse = GetModalityResponse(modalityResponse, languageCode, userType, supportTicketAttribute, country, source);
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
            languageSkillObject.characteristicid = languageCharacteristicId ?? omnichannelService.GetSkillCharacteristicId("en"); ;
            //languageSkillObject.ratingvalueid = "144b5d8f-8014-ed11-b83d-000d3a3bb008";
            //region characteristic 
            string region = CountryToRegionMapping.CountryToRegionData[country];
            string regionCharacteristicId = omnichannelService.GetSkillCharacteristicId(region ?? "Americas");
            SkillObject regionSkillObject = new SkillObject();
            regionSkillObject.characteristicid = regionCharacteristicId ?? omnichannelService.GetSkillCharacteristicId("Americas");
            //regionSkillObject.ratingvalueid = "144b5d8f-8014-ed11-b83d-000d3a3bb008";
            //vdm characteristic
            SkillObject vdmSkillObject = new SkillObject();
            vdmSkillObject.characteristicid = vdmResponse.skillValue;
            //vdmSkillObject.ratingvalueid = "144b5d8f-8014-ed11-b83d-000d3a3bb008";

            List<SkillObject> skillObjects = new List<SkillObject>();
            skillObjects.Add(vdmSkillObject);
            skillObjects.Add(languageSkillObject);
            skillObjects.Add(regionSkillObject);

            Skills skills = new Skills();

            skills.skills = skillObjects;

            CustomContext customContext = new CustomContext();
            customContext.EnrichRoutingContext = new EnrichRoutingContext { value = JsonConvert.SerializeObject(skills) , isDisplayable = true };
            customContext.ServiceLevel = new ServiceLevel { value = supportTicketAttribute.EntitlementInformation?.ServiceLevel, isDisplayable = true };
            customContext.Skill = new Skill { value = vdmResponse.skillName, isDisplayable = true };
            customContext.ACE = new ACE { value = isACE.ToString(), isDisplayable = true };
            OmnichannelRequest omnichannelRequest = new OmnichannelRequest();

            omnichannelRequest.CustomContext = customContext;

            var omnichannelResponse = await omnichannelService.GetAgentAvailability(omnichannelRequest, source, userType, requestId);

            if (omnichannelResponse == null)
            {
                //modalityResponse = GetModalityResponse(modalityResponse, languageCode, userType, supportTicketAttribute, country, source);
                this._telemetryService.LogTrace<ModalityController>("Omnichannel response is null", logProperties);
                return Ok(modalityResponse);
            }

            SkillInfo vdmSkill = new SkillInfo();
            vdmSkill.SkillValue = vdmResponse.skillValue;
            vdmSkill.SkillLabel = vdmResponse.skillName;
            vdmSkill.SkillType = "Skill";

            SkillInfo languageSkill = new SkillInfo();
            languageSkill.SkillValue = languageSkillObject.characteristicid;
            languageSkill.SkillLabel = languageCode;
            languageSkill.SkillType = "Language";

            SkillInfo regionSkill = new SkillInfo();
            regionSkill.SkillValue = regionSkillObject.characteristicid;
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
            //Adding queue id to enrich routing context
            //customContext.EnrichRoutingContext = new EnrichRoutingContext { value = JsonConvert.SerializeObject(skills), isDisplayable = true };
            modalityInfo.CustomContext = customContext;

            (modalityResponse.Modalities ??= new List<ModalityInfo>()).Add(modalityInfo);

            return Ok(modalityResponse);
        }

        [Authorize]
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

        [Authorize]
        [HttpPost]
        [Route("createThemeSubjectMapping")]
        public async Task<ActionResult<ThemeSubjectMappingResponse>> CreateThemeSubjectMapping([FromBody] ThemeMappingRequest themeMappingRequest)
        {

            ThemeSubjectMappingResponse response = null;

            foreach (var mapping in themeMappingRequest.ThemeRuleData)
            {
                var array = mapping.Split('_');
                ThemeSubjectMappingResponse themeSubjectMappingResponse = new ThemeSubjectMappingResponse();

                themeSubjectMappingResponse.id = mapping.ToLowerInvariant();
                themeSubjectMappingResponse.theme = array[0];
                themeSubjectMappingResponse.themeL1 = array[1];
                themeSubjectMappingResponse.themeL2 = array[2];
                themeSubjectMappingResponse.themeL3 = array[3];
                themeSubjectMappingResponse.entitlement = array[4];
                themeSubjectMappingResponse.languageName = array[5];
                themeSubjectMappingResponse.countryName = array[6];
                themeSubjectMappingResponse.subjectId = themeMappingRequest.subjectId;
                themeSubjectMappingResponse.isC2C = themeMappingRequest.isC2C;
                themeSubjectMappingResponse.isChat = themeMappingRequest.isChat;

                response = this.cosmosDbClient.UpsertItemAsync<ThemeSubjectMappingResponse>(themeSubjectMappingResponse).Result;
            }

            if (response != null)
            {
                var themesubjectmapping = new JObject
                {
                    { "id", response.id },
                    { "isC2C", response.isC2C },
                    { "isChat", response.isChat },
                    { "SubjectId", response.subjectId },
                    { "Theme", response.theme },
                    { "ThemeL1", response.themeL1 },
                    { "ThemeL2", response.themeL2 },
                    { "ThemeL3", response.themeL3 },
                    { "LanguageName", response.languageName },
                    { "CountryName", response.countryName },
                    { "Entitlement", response.entitlement },
                };

                return Ok(themesubjectmapping);
            }

            return null;
        }

        private ModalityResponse GetModalityResponse(ModalityResponse modalityResponse, string languageCode, string userType, SupportTicketAttribute supportTicketAttribute, string country, string source)
        {
            if (string.IsNullOrEmpty(userType))
            {
                userType = "commercial"; //default value is commercial
            }
            userType = userType.ToLowerInvariant();

            var omnichannelService = userType.Equals("eu") ? this.omnichannelEUService : this.omnichannelService;

            //language characteristic 
            string languageCharacteristicId = omnichannelService.GetSkillCharacteristicId(languageCode);
            SkillObject languageSkillObject = new SkillObject();
            languageSkillObject.characteristicid = languageCharacteristicId ?? omnichannelService.GetSkillCharacteristicId("en"); ;
            //languageSkillObject.ratingvalueid = "144b5d8f-8014-ed11-b83d-000d3a3bb008";
            //region characteristic 
            string region = CountryToRegionMapping.CountryToRegionData[country];
            string regionCharacteristicId = omnichannelService.GetSkillCharacteristicId(region ?? "Americas");
            SkillObject regionSkillObject = new SkillObject();
            regionSkillObject.characteristicid = regionCharacteristicId ?? omnichannelService.GetSkillCharacteristicId("Americas");
            //regionSkillObject.ratingvalueid = "144b5d8f-8014-ed11-b83d-000d3a3bb008";
            //vdm characteristic
            SkillObject vdmSkillObject = new SkillObject();
            vdmSkillObject.characteristicid = "5f0f0540-ed5d-ee11-8143-000d3af8897c";
            //vdmSkillObject.ratingvalueid = "144b5d8f-8014-ed11-b83d-000d3a3bb008";

            List<SkillObject> skillObjects = new List<SkillObject>();
            skillObjects.Add(vdmSkillObject);
            skillObjects.Add(languageSkillObject);
            skillObjects.Add(regionSkillObject);

            Skills skills = new Skills();

            skills.skills = skillObjects;

            CustomContext customContext = new CustomContext();
            customContext.EnrichRoutingContext = new EnrichRoutingContext { value = JsonConvert.SerializeObject(skills), isDisplayable = true };
            customContext.ServiceLevel = new ServiceLevel { value = supportTicketAttribute.EntitlementInformation?.ServiceLevel, isDisplayable = true };
            customContext.Skill = new Skill { value = "Cust Eng: CIJ Provisioning", isDisplayable = true };
            customContext.ACE = new ACE { value = "False", isDisplayable = true };
            OmnichannelRequest omnichannelRequest = new OmnichannelRequest();

            omnichannelRequest.CustomContext = customContext;

            SkillInfo vdmSkill = new SkillInfo();
            vdmSkill.SkillValue = "5f0f0540-ed5d-ee11-8143-000d3af8897c";
            vdmSkill.SkillLabel = "Cust Eng: CIJ Provisioning";
            vdmSkill.SkillType = "Skill";

            SkillInfo languageSkill = new SkillInfo();
            languageSkill.SkillValue = languageSkillObject.characteristicid;
            languageSkill.SkillLabel = languageCode;
            languageSkill.SkillType = "Language";

            SkillInfo regionSkill = new SkillInfo();
            regionSkill.SkillValue = regionSkillObject.characteristicid;
            regionSkill.SkillLabel = region ?? "Americas";
            regionSkill.SkillType = "Region";

            (modalityResponse.Skills ??= new List<SkillInfo>()).Add(languageSkill);
            modalityResponse.Skills.Add(vdmSkill);
            modalityResponse.Skills.Add(regionSkill);

            WidgetDetails widgetDetails = omnichannelService.GetWidgetDetails(languageCode, source, userType);

            widgetDetails.Theme = "Light";

            ModalityInfo modalityInfo = new ModalityInfo();
            modalityInfo.Modality = 4;
            modalityInfo.WaitTime = "0";
            modalityInfo.IsAgentAvailable = true;
            modalityInfo.InHoops = true;

            modalityInfo.WidgetDetails = widgetDetails;

            //Adding queue id to enrich routing context
            //customContext.EnrichRoutingContext = new EnrichRoutingContext { value = JsonConvert.SerializeObject(skills), isDisplayable = true };
            modalityInfo.CustomContext = customContext;

            (modalityResponse.Modalities ??= new List<ModalityInfo>()).Add(modalityInfo);
            return modalityResponse;
        }
    }
}