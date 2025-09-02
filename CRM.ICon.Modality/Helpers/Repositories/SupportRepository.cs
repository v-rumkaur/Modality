using Azure.Core;
using Azure.Identity;
using CRM.ICon.Modality.Configuration;
using CRM.ICon.Modality.Helpers.Logging;
using CRM.ICon.Modality.Model.Modalities;
using CRM.ICon.Modality.Services.Modality;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.ServiceEssentials.Telemetry.Abstractions;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Web;

using System;
using System.Collections.Generic;

using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using System.Configuration;


namespace CRM.ICon.Modality.Helpers.Repositories
{
    public class SupportRepository : ISupportRepository
    {
        // Compass values
        private const string LocalePlaceholder = "{lang-locale}";

        private const string MenuPagesKey = "in-product-menu-pages";
        private const string MetaTagsKey = "meta-tags";
        private const string NameKey = "#name";
        private const string LinksKey = "links";
        private const string LinkKey = "link";
        private const string UrlKey = "link-url";
        private const string ExternalLinkKey = "external-link";
        private const string DescriptionKey = "description";
        private const string Form = "/form/";

        // Modality values
        private const string ContactUsPath = "/contact/";
        private const string HoopsPath = "/contact/hoops/";
        private const string VNextPath = "/v1.0/";

        private const string SubjectQueryParamName = "subject";
        private const string SubjectIdQueryParamName = "subjectid";
        private const string ModalityQueryParamName = "modality";
        private const string AuthtypeQueryParamName = "authtype";
        private const string FormnameQueryParamName = "formname";
        private const string LanguageQueryParamName = "language";
        private const string CountryQueryParamName = "country";
        private const string ModeQueryParamName = "mode";
        private const string PartnerIdParamName = "partnerId";
        private const string PreviewParamName = "preview";
        private const string DisabilityQueryParamName = "disability";
        private const string ProductQueryParamName = "product";
        private const string IssueQueryParamName = "issue";

        private const string DefaultFallbackLocale = "en-us";
        private const string Unauth = "unauth";
        private const int FormnameIndex = 3;

        #region modality and metatag names
        private const string Chat = "chat";
        private const string OCChat = "occhat";
        private const string Callback = "callback";
        private const string MessageGamer = "message-gamer";
        private const string ScheduleCallback = "schedulecallback";
        private const string TollFree = "tollfree";
        private const string CareLocator = "carelocator";
        private const string DeviceRepair = "devicerepair";
        private const string ComplaintForm = "complaintform";
        private const string ServicesAndSubscriptions = "servicesandsubscriptions";
        private const string PaymentOptions = "paymentoptions";
        private const string Orders = "findpurchaseortransaction";
        private const string ResetPassword = "resetpassword";
        private const string OemSupportNamed = "contactoemsupport";
        private const string OemSupportAnon = "contactanonymousoem";
        private const string OemSupportMicrosoft = "contactmsoem";
        private const string InstantAnswer = "instantanswer";
        private const string ScheduleAppointment = "scheduleappointment";

        private const string ScheduleCallbackTag = "schedule-call";
        private const string PhoneTag = "phone";
        private const string CareLocatorTag = "care-locator";
        private const string DeviceRepairTag = "device-repair";
        private const string ComplaintFormTag = "complaint-form";
        private const string ServicesAndSubscriptionsTag = "services-subscriptions";
        private const string PaymentOptionsTag = "payment-options";
        private const string OrdersTag = "orders";
        private const string ResetPasswordTag = "reset-password";
        private const string OemSupportNamedTag = "device-custom";
        private const string OemSupportAnonTag = "device-custom,anon-oem";
        private const string OemSupportMicrosoftTag = "device-custom,ms-device";
        private const string InstantAnswerTag = "instant-answer";
        private const string ScheduleAppointmentTag = "schedule-appointment";
        #endregion

        /// <summary>
        /// A map of metatag to supportchannel names, for back compat with Modalities APIs, Unversioned and V1
        /// </summary>
        private static readonly Dictionary<string, string> metaTagMap = new Dictionary<string, string>
            {
                {ScheduleCallbackTag, ScheduleCallback},
                {PhoneTag, TollFree},
                {CareLocatorTag, CareLocator},
                {DeviceRepairTag, DeviceRepair},
                {ComplaintFormTag, ComplaintForm},
                {ServicesAndSubscriptionsTag, ServicesAndSubscriptions},
                {PaymentOptionsTag, PaymentOptions},
                {OrdersTag, Orders},
                {ResetPasswordTag, ResetPassword},
                {OemSupportNamedTag, OemSupportNamed},
                {OemSupportAnonTag, OemSupportAnon},
                {OemSupportMicrosoftTag, OemSupportMicrosoft},
                {InstantAnswerTag, InstantAnswer},
                {ScheduleAppointmentTag, ScheduleAppointment}
            };

        /// <summary>
        /// The fallback locales.
        /// </summary>
        private readonly Dictionary<string, string> fallbackLocales;

        /// <summary>
        /// The compass service.
        /// </summary>
        private readonly ICompassService compassService;

        /// <summary>
        /// The partner config
        /// </summary>
        private readonly IPartnerConfiguration partnerConfig;

        /// <summary>
        /// The partner config
        /// </summary>
        private readonly IVNextConfiguration vNextConfig;

        private readonly ISllLogger sllLogger;

        private readonly OCQueueAvailabilityServiceConfiguration ocQueueAvailabilityServiceConfiguration;

        private readonly ConfigurationMappingDocDbProvider configurationMappingDocDbProvider;

        private readonly HttpClient httpClient;

        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="SupportRepository"/> class.
        /// </summary>
        /// <param name="compassService">The compass service.</param>
        /// <param name="partnerConfig">the partner config</param>
        /// <param name="vNextConfig">the vNext config</param>
        public SupportRepository(
            Services.Modality.ICompassService compassService,
            IPartnerConfiguration partnerConfig,
            IVNextConfiguration vNextConfig,
            ISllLogger sllLogger,
            OCQueueAvailabilityServiceConfiguration ocQueueAvailabilityServiceConfiguration,
            ConfigurationMappingDocDbProvider configurationMappingDocDbProvider,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            this.compassService = compassService;
            this.fallbackLocales = compassService.GetFallbackLocales();
            this.partnerConfig = partnerConfig;
            this.vNextConfig = vNextConfig;
            this.sllLogger = sllLogger;
            this.ocQueueAvailabilityServiceConfiguration = ocQueueAvailabilityServiceConfiguration;
            this.configurationMappingDocDbProvider = configurationMappingDocDbProvider;
            this.httpClient = httpClient;
            this.configuration = configuration;
        }

        /// <summary>Gets modalities which correspond to the given params</summary>
        /// <param name="product">The product.</param>
        /// <param name="issue">The issue.</param>
        /// <param name="partnerId">The partner id.</param>
        /// <param name="platform">The platform.</param>
        /// <param name="preview">The is preview.</param>
        /// <param name="mode">The mode</param>
        /// <param name="disability">The accessibility.</param>
        /// <param name="locale">The locale.</param>
        /// <param name="host">The assisted support environment</param>
        /// <param name="isVNext">If is VNext request</param>
        /// <param name="isTest">If is a test request</param>
        /// <returns>A collection of support channels as dictionaries</returns>
        public async Task<IEnumerable<Dictionary<string, string>>> GetModalities(
            string product,
            string issue,
            string partnerId,
            string platform,
            bool preview,
            string mode,
            bool disability,
            string locale,
            string host,
            bool isVNext,
            bool isTest)
        {
            var fullProductPath = string.Format(this.ocQueueAvailabilityServiceConfiguration.BueUrl, product);
            PublishContent compassContent;
            string requestId = null;
            try
            {
                var token = await this.GenerateToken();
                string isPreview = preview.ToString().ToLowerInvariant();
                var uriBuilder = new UriBuilder(fullProductPath);
                var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                query["$filter"] = $"Locale eq '{locale}' and IsPreview eq {isPreview}";
                uriBuilder.Query = query.ToString();
                var message = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // unique guid needed by BUE team when calling their API
                requestId = Guid.NewGuid().ToString();

                message.Headers.TryAddWithoutValidation("X-RequesT-ID", requestId);
                message.Headers.TryAddWithoutValidation("X-Application-ID", "c1413fd6-4a0d-4cbc-83d1-79bd0898cb02");
                message.Headers.TryAddWithoutValidation("X-Tenant-ID", this.ocQueueAvailabilityServiceConfiguration.BueTenantId);
                compassContent = await this.compassService.GetCompassContentAsync(this.httpClient, message, fullProductPath, locale, preview);
                this.sllLogger.WriteInformationalTelemetry("X-RequesT-ID", requestId);
            }
            catch (Exception exception)
            {
                this.sllLogger.WriteErrorTelemetry("GetModalities", "Get Modalities call for ConfigHub Failed Exception: {0} RequestId: {1}", exception, requestId);
                throw;
            }

            var modalities = await this.ConstructModalities(
                compassContent,
                product,
                issue,
                locale,
                partnerId,
                mode,
                preview,
                disability,
                host,
                isVNext,
                isTest);

            return modalities;
        }

        /// <summary>
        /// Gets the fallback locale, if available, for a given locale
        /// </summary>
        /// <param name="locale">the locale</param>
        /// <returns>The mapped fallback locale, if exists, else default fallback locale.</returns>
        public string GetFallbackLocale(string locale)
        {
            return this.fallbackLocales.ContainsKey(locale) ? this.fallbackLocales[locale] : DefaultFallbackLocale;
        }

        /// <summary>
        /// Constructs a set of modalities
        /// </summary>
        /// <param name="compassContent">The compass content.</param>
        /// <param name="product">The product id.</param>
        /// <param name="issue">The issue.</param>
        /// <param name="locale">The locale.</param>
        /// <param name="partnerId">The partner id.</param>
        /// <param name="mode">The mode</param>
        /// <param name="preview">The preview.</param>
        /// <param name="disability">The accessibility.</param>
        /// <param name="host">The assisted support host</param>
        /// <param name="isVNext">If is VNext request</param>
        /// <param name="isTest">If is a test request</param>
        /// <returns>The <see cref="Task"/>.</returns>
        protected async Task<IEnumerable<Dictionary<string, string>>> ConstructModalities(PublishContent compassContent, string product, string issue, string locale, string partnerId, string mode, bool preview, bool disability, string host, bool isVNext, bool isTest)
        {
            if (compassContent == null)
            {
                return await Task.FromResult(new List<Dictionary<string, string>>());
            }

            var modalities = new List<Dictionary<string, string>>();
            var productIssueKey = string.Format(CultureInfo.InvariantCulture, "{0} - {1}", product, issue);
            foreach (var compassIssue in compassContent[MenuPagesKey].Items)
            {
                if (compassIssue[NameKey].Value.Equals(productIssueKey, StringComparison.OrdinalIgnoreCase))
                {
                    var compassIssueLinks = compassIssue[LinksKey].Items;
                    foreach (var link in compassIssueLinks)
                    {
                        // Translate from the Compass tag to the json field name.
                        var translatedName = this.TranslateName(link[MetaTagsKey].Value);
                        if (!string.IsNullOrWhiteSpace(translatedName))
                        {
                            var result = await this.GetModalities(
                            link,
                            translatedName,
                            product,
                            issue,
                            locale,
                            partnerId,
                            mode,
                            disability,
                            preview,
                            host,
                            isVNext,
                            isTest);
                            if (result.Count > 0)
                            {
                                modalities.Add(result);
                            }
                        }
                    }
                }
            }

            return await Task.FromResult(modalities);
        }

        /// <summary>
        /// Get modalities which correspond to the given params
        /// </summary>
        /// <param name="link">The link.</param>
        /// <param name="name">The name.</param>
        /// <param name="locale">The locale.</param>
        /// <param name="partnerId">The partner id.</param>
        /// <param name="mode">The mode</param>
        /// <param name="disability">The accessibility.</param>
        /// <param name="preview">The preview.</param>
        /// <param name="host">The assisted support environment</param>
        /// <param name="isVNext">If is VNext request</param>
        /// <param name="isTest">If is a test request</param>
        /// <returns>The <see cref="Task"/>.</returns>
        protected async Task<Dictionary<string, string>> GetModalities(PublishingField link, string name, string product, string issue, string locale, string partnerId, string mode, bool disability, bool preview, string host, bool isVNext, bool isTest)
        {
            var assistedSupportUri = string.IsNullOrWhiteSpace(host) ?
                partnerConfig.GetUri() :
                new UriBuilder(partnerConfig.Scheme, host, partnerConfig.Port).Uri;

            var modalities = new Dictionary<string, string>();

            var circuitBreakerFlag = await this.GetCircuitBreakerFlag(product, issue, locale, name);

            // Is Circuit Breaker Flag Active
            if (circuitBreakerFlag)
            {
                return await Task.FromResult(modalities);
            }

            modalities.Add(ModalityField.Name, name);

            var uriString = await this.GetUriString(link, locale);

            // strip 'form' if present
            var hoopsUri = uriString;
            if (Regex.IsMatch(hoopsUri, Form, RegexOptions.IgnoreCase))
            {
                var fields = hoopsUri.Split('/');
                if (fields.Length > 5)
                {
                    var subjectIdPart = fields[2];
                    var modalityPart = fields[4];
                    hoopsUri = string.Format(CultureInfo.InvariantCulture, "/{0}/{1}/", modalityPart, subjectIdPart);
                }
            }

            bool isExternalLink;
            bool.TryParse(link[ExternalLinkKey].Value, out isExternalLink);

            // Is External Modality
            if (isExternalLink)
            {
                // Add external Link defined in Compass
                //
                // Is Message-gamer (hard-coded for a custom partnerId for message-gamer2)
                if (name.Contains(MessageGamer) && partnerId.Equals("e4381e95-b3fb-48f5-be50-e59142564496"))
                {
                    modalities.Add(ModalityField.Link, HttpUtility.HtmlDecode(uriString.Replace("cbc3e761-f603-4a97-9113-2b09298272db", "e4381e95-b3fb-48f5-be50-e59142564496")));
                }
                else
                {
                    modalities.Add(ModalityField.Link, HttpUtility.HtmlDecode(uriString));
                }

                // Is occhat modality
                if (name.Equals(OCChat))
                {
                    // Extract whatever is after the last slash of the given uriString as the subject Id
                    var splits = uriString.Split('/');
                    int subjectId;
                    int.TryParse(splits[splits.Length - 1], out subjectId);
                    var localeParts = locale.Split('-');
                    var language = localeParts[0];
                    var country = localeParts[1];

                    // Add OpenNow link
                    modalities.Add(
                        ModalityField.OpenNowLink,
                        this.CreateLink(
                            "api/hoops/open/",
                            subjectId,
                            Chat,
                            language,
                            country,
                            partnerId,
                            mode,
                            disability,
                            preview,
                            assistedSupportUri).ToString());

                    // Add Hoops link
                    modalities.Add(
                        ModalityField.HoopsLink,
                        this.CreateLink(
                            locale + HoopsPath + Chat + "/" + subjectId + "/",
                            null,
                            null,
                            null,
                            null,
                            partnerId,
                            mode,
                            disability,
                            preview,
                            assistedSupportUri).ToString());

                    // Add QueueAvailability link
                    modalities.Add(
                        ModalityField.QueueAvailability,
                        this.CreateLink(
                            "api/queue/availability/",
                            subjectId,
                            product,
                            issue,
                            null,
                            language,
                            country,
                            partnerId,
                            mode,
                            disability,
                            preview,
                            assistedSupportUri).ToString());
                }
            }

            // Is Chat or Callback modality
            else if (name.Equals(Chat) || name.Equals(Callback))
            {
                // Subject regex. Match a '/' but discard it, match multiple decimals followed by a '/' at the end of the string which is discarded
                // example: "/form/92/unauth/chat/" -> 92
                var subjectRegex = new Regex("(?!\\/)\\d+?(?=\\/|$)");
                var match = subjectRegex.Match(uriString);
                int subjectId;
                int.TryParse(match.Value, out subjectId);

                // Add Link
                if (isVNext)
                {
                    // check for formname
                    string formname = null;
                    string authtype = null;
                    if (uriString.Contains(Form))
                    {
                        formname = uriString.Split('/')[FormnameIndex];
                        authtype = Unauth;
                    }

                    Uri vNextUri = new UriBuilder(vNextConfig.Scheme, isTest ? vNextConfig.TestHost : vNextConfig.Host, vNextConfig.Port).Uri;
                    modalities.Add(
                    ModalityField.Link,
                    this.CreateLink(
                        locale + VNextPath,
                        subjectId,
                        null,
                        null,
                        name,
                        authtype,
                        formname,
                        null,
                        null,
                        partnerId,
                        mode,
                        disability,
                        preview,
                        vNextUri,
                        true).ToString());
                }
                else
                {
                    modalities.Add(
                    ModalityField.Link,
                    this.CreateLink(
                        locale + ContactUsPath + uriString.TrimStart('/'),
                        null,
                        null,
                        null,
                        null,
                        partnerId,
                        mode,
                        disability,
                        preview,
                        assistedSupportUri).ToString());
                }

                var localeParts = locale.Split('-');
                var language = localeParts[0];
                var country = localeParts[1];

                if (localeParts.Length == 3)
                {
                    language = language + '-' + localeParts[1];
                    country = localeParts[2];
                }

                // Add OpenNow link
                modalities.Add(
                    ModalityField.OpenNowLink,
                    this.CreateLink(
                        "api/hoops/open/",
                        subjectId,
                        name,
                        language,
                        country,
                        partnerId,
                        mode,
                        disability,
                        preview,
                        assistedSupportUri).ToString());

                // Add Hoops Link
                modalities.Add(
                    ModalityField.HoopsLink,
                    this.CreateLink(
                        locale + HoopsPath + hoopsUri.TrimStart('/'),
                        null,
                        null,
                        null,
                        null,
                        partnerId,
                        mode,
                        disability,
                        preview,
                        assistedSupportUri).ToString());

                // Add QueueLength link
                modalities.Add(
                    ModalityField.QueueLength,
                    this.CreateLink(
                        "api/queue/length/",
                        subjectId,
                        null,
                        language,
                        country,
                        partnerId,
                        mode,
                        disability,
                        preview,
                        assistedSupportUri).ToString());

                // Add WaitTime link
                modalities.Add(
                    ModalityField.WaitTime,
                    this.CreateLink(
                        "api/queue/waittime/",
                        subjectId,
                        name,
                        language,
                        country,
                        partnerId,
                        mode,
                        disability,
                        preview,
                        assistedSupportUri).ToString());

            }

            // Is Phone Modality
            else if (name.Equals(TollFree))
            {
                // Subject regex. Match a '/' but discard it, match multiple decimals followed by a '/' at the end of the string which is discarded
                var subjectRegex = new Regex("(?!\\/)\\d+?(?=\\/|$)");
                var match = subjectRegex.Match(uriString);
                int subjectId;
                int.TryParse(match.Value, out subjectId);
                var localeParts = locale.Split('-');
                var language = localeParts[0];
                var country = localeParts[1];

                // Add OpenNow link
                modalities.Add(
                    ModalityField.OpenNowLink,
                    this.CreateLink(
                        "api/hoops/open/",
                        subjectId,
                        name,
                        language,
                        country,
                        partnerId,
                        mode,
                        disability,
                        preview,
                        assistedSupportUri).ToString());

                // Add Hoops link
                modalities.Add(
                    ModalityField.HoopsLink,
                    this.CreateLink(
                        locale + HoopsPath + hoopsUri.TrimStart('/'),
                        null,
                        null,
                        null,
                        null,
                        partnerId,
                        mode,
                        disability,
                        preview,
                        assistedSupportUri).ToString());

                // Add Phone link
                modalities.Add(ModalityField.Phone, link[DescriptionKey].Value);
            }

            // Is Instant Answer modality
            else if (name.Equals(InstantAnswer))
            {
                // Add Article ID
                modalities.Add(ModalityField.ArticleId, link[LinkKey][UrlKey].Value);
            }

            // Is basic Link modality
            else
            {
                // Add Link
                modalities.Add(
                    ModalityField.Link,
                    this.CreateLink(
                        locale + ContactUsPath + uriString.TrimStart('/'),
                        null,
                        null,
                        null,
                        null,
                        partnerId,
                        mode,
                        disability,
                        preview,
                        assistedSupportUri).ToString());
            }

            return await Task.FromResult(modalities);
        }

        /// <summary>
        /// The get uri string.
        /// </summary>
        /// <param name="link">The link.</param>
        /// <param name="locale">The locale.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        protected virtual async Task<string> GetUriString(PublishingField link, string locale)
        {
            var uriField = link[LinkKey][UrlKey];
            var uri = string.Empty;

            // Replace lang-locale if necessary in link field
            if (uriField?.Value != null)
            {
                uri = uriField.Value.Replace(LocalePlaceholder, locale.ToLowerInvariant());
            }

            return await Task.FromResult(uri);
        }

        /// <summary>
        /// The translate name.
        /// </summary>
        /// <param name="compassTag">The friendly name.</param>
        /// <returns>The <see cref="string"/>.</returns>
        protected string TranslateName(string compassTag)
        {
            var metaTag = compassTag.ToLowerInvariant();
            return metaTagMap.ContainsKey(metaTag) ? metaTagMap[metaTag] : metaTag;
        }

        // An override function to the CreateLink; this one doesn't take authtype and formname as parameters
        private Uri CreateLink(
            string linkPath,
            int? subject,
            string modality,
            string language,
            string country,
            string partnerId,
            string mode,
            bool? disability,
            bool? preview,
            Uri host)
        {
            return CreateLink(linkPath, subject, null, null, modality, null, null, language, country, partnerId, mode, disability, preview, host, false);
        }

        // An override function to the CreateLink; this one takes product and issue
        private Uri CreateLink(
            string linkPath,
            int? subject,
            string product,
            string issue,
            string modality,
            string language,
            string country,
            string partnerId,
            string mode,
            bool? disability,
            bool? preview,
            Uri host)
        {
            return CreateLink(linkPath, subject, product, issue, modality, null, null, language, country, partnerId, mode, disability, preview, host, false);
        }

        /// <summary>
        /// The create link.
        /// </summary>
        /// <param name="linkPath">The link path.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="modality">The modality.</param>
        /// <param name="authtype">The authtype.</param>
        /// <param name="formname">The formname.</param>
        /// <param name="language">The language.</param>
        /// <param name="country">The country.</param>
        /// <param name="partnerId">The partner id.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="disability">The disability.</param>
        /// <param name="preview">The preview.</param>
        /// <param name="host">The assisted support environment</param>
        /// <param name="isLink">If this is invoded when creating the Link</param>
        /// <returns>The <see cref="Uri"/>.</returns>
        protected Uri CreateLink(
            string linkPath,
            int? subject,
            string product,
            string issue,
            string modality,
            string authtype,
            string formname,
            string language,
            string country,
            string partnerId,
            string mode,
            bool? disability,
            bool? preview,
            Uri host,
            bool isLink)
        {
            var qb = this.CreateQueryStringBuilder(
                modality,
                subject,
                product,
                issue,
                authtype,
                formname,
                language,
                country,
                partnerId,
                mode,
                disability,
                preview,
                isLink);
            var builder = CreateUriBuilder(host, linkPath);
            return qb.AddToUri(builder.Uri);
        }

        /// <summary>
        /// Factory for creating URL Queries from a modality (will not add null values to the query)
        /// </summary>
        /// <param name="modalityName">The modality name to use</param>
        /// <param name="subjectId">The subjectId</param>
        /// <param name="product"></param>
        /// <param name="issue"></param>
        /// <param name="authtype">The authtype.</param>
        /// <param name="formname">The formname.</param>
        /// <param name="language">The language to use</param>
        /// <param name="country">The country to use</param>
        /// <param name="partnerId">Partner id for logging</param>
        /// <param name="mode">The mode to use. Default is "live".</param>
        /// <param name="disability">Accessibility resources requested. Default is "none".</param>
        /// <param name="preview">Request draft resources. Default is false</param>
        /// <param name="isLink">If this is invoded when creating the Link</param>
        /// <returns>Uri Query Builder.</returns>
        private UriQueryBuilder CreateQueryStringBuilder(
            string modalityName,
            int? subjectId,
            string product,
            string issue,
            string authtype,
            string formname,
            string language,
            string country,
            string partnerId,
            string mode,
            bool? disability,
            bool? preview,
            bool isLink)
        {
            var qb = new UriQueryBuilder();
            AddToQueryBuilder(qb, ModalityQueryParamName, modalityName);
            AddToQueryBuilder(qb, isLink ? SubjectIdQueryParamName : SubjectQueryParamName, subjectId?.ToString(CultureInfo.InvariantCulture));
            AddToQueryBuilder(qb, ProductQueryParamName, product);
            AddToQueryBuilder(qb, IssueQueryParamName, issue);
            AddToQueryBuilder(qb, AuthtypeQueryParamName, authtype);
            AddToQueryBuilder(qb, FormnameQueryParamName, formname);
            AddToQueryBuilder(qb, LanguageQueryParamName, language);
            AddToQueryBuilder(qb, CountryQueryParamName, country);
            AddToQueryBuilder(qb, DisabilityQueryParamName, disability?.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
            AddToQueryBuilder(qb, ModeQueryParamName, mode);
            AddToQueryBuilder(qb, PartnerIdParamName, partnerId);

            if (preview != null && preview.Value == true)
            {
                AddToQueryBuilder(qb, PreviewParamName, true.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
            }

            return qb;
        }

        /// <summary>
        /// Add a string to the query builder if the string is not null
        /// </summary>
        /// <param name="qb">The query builder to add to</param>
        /// <param name="name">The value name to add</param>
        /// <param name="value">The value to add</param>
        private static void AddToQueryBuilder(UriQueryBuilder qb, string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                qb.Add(name, value);
            }
        }

        /// <summary>
        /// Create a Uri Builder from a Uri and optional path
        /// </summary>
        /// <param name="uri">The Uri</param>
        /// <param name="path">the path</param>
        /// <returns></returns>
        private static UriBuilder CreateUriBuilder(Uri uri, string path = null)
        {
            return new UriBuilder(uri.Scheme, uri.Host, uri.Port, path);
        }

        /// <summary>
        /// Gets the circuit breaker flag
        /// </summary>
        /// <param name="product"></param>
        /// <param name="issue"></param>
        /// <param name="locale"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<bool> GetCircuitBreakerFlag(string product, string issue, string locale, string name)
        {

            string id = "circuitbreaker";

            if (this.configurationMappingDocDbProvider != null)
            {
                var result = await this.configurationMappingDocDbProvider.ExecuteQueryAsync<CircuitBreaker>(id, this.sllLogger);

                if (result != null)
                {
                    var circuitBreaker = result.Item2;
                    if (circuitBreaker != null)
                    {
                        if (circuitBreaker.isGlobal)
                        {
                            return true;
                        }
                        var modalities = circuitBreaker.Modalities;
                        if (modalities != null)
                        {
                            foreach (CRM.ICon.Modality.Model.Modalities.Modality modality in modalities)
                            {
                                var modalityName = modality.modality;
                                var productName = modality.product;
                                if (modalityName != null && modalityName.Equals(name) && productName != null && productName.Equals(product))
                                {
                                    return modality.isActive;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Updates the circuit breaker modalities and returns response
        /// </summary>
        /// <param name="circuitBreaker"></param>
        /// <returns>Updated CircuitBreaker modalities</returns>
        public async Task<CircuitBreaker> UpdateCircuitBreaker(CircuitBreaker circuitBreaker)
        {
            string id = "circuitbreaker";

            if (this.configurationMappingDocDbProvider != null)
            {
                var result = await this.configurationMappingDocDbProvider.ExecuteUpdateQueryAsync<CircuitBreaker>(circuitBreaker, this.sllLogger, id);
                return result;
            }

            return null;
        }

        private async Task<string> GenerateToken()
        {
            var managedIdentityId = this.configuration.GetStringValue(ModalityConstants.KeyvaultConfigSectionName, ModalityConstants.ManagedIdentityId);
            var credential = new ManagedIdentityCredential(managedIdentityId);
            var result = await credential.GetTokenAsync(new TokenRequestContext(new[] { this.ocQueueAvailabilityServiceConfiguration.BueScopeId })).ConfigureAwait(false);
            var token = result.Token;
            return token.ToString();
        }
    }
}
