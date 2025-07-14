using Azure.Core;
using CRM.ICon.Modality.Helpers;
using CRM.ICon.Modality.Helpers.Cosmos;
using CRM.ICon.Modality.Helpers.ModalityCosmos;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model;
using CRM.ICon.Modality.Services.Omnichannel;
using CRM.ICon.Modality.Services.VDM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTelemetry.Resources;
using System;
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

        private readonly bool IsMCS = false;
        private readonly string Ring = "Ring4";
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
            logProperties["RequestBody"] = JsonConvert.SerializeObject(modalityRequest);
            _telemetryService.LogTrace<ModalityController>("Received GetAvailableModalities request", logProperties);

            if (modalityRequest == null)
            {
                logProperties["Reason"] = "Request body is null";
                _telemetryService.LogTrace<ModalityController>("Validation failed", logProperties);
                return BadRequest("Request body for GetAvailableModalities is null");
            }

            var locale = modalityRequest.Locale?.ToLowerInvariant();
            var source = modalityRequest.Source.ToLowerInvariant();
            var supportAreaName = modalityRequest.SupportTicketAttributes?.SupportAreaName?.ToLowerInvariant();
            var userType = modalityRequest.UserType?.ToLowerInvariant();
            var requestId = modalityRequest.RequestId;
            var userLCID = modalityRequest.UserLcid.GetValueOrDefault();

            logProperties.AddObjectAsString("Source", source);
            logProperties.AddObjectAsString("RequestId", modalityRequest.RequestId);

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

            ICollection<LanguageSkillData> languageSkills = UserLcidToSkillData(userLCID);

            LanguageSkillData languageSkillData = languageSkills.FirstOrDefault();
            string language = languageSkillData?.SkillName.ToString();
            List<BusinessHour> businessHourDatas;
            BusinessHourConstants.BusinessHourData.TryGetValue(language, out businessHourDatas);

            if (!IsBusinessHour(businessHourDatas))
            {
                var businessHourData = businessHourDatas.LastOrDefault();

                if (ValidateBusinessHourData(businessHourData))
                {
                    var businessHoursDetails = new BusinessHour
                    {
                        StartDayOfWeek = businessHourData.StartDayOfWeek,
                        EndDayOfWeek = businessHourData.EndDayOfWeek,
                        StartHour = businessHourData.StartHour,
                        EndHour = businessHourData.EndHour,
                        TimeZoneName = businessHourData.TimeZoneName,
                        UtcOffset = businessHourData.UtcOffset,
                        StartMin = businessHourData.StartMin,
                        EndMin = businessHourData.EndMin
                    };

                    List<BusinessHour> list = new List<BusinessHour>();
                    list.Add(businessHoursDetails);

                    HoopsInfo businessHoursInfo = new HoopsInfo();
                    businessHoursInfo.BusinessHours = list;

                    modalityInfoPhone.InHoops = false;
                    modalityInfoPhone.HoopsInfo = businessHoursInfo;
                    modalityInfoPhone.IsAgentAvailable = false;

                    modalityInfoEmail.InHoops = false;
                    modalityInfoEmail.HoopsInfo = businessHoursInfo;
                    modalityInfoEmail.IsAgentAvailable = false;

                }
            }

            (modalityResponse.Modalities ??= new List<ModalityInfo>()).Add(modalityInfoEmail);
            modalityResponse.Modalities.Add(modalityInfoPhone);

            if (string.IsNullOrEmpty(userType))
            {
                _telemetryService.LogTrace<ModalityController>("Empty User Type defaulting to Commercial", logProperties);
                userType = "commercial"; //default value is commercial
            }

            var omnichannelService = userType.Equals("eu") ? this.omnichannelEUService : this.omnichannelService;

            // Queue based Routing and Assignment
            if (source != null && !source.Contains("ppac"))
            {

                bool isConciergeChat = supportAreaName != null && string.Equals(supportAreaName, "concierge", StringComparison.CurrentCultureIgnoreCase) && ValidateConciergeChat(modalityRequest, language);

                bool isSCIMChat = ValidateSCIMChat(modalityRequest, language);

                if (isSCIMChat)
                {
                    source = source + "SCIM";
                }
                else if (isConciergeChat)
                {
                    source = source + "Concierge";
                }
                else if (string.Equals(source, "SupportCentral", StringComparison.CurrentCultureIgnoreCase) ||
                    string.Equals(source, "SupportCentralSearch", StringComparison.CurrentCultureIgnoreCase))
                {
                    return Ok(modalityResponse);
                }

                CustomContext chatCustomContext = new CustomContext();
                chatCustomContext.LCID = new LCID { value = userLCID, isDisplayable = true };
                chatCustomContext.Source = new Source { value = source, isDisplayable = true };

                OmnichannelRequest chatOmnichannelRequest = new OmnichannelRequest();
                chatOmnichannelRequest.CustomContext = chatCustomContext;

                _telemetryService.LogTrace<ModalityController>("Calling OmnichannelService.GetAgentAvailability", logProperties);

                var queueAvailability = await omnichannelService.GetAgentAvailability(chatOmnichannelRequest, source, userType, requestId);

                if (queueAvailability != null)
                {
                    ModalityInfo modalityInfoChat = new ModalityInfo();

                    int averagewaittime = 0;
                    int.TryParse(queueAvailability.AverageWaitTime, out averagewaittime);

                    if (isConciergeChat && averagewaittime > 15)
                    {
                        return Ok(modalityResponse);
                    }

                    if (isSCIMChat && averagewaittime > 5)
                    {
                        return Ok(modalityResponse);
                    }

                    if (!queueAvailability.IsQueueAvailable)
                    {
                        var businessHourDetails = GetEmeraldBusinessHours();

                        if (isSCIMChat)
                        {
                            businessHourDetails = GetSCIMBusinessHours();
                        }

                        else if (isConciergeChat)
                        {
                            businessHourDetails = GetConciergeBusinessHours();
                        }

                        List<BusinessHour> businessHourList = new List<BusinessHour>();
                        businessHourList.Add(businessHourDetails);
                        HoopsInfo hoopsInfo = new HoopsInfo();
                        hoopsInfo.BusinessHours = businessHourList;
                        modalityInfoChat.HoopsInfo = hoopsInfo;
                    }

                    modalityInfoChat.Modality = 4;
                    modalityInfoChat.WaitTime = queueAvailability.AverageWaitTime;
                    modalityInfoChat.IsAgentAvailable = queueAvailability.IsAgentAvailable;
                    modalityInfoChat.InHoops = queueAvailability.IsQueueAvailable;

                    modalityResponse.Modalities.Add(modalityInfoChat);
                }

                return Ok(modalityResponse);
            }

            var languageCode = GetLanguage(locale, userLCID);

            // Skill based Routing and Assignment
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

            VDMResponse vdmResponse = null;

            if (!String.IsNullOrEmpty(supportTicketAttribute?.SkillName) && !String.IsNullOrEmpty(supportTicketAttribute?.SkillValue))
            {
                vdmResponse = new VDMResponse();
                vdmResponse.SkillValue = supportTicketAttribute.SkillValue;
                vdmResponse.SkillName = supportTicketAttribute.SkillName;
            }
            else
            {
                this._telemetryService.LogTrace<ModalityController>("Partner did not provide skill", logProperties);
                vdmResponse = await vdmService.GetVDMSkill(new VDMRequest { Text = supportTicketAttribute.Description, Boundary = "public", SapId = supportTicketAttribute.SapId, PredictionPurposes = "crmee_ml_skill_model" }, requestId);
            }

            if (vdmResponse == null || vdmResponse.SkillValue == null)
            {
                this._telemetryService.LogTrace<ModalityController>("VDM response is null", logProperties);
                // If VDM response is null, then chat modality and skills are not returned. 
                return Ok(modalityResponse);
            }

            //language characteristic 
            string languageCharacteristicId = omnichannelService.GetSkillCharacteristicId(languageCode);
            SkillObject languageSkillObject = new SkillObject();
            languageSkillObject.characteristicid = languageCharacteristicId ?? omnichannelService.GetSkillCharacteristicId("en"); ;

            //region characteristic 
            string region = CountryToRegionMapping.CountryToRegionData[country];
            string regionCharacteristicId = omnichannelService.GetSkillCharacteristicId(region ?? "Americas");
            SkillObject regionSkillObject = new SkillObject();
            regionSkillObject.characteristicid = regionCharacteristicId ?? omnichannelService.GetSkillCharacteristicId("Americas");

            //vdm characteristic
            SkillObject vdmSkillObject = new SkillObject();
            vdmSkillObject.characteristicid = vdmResponse.SkillValue;

            List<SkillObject> skillObjects = new List<SkillObject>();
            skillObjects.Add(vdmSkillObject);
            skillObjects.Add(languageSkillObject);
            skillObjects.Add(regionSkillObject);

            Skills skills = new Skills();

            skills.skills = skillObjects;

            CustomContext customContext = new CustomContext();
            customContext.EnrichRoutingContext = new EnrichRoutingContext { value = JsonConvert.SerializeObject(skills), isDisplayable = true };
            customContext.ServiceLevel = new ServiceLevel { value = supportTicketAttribute.EntitlementInformation?.ServiceLevel, isDisplayable = true };
            customContext.Skill = new Skill { value = vdmResponse.SkillName, isDisplayable = true };
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
            vdmSkill.SkillValue = vdmResponse.SkillValue;
            vdmSkill.SkillLabel = vdmResponse.SkillName;
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

            WidgetDetails widgetDetails = await omnichannelService.GetWidgetDetails(languageCode, source, userType, IsMCS, Ring);

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

            logProperties["ModalityResponse"] = JsonConvert.SerializeObject(modalityResponse);
            _telemetryService.LogTrace<ModalityController>("Returning modality response", logProperties);
            return Ok(modalityResponse);
        }

        [Authorize]
        [HttpPost]
        [Route("getWidgetDetails")]
        public async Task<ActionResult<WidgetDetails>> GetWidgetDetails([FromBody] WidgetRequest widgetRequest)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["WidgetRequest"] = JsonConvert.SerializeObject(widgetRequest);

            _telemetryService.LogTrace<ModalityController>("Received GetWidgetDetails request", logProperties);

            if (widgetRequest == null)
            {
                return BadRequest("Request body is missing.");
            }

            if (string.IsNullOrWhiteSpace(widgetRequest.Source))
            {
                return BadRequest("Source is required in the request body.");
            }

            var source = widgetRequest.Source.ToLowerInvariant();
            string locale = widgetRequest.Locale.ToLowerInvariant();

            string languageCode;
            try
            {
                var cultureInfo = new CultureInfo(locale);
                languageCode = string.IsNullOrWhiteSpace(cultureInfo.TwoLetterISOLanguageName)
                    ? "en"
                    : cultureInfo.TwoLetterISOLanguageName.ToLowerInvariant();
            }
            catch (Exception ex)
            {
                logProperties["LocaleParseError"] = ex.ToString();
                _telemetryService.LogTrace<ModalityController>("Locale parsing failed, defaulting to 'en'.", logProperties);
                languageCode = "en";
            }

            // Default to "commercial" if user type is not provided, and normalize to lowercase
            var userType = string.IsNullOrWhiteSpace(widgetRequest.UserType)
                ? "commercial"
                : widgetRequest.UserType.ToLowerInvariant();

            var omnichannelService = userType.Equals("eu") ? this.omnichannelEUService : this.omnichannelService;

            var ring = widgetRequest.Ring ?? "Ring4";

            // GetWidgetDetails for MCS
            WidgetDetails widgetDetailsMCS = await omnichannelService.GetWidgetDetails(languageCode, source, userType, true, ring);

            // GetWidgetDetails for Non MCS
            WidgetDetails widgetDetailsNonMCS = await omnichannelService.GetWidgetDetails(languageCode, source, userType, false, ring);

            List<WidgetDetails> widgetDetailsList = new();

            void AddIfValid(WidgetDetails widgetDetails)
            {
                if (widgetDetails?.WidgetId != null)
                {
                    widgetDetailsList.Add(widgetDetails);
                }
            }

            AddIfValid(widgetDetailsMCS);
            AddIfValid(widgetDetailsNonMCS);

            logProperties["WidgetDetailsList"] = JsonConvert.SerializeObject(widgetDetailsList);
            _telemetryService.LogTrace<ModalityController>("Returning widget details list", logProperties);
            return Ok(widgetDetailsList);
        }

        [Authorize]
        [HttpPost]
        [Route("getPredictedSkill")]
        public async Task<ActionResult<VDMResponse>> GetSkillPrediction([FromBody] ModalityRequest modalityRequest)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["RequestBody"] = JsonConvert.SerializeObject(modalityRequest);
            _telemetryService.LogTrace<ModalityController>("Received GetSkillPrediction request", logProperties);

            var supportTicketAttribute = modalityRequest.SupportTicketAttributes;
            var validationErrors = new List<string>();

            if (supportTicketAttribute == null)
            {
                validationErrors.Add("Support ticket attribute is missing.");
                logProperties["ValidationErrors"] = JsonConvert.SerializeObject(validationErrors);
                _telemetryService.LogTrace<ModalityController>("Validation failed", logProperties);
                return BadRequest(new { Errors = validationErrors });
            }

            if (string.IsNullOrWhiteSpace(supportTicketAttribute.Title))
            {
                validationErrors.Add("Title is missing or empty.");
            }
            if (string.IsNullOrWhiteSpace(supportTicketAttribute.Description))
                validationErrors.Add("Description is missing or empty.");

            if (string.IsNullOrWhiteSpace(supportTicketAttribute.SapId))
                validationErrors.Add("Sap Id is missing or empty.");

            if (validationErrors.Count != 0)
            {
                logProperties["ValidationErrors"] = JsonConvert.SerializeObject(validationErrors);
                _telemetryService.LogTrace<ModalityController>("Validation failed", logProperties);
                return BadRequest(new { Errors = validationErrors });
            }

            logProperties.Add("RequestId", modalityRequest.RequestId);
            logProperties.Add("Source", modalityRequest.Source);
            _telemetryService.LogTrace<ModalityController>("Calling VDMService.GetVDMSkill", logProperties);

            var vdmResponse = await vdmService.GetVDMSkill(
                new VDMRequest
                {
                    Text = supportTicketAttribute.Description,
                    Boundary = "public",
                    SapId = supportTicketAttribute.SapId,
                    PredictionPurposes = "crmee_ml_skill_model"
                },
                modalityRequest.RequestId
            );

            logProperties["VDMResponse"] = JsonConvert.SerializeObject(vdmResponse);

            if (vdmResponse?.SkillValue == null)
            {
                _telemetryService.LogTrace<ModalityController>("No skill prediction was returned by the VDM service for the provided support ticket.", logProperties);
                return NotFound(new
                {
                    ErrorCode = "SkillNotFound",
                    Message = "No skill prediction was returned by the VDM service for the provided support ticket."
                });
            }

            _telemetryService.LogTrace<ModalityController>("GetSkillPrediction Succeeded: returning VDMResponse", logProperties);
            return Ok(vdmResponse);
        }

        [Authorize]
        [HttpPost]
        [Route("createThemeSubjectMapping")]
        public async Task<ActionResult<ThemeSubjectMappingResponse>> CreateThemeSubjectMapping([FromBody] ThemeMappingRequest themeMappingRequest)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["ThemeMappingRequest"] = JsonConvert.SerializeObject(themeMappingRequest);

            _telemetryService.LogTrace<ModalityController>("Received CreateThemeSubjectMapping request", logProperties);

            // TODO: insert request null check

            ThemeSubjectMappingResponse response = null;

            foreach (var mapping in themeMappingRequest.ThemeRuleData)
            {
                try
                {
                    //TODO: insert mapping format validation

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

                    logProperties["FormattedThemeMappingRequest"] = JsonConvert.SerializeObject(themeSubjectMappingResponse);

                    response = await this.cosmosDbClient.UpsertItemAsync<ThemeSubjectMappingResponse>(themeSubjectMappingResponse);
                }
                catch (Exception ex)
                {
                    logProperties["Error"] = ex.ToString();
                    _telemetryService.LogError<ModalityController>("Error while processing createThemeSubjectMapping", logProperties);
                    return StatusCode(500, "CreateThemeSubjectMapping Failed: error occurred while processing the theme subject mapping.");
                }
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

                logProperties["ThemeSubjectMappingResponse"] = JsonConvert.SerializeObject(themesubjectmapping);
                _telemetryService.LogTrace<ModalityController>("CreateThemeSubjectMapping Succeeded: returning ThemeSubjectMappingResponse", logProperties);


                return Ok(themesubjectmapping);
            }
            _telemetryService.LogTrace<ModalityController>("CreateThemeSubjectMapping Failed: response is null", logProperties);
            return StatusCode(500, "CreateThemeSubjectMapping failed: response is null");
        }

        [Authorize]
        [HttpPost]
        [Route("createWidgetMapping")]
        public async Task<ActionResult<WidgetMappingResponse>> CreateWidgetMapping([FromBody] WidgetMappingRequest widgetRequest)
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
                languageCode = cultureInfo.TwoLetterISOLanguageName.ToLowerInvariant();

            }
            catch (Exception exception)
            {
                // fallback to en-us
                languageCode = "en";
                widgetRequest.Locale = "en-us";
            }

            var userType = widgetRequest.UserType;

            if (string.IsNullOrEmpty(userType))
            {
                userType = "commercial"; //default value is commercial
            }

            userType = userType.ToLowerInvariant();

            var omnichannelService = userType.Equals("eu") ? this.omnichannelEUService : this.omnichannelService;

            WidgetMappingResponse widgetMappingResponse = await omnichannelService.CreateWidgetDetails(widgetRequest, languageCode, source, userType);

            return Ok(widgetMappingResponse);
        }

        private static bool ValidateConciergeChat(ModalityRequest modalityRequest, string language)
        {
            if (modalityRequest.ExtensionAttributes != null && modalityRequest.ExtensionAttributes.ContainsKey("Theme"))
            {
                string theme = modalityRequest.ExtensionAttributes["Theme"];
                if (theme == null)
                {
                    return false;
                }

                bool isTrial;
                bool isBizAssist;
                bool isProdirect;

                if ((modalityRequest.ExtensionAttributes.ContainsKey("IsTrial") && bool.TryParse(modalityRequest.ExtensionAttributes["IsTrial"], out isTrial) && isTrial)
                    || (modalityRequest.ExtensionAttributes.ContainsKey("IsBizAssist") && bool.TryParse(modalityRequest.ExtensionAttributes["IsBizAssist"], out isBizAssist) && isBizAssist)
                    || (modalityRequest.ExtensionAttributes.ContainsKey("IsProdirect") && bool.TryParse(modalityRequest.ExtensionAttributes["IsProdirect"], out isProdirect) && isProdirect))
                {
                    return false;
                }

                HashSet<string> themes = new HashSet<string> {"Office Client - activate office apps",
                    "Prevent user accounts from getting compromised", "Office Client - Word",
                    "Office Client - download and install office apps", "Admin - Sign in and password issues", "Commerce - Manage bills, payments, subscriptions and licenses",
                    "Admin - Manage my users, groups and resources", "Outlook - Setup and use Outlook (including Mac)", "Office Client - Use Office apps (including Mac)"};

                return modalityRequest.CustomerAttributes?.SubscriptionType != "1" && language.Equals("ENG") && themes.Contains(theme, StringComparer.OrdinalIgnoreCase);
            }

            return false;
        }

        private static bool ValidateSCIMChat(ModalityRequest modalityRequest, string language)
        {
            if (modalityRequest.ExtensionAttributes != null && modalityRequest.ExtensionAttributes.ContainsKey("AimIssue"))
            {
                string aimIssue = modalityRequest.ExtensionAttributes["AimIssue"];
                if (aimIssue == null)
                {
                    return false;
                }

                HashSet<string> aimIssues = new HashSet<string> { "6700002", "9012387", "9012386", "9023195", "9023950", "9000171", "9000221", "9000652", "9000654", "9000662", "9012172", "9004438",
                    "6700005", "6200002", "6700006", "6700008", "9003834", "6700003", "9004639", "9004635", "9004638", "9004644", "9004636", "9003771", "9004641", "9004640", "9004634", "9003785", "9003781", "9024087", "9023049", "9012173" };

                return (!String.IsNullOrWhiteSpace(modalityRequest.CustomerAttributes?.SubscriptionType) && modalityRequest.CustomerAttributes.SubscriptionType != "1")
                    && (!String.IsNullOrWhiteSpace(language) && language.Equals("ENG"))
                    && aimIssues.Contains(aimIssue, StringComparer.OrdinalIgnoreCase);
            }

            return false;
        }

        /// <summary>
        /// Get the SkillData from user preference
        /// </summary>
        /// <param name="userLcid">User LCID</param>
        /// <returns>The SkillData</returns>
        private static ICollection<LanguageSkillData> UserLcidToSkillData(int userLcid)
        {
            CultureInfo cultureInfo = null;
            RegionInfo regionInfo = null;
            try
            {
                cultureInfo = new CultureInfo(userLcid);
            }
            catch (Exception ex)
            {
                return LanguageSkillData.Default;
            }

            try
            {
                regionInfo = new RegionInfo(userLcid);
            }
            catch (Exception ex)
            {

            }

            string language = cultureInfo.ThreeLetterISOLanguageName;
            if (regionInfo != null)
            {
                if (regionInfo.TwoLetterISORegionName.Equals(BusinessHourConstants.PortgualPortugueseSuffix))
                {
                    language += BusinessHourConstants.PortgualPortugueseSuffix;
                }
                else if (regionInfo.TwoLetterISORegionName.Equals(BusinessHourConstants.TaiwanChineseSuffix))
                {
                    language += BusinessHourConstants.TaiwanChineseSuffix;
                }
                else if (regionInfo.TwoLetterISORegionName.Equals(BusinessHourConstants.HongkongChineseSuffix))
                {
                    language += BusinessHourConstants.HongkongChineseSuffix;
                }
            }

            if (!string.IsNullOrWhiteSpace(language))
            {
                foreach (KeyValuePair<string, string> isoCodes in IsoLanguageReplacementCodes)
                {
                    if (language.Contains(isoCodes.Key))
                    {
                        language = language.Replace(isoCodes.Key, isoCodes.Value);
                        break;
                    }
                }
            }

            LanguageSkill languageSkill;
            if (!Enum.TryParse<LanguageSkill>(language, true, out languageSkill))
            {
                languageSkill = LanguageSkill.ENG;
            }

            IList<LanguageSkillData> result = new List<LanguageSkillData>
            {
                new LanguageSkillData() { SkillName = languageSkill },
            };

            return result;
        }

        /// <summary>
        /// Flag to determine if it is Business Hours
        /// </summary>
        /// <param name="datas">Data</param>
        /// <returns>Flag to show if it is Business Hour</returns>
        private static bool IsBusinessHour(List<BusinessHour> datas)
        {
            bool isBusinessHour = false;
            if (datas != null && datas.Count > 0)
            {
                var businessHourData = datas.FirstOrDefault();

                if (IsBusinessHour(businessHourData))
                {
                    isBusinessHour = true;
                }
            }
            else
            {
                isBusinessHour = true;
            }

            return isBusinessHour;
        }

        /// <summary>
        /// Check whether it is in business hours now
        /// </summary>
        /// <param name="data">Business Hours</param>
        /// <returns>Whether it is in business hours now</returns>
        private static bool IsBusinessHour(BusinessHour data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("business hour data is null");
            }

            DateTime now = DateTime.UtcNow;
            TimeZoneInfo timezone = TimeZoneInfo.FindSystemTimeZoneById(data.TimeZoneName);
            DateTime localNow = TimeZoneInfo.ConvertTime(now, timezone);

            // Calcalute UtcOffset
            string timeStr1 = localNow.ToString("MM/dd/yyyy HH:mm");
            string timeStr2 = now.ToString("MM/dd/yyyy HH:mm");
            data.UtcOffset = (Convert.ToDateTime(timeStr1) - Convert.ToDateTime(timeStr2)).ToString(@"hh\:mm\:ss");

            // Check if business hour
            int currentDayOfWeek = (int)localNow.DayOfWeek;
            double currentTime = localNow.Hour + (localNow.Minute / 100.0);
            double dataStartTime = data.StartHour + (data.StartMin / 100.0);
            double dataEndTime = data.EndHour + (data.EndMin / 100.0);

            // case to accomodate crossing of days when support schedule ends in the next day
            if (dataStartTime > dataEndTime && data.StartDayOfWeek == data.EndDayOfWeek)
            {
                int dataEndDay = (data.StartDayOfWeek == (int)DayOfWeek.Saturday) ? (int)DayOfWeek.Sunday : data.StartDayOfWeek + 1;

                return (currentDayOfWeek == data.StartDayOfWeek && currentTime >= dataStartTime) || (currentDayOfWeek == dataEndDay && currentTime < dataEndTime);
            }

            return (currentDayOfWeek >= data.StartDayOfWeek && currentDayOfWeek <= data.EndDayOfWeek) && ((dataStartTime == dataEndTime) || (currentTime >= dataStartTime && currentTime < dataEndTime));
        }

        private static bool ValidateBusinessHourData(BusinessHour businessHourData)
        {
            return businessHourData != null && businessHourData.StartDayOfWeek != null
                && businessHourData.EndDayOfWeek != null && businessHourData.EndHour != null
                && businessHourData.StartMin != null && businessHourData.EndMin != null;
        }

        /// <summary>
        /// Gets a value of ISO language replacement codes
        /// </summary>
        private static readonly IDictionary<string, string> IsoLanguageReplacementCodes = new Dictionary<string, string>
        {
            { "nob", "nor" },
            { "dnk", "dan" },
            { "hrb", "hrv" },
            { "srn", "srp" },
            { "srs", "srp" },
        };

        private static BusinessHour GetConciergeBusinessHours()
        {
            return new BusinessHour
            {
                StartDayOfWeek = 1,
                EndDayOfWeek = 5,
                StartHour = 4,
                EndHour = 16,
                TimeZoneName = "Pacfic Standard Time",
                UtcOffset = "07:00:00",
                StartMin = 0,
                EndMin = 0
            };
        }

        private static BusinessHour GetSCIMBusinessHours()
        {
            return new BusinessHour
            {
                StartDayOfWeek = 1,
                EndDayOfWeek = 5,
                StartHour = 5,
                EndHour = 15,
                TimeZoneName = "Pacfic Standard Time",
                UtcOffset = "07:00:00",
                StartMin = 0,
                EndMin = 0

            };
        }

        private static BusinessHour GetEmeraldBusinessHours()
        {
            return new BusinessHour
            {
                StartDayOfWeek = 1,
                EndDayOfWeek = 0,
                StartHour = 7,
                EndHour = 18,
                TimeZoneName = "Pacfic Standard Time",
                UtcOffset = "07:00:00",
                StartMin = 0,
                EndMin = 0
            };
        }

        private static string GetLanguage(string locale, int userLCID)
        {
            string languageCode = "en";
            try
            {
                CultureInfo cultureInfo = locale != null ? new CultureInfo(locale) : new CultureInfo(userLCID);
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
                return languageCode;
            }

            return languageCode;
        }
    }
}