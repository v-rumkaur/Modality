using CRM.ICon.Modality.Helpers;
using CRM.ICon.Modality.Model;
using CRM.ICon.Modality.Services.Modality;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace CRM.ICon.Modality.Services.Modality
{
    public class ModalitiesService : IModalitiesService
    {
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
        private const string ContactUsPath = "/contact/";
        private const string HoopsPath = "/contact/hoops/";
        private const string VNextPath = "/v1.0/";
        private const string DefaultFallbackLocale = "en-us";
        private const string Unauth = "unauth";
        private const int FormnameIndex = 3;
        private const string Chat = "chat";
        private const string OCChat = "occhat";
        private const string Callback = "callback";
        private const string ScheduleCallback = "schedulecallback";
        private const string TollFree = "tollfree";
        private const string InstantAnswer = "instantanswer";
        private static readonly Dictionary<string, string> metaTagMap = new Dictionary<string, string>
        {
            {"schedule-call", "schedulecallback"},
            {"phone", "tollfree"},
            {"care-locator", "carelocator"},
            {"device-repair", "devicerepair"},
            {"complaint-form", "complaintform"},
            {"services-subscriptions", "servicesandsubscriptions"},
            {"payment-options", "paymentoptions"},
            {"orders", "findpurchaseortransaction"},
            {"reset-password", "resetpassword"},
            {"device-custom", "contactoemsupport"},
            {"device-custom,anon-oem", "contactanonymousoem"},
            {"device-custom,ms-device", "contactmsoem"},
            {"instant-answer", "instantanswer"},
            {"schedule-appointment", "scheduleappointment"}
        };

        private readonly Dictionary<string, string> _fallbackLocales;
        private readonly ICompassService _compassService;
        private readonly IPartnerConfiguration _partnerConfig;
        private readonly IVNextConfiguration _vNextConfig;
        private readonly ISllLogger _sllLogger;
        private readonly OCQueueAvailabilityServiceConfiguration _ocQueueAvailabilityServiceConfiguration;       
        private readonly IConfigurationMappingProvider _configurationMappingDocDbProvider;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ModalitiesService(
            ICompassService compassService,
            IPartnerConfiguration partnerConfig,
            IVNextConfiguration vNextConfig,
            ISllLogger sllLogger,
            IOptions<OCQueueAvailabilityServiceConfiguration> ocQueueAvailabilityServiceConfiguration,
            IConfigurationMappingProvider configurationMappingProvider,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _compassService = compassService;
            _fallbackLocales = compassService.GetFallbackLocales();
            _partnerConfig = partnerConfig;
            _vNextConfig = vNextConfig;
            _sllLogger = sllLogger;
            _ocQueueAvailabilityServiceConfiguration = ocQueueAvailabilityServiceConfiguration.Value;
            _configurationMappingDocDbProvider = configurationMappingProvider;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<ModalitiesV2> GetModalitiesAsync(
            string product, string issue, string partnerId, string platform,
            string language, string country, string mode,
            bool preview, bool disability, bool fallback, string host, bool isVNext, bool isTest)
        {
            var locale = $"{language}-{country}";
            var fullProductPath = string.Format(_ocQueueAvailabilityServiceConfiguration.BueUrl, product);

            PublishContent compassContent;
            string requestId = null;
            try
            {
                var token = await GenerateToken();
                string isPreview = preview.ToString().ToLowerInvariant();
                var uriBuilder = new UriBuilder(fullProductPath);
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                query["$filter"] = $"Locale eq '{locale}' and IsPreview eq {isPreview}";
                uriBuilder.Query = query.ToString();
                var message = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                requestId = Guid.NewGuid().ToString();
                message.Headers.TryAddWithoutValidation("X-RequesT-ID", requestId);
                message.Headers.TryAddWithoutValidation("X-Application-ID", "c1413fd6-4a0d-4cbc-83d1-79bd0898cb02");
                message.Headers.TryAddWithoutValidation("X-Tenant-ID", _ocQueueAvailabilityServiceConfiguration.BueTenantId);

                compassContent = await _compassService.GetCompassContentAsync(_httpClient, message, fullProductPath, locale, preview);
                _sllLogger.WriteInformationalTelemetry("X-RequesT-ID", requestId);
            }
            catch (Exception ex)
            {
                _sllLogger.WriteErrorTelemetry("GetModalities", $"Get Modalities call for ConfigHub Failed Exception: {ex} RequestId: {requestId}");
                throw;
            }

            var modalitiesList = await ConstructModalities(compassContent, product, issue, locale, partnerId, mode, preview, disability, host, isVNext, isTest);
            return new ModalitiesV2(modalitiesList);
        }

        public async Task<IEnumerable<Dictionary<string, string>>> GetModalities(
            string product, string issue, string partnerId, string platform, bool preview,
            string mode, bool disability, string locale, string host, bool isVNext, bool isTest)
        {
            var fullProductPath = string.Format(_ocQueueAvailabilityServiceConfiguration.BueUrl, product);

            PublishContent compassContent;
            string requestId = null;
            try
            {
                var token = await GenerateToken();
                string isPreview = preview.ToString().ToLowerInvariant();
                var uriBuilder = new UriBuilder(fullProductPath);
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                query["$filter"] = $"Locale eq '{locale}' and IsPreview eq {isPreview}";
                uriBuilder.Query = query.ToString();
                var message = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                requestId = Guid.NewGuid().ToString();
                message.Headers.TryAddWithoutValidation("X-RequesT-ID", requestId);
                message.Headers.TryAddWithoutValidation("X-Application-ID", "c1413fd6-4a0d-4cbc-83d1-79bd0898cb02");
                message.Headers.TryAddWithoutValidation("X-Tenant-ID", _ocQueueAvailabilityServiceConfiguration.BueTenantId);

                compassContent = await _compassService.GetCompassContentAsync(_httpClient, message, fullProductPath, locale, preview);
                _sllLogger.WriteInformationalTelemetry("X-RequesT-ID", requestId);
            }
            catch (Exception ex)
            {
                _sllLogger.WriteErrorTelemetry("GetModalities", $"Get Modalities call for ConfigHub Failed Exception: {ex} RequestId: {requestId}");
                throw;
            }

            var modalities = await ConstructModalities(compassContent, product, issue, locale, partnerId, mode, preview, disability, host, isVNext, isTest);
            return modalities;
        }

        public string GetFallbackLocale(string locale)
        {
            return _fallbackLocales.ContainsKey(locale) ? _fallbackLocales[locale] : DefaultFallbackLocale;
        }

        protected async Task<List<Dictionary<string, string>>> ConstructModalities(PublishContent compassContent, string product, string issue, string locale, string partnerId, string mode, bool preview, bool disability, string host, bool isVNext, bool isTest)
        {
            var modalities = new List<Dictionary<string, string>>();
            if (compassContent == null) return modalities;

            var productIssueKey = $"{product} - {issue}";
            foreach (var compassIssue in compassContent[MenuPagesKey].Items)
            {
                if (compassIssue[NameKey].Value.Equals(productIssueKey, StringComparison.OrdinalIgnoreCase))
                {
                    var compassIssueLinks = compassIssue[LinksKey].Items;
                    foreach (var link in compassIssueLinks)
                    {
                        var translatedName = TranslateName(link[MetaTagsKey].Value);
                        if (!string.IsNullOrWhiteSpace(translatedName))
                        {
                            var result = await GetModalities(link, translatedName, product, issue, locale, partnerId, mode, disability, preview, host, isVNext, isTest);
                            if (result.Count > 0) modalities.Add(result);
                        }
                    }
                }
            }
            return modalities;
        }

        protected async Task<Dictionary<string, string>> GetModalities(PublishingField link, string name, string product, string issue, string locale, string partnerId, string mode, bool disability, bool preview, string host, bool isVNext, bool isTest)
        {
            var assistedSupportUri = string.IsNullOrWhiteSpace(host)
                ? _partnerConfig.GetUri()
                : new UriBuilder(_partnerConfig.Scheme, host, _partnerConfig.Port).Uri;

            var modalities = new Dictionary<string, string>();
            var circuitBreakerFlag = await GetCircuitBreakerFlag(product, issue, locale, name);
            if (circuitBreakerFlag) return modalities;

            modalities.Add(ModalityField.Name, name);

            var uriString = await GetUriString(link, locale);

            var hoopsUri = uriString;
            if (Regex.IsMatch(hoopsUri, Form, RegexOptions.IgnoreCase))
            {
                var fields = hoopsUri.Split('/');
                if (fields.Length > 5)
                {
                    var subjectIdPart = fields[2];
                    var modalityPart = fields[4];
                    hoopsUri = $"/{modalityPart}/{subjectIdPart}/";
                }
            }

            bool isExternalLink;
            bool.TryParse(link[ExternalLinkKey].Value, out isExternalLink);

            if (isExternalLink)
            {
                if (name.Contains("message-gamer") && partnerId.Equals("e4381e95-b3fb-48f5-be50-e59142564496"))
                {
                    modalities.Add(ModalityField.Link, HttpUtility.HtmlDecode(uriString.Replace("cbc3e761-f603-4a97-9113-2b09298272db", "e4381e95-b3fb-48f5-be50-e59142564496")));
                }
                else
                {
                    modalities.Add(ModalityField.Link, HttpUtility.HtmlDecode(uriString));
                }

                if (name.Equals(OCChat))
                {
                    var splits = uriString.Split('/');
                    int subjectId;
                    int.TryParse(splits[splits.Length - 1], out subjectId);
                    var localeParts = locale.Split('-');
                    var language = localeParts[0];
                    var country = localeParts[1];

                    modalities.Add(ModalityField.OpenNowLink, CreateLink("api/hoops/open/", subjectId, Chat, language, country, partnerId, mode, disability, preview, assistedSupportUri).ToString());
                    modalities.Add(ModalityField.HoopsLink, CreateLink($"{locale}{HoopsPath}{Chat}/{subjectId}/", null, null, null, null, partnerId, mode, disability, preview, assistedSupportUri).ToString());
                    modalities.Add(ModalityField.QueueAvailability, CreateLink("api/queue/availability/", subjectId, product, issue, null, language, country, partnerId, mode, disability, preview, assistedSupportUri).ToString());
                }
            }
            else if (name.Equals(Chat) || name.Equals(Callback))
            {
                var subjectRegex = new Regex("(?!\\/)\\d+?(?=\\/|$)");
                var match = subjectRegex.Match(uriString);
                int subjectId;
                int.TryParse(match.Value, out subjectId);

                if (isVNext)
                {
                    string formname = null;
                    string authtype = null;
                    if (uriString.Contains(Form))
                    {
                        formname = uriString.Split('/')[FormnameIndex];
                        authtype = Unauth;
                    }

                    Uri vNextUri = new UriBuilder(_vNextConfig.Scheme, isTest ? _vNextConfig.TestHost : _vNextConfig.Host, _vNextConfig.Port).Uri;
                    modalities.Add(ModalityField.Link,
                        CreateLink($"{locale}{VNextPath}", subjectId, null, null, name, authtype, formname, null, null, partnerId, mode, disability, preview, vNextUri, true).ToString());
                }
                else
                {
                    modalities.Add(ModalityField.Link,
                        CreateLink($"{locale}{ContactUsPath}{uriString.TrimStart('/')}", null, null, null, null, partnerId, mode, disability, preview, assistedSupportUri).ToString());
                }

                var localeParts = locale.Split('-');
                var language = localeParts[0];
                var country = localeParts[1];

                if (localeParts.Length == 3)
                {
                    language = $"{language}-{localeParts[1]}";
                    country = localeParts[2];
                }

                modalities.Add(ModalityField.OpenNowLink, CreateLink("api/hoops/open/", subjectId, name, language, country, partnerId, mode, disability, preview, assistedSupportUri).ToString());
                modalities.Add(ModalityField.HoopsLink, CreateLink($"{locale}{HoopsPath}{hoopsUri.TrimStart('/')}", null, null, null, null, partnerId, mode, disability, preview, assistedSupportUri).ToString());
                modalities.Add(ModalityField.QueueLength, CreateLink("api/queue/length/", subjectId, null, language, country, partnerId, mode, disability, preview, assistedSupportUri).ToString());
                modalities.Add(ModalityField.WaitTime, CreateLink("api/queue/waittime/", subjectId, name, language, country, partnerId, mode, disability, preview, assistedSupportUri).ToString());
            }
            else if (name.Equals(TollFree))
            {
                var subjectRegex = new Regex("(?!\\/)\\d+?(?=\\/|$)");
                var match = subjectRegex.Match(uriString);
                int subjectId;
                int.TryParse(match.Value, out subjectId);
                var localeParts = locale.Split('-');
                var language = localeParts[0];
                var country = localeParts[1];

                modalities.Add(ModalityField.OpenNowLink, CreateLink("api/hoops/open/", subjectId, name, language, country, partnerId, mode, disability, preview, assistedSupportUri).ToString());
                modalities.Add(ModalityField.HoopsLink, CreateLink($"{locale}{HoopsPath}{hoopsUri.TrimStart('/')}", null, null, null, null, partnerId, mode, disability, preview, assistedSupportUri).ToString());
                modalities.Add(ModalityField.Phone, link[DescriptionKey].Value);
            }
            else if (name.Equals(InstantAnswer))
            {
                modalities.Add(ModalityField.ArticleId, link[LinkKey][UrlKey].Value);
            }
            else
            {
                modalities.Add(ModalityField.Link, CreateLink($"{locale}{ContactUsPath}{uriString.TrimStart('/')}", null, null, null, null, partnerId, mode, disability, preview, assistedSupportUri).ToString());
            }

            return modalities;
        }

        protected virtual async Task<string> GetUriString(PublishingField link, string locale)
        {
            var uriField = link[LinkKey][UrlKey];
            var uri = string.Empty;
            if (uriField?.Value != null)
            {
                uri = uriField.Value.Replace(LocalePlaceholder, locale.ToLowerInvariant());
            }
            return await Task.FromResult(uri);
        }

        protected string TranslateName(string compassTag)
        {
            var metaTag = compassTag.ToLowerInvariant();
            return metaTagMap.ContainsKey(metaTag) ? metaTagMap[metaTag] : metaTag;
        }

        private Uri CreateLink(string linkPath, int? subject, string modality, string language, string country, string partnerId, string mode, bool? disability, bool? preview, Uri host)
        {
            return CreateLink(linkPath, subject, null, null, modality, null, null, language, country, partnerId, mode, disability, preview, host, false);
        }

        private Uri CreateLink(string linkPath, int? subject, string product, string issue, string modality, string language, string country, string partnerId, string mode, bool? disability, bool? preview, Uri host)
        {
            return CreateLink(linkPath, subject, product, issue, modality, null, null, language, country, partnerId, mode, disability, preview, host, false);
        }

        private Uri CreateLink(string linkPath, int? subject, string product, string issue, string modality, string authtype, string formname, string language, string country, string partnerId, string mode, bool? disability, bool? preview, Uri host, bool isLink)
        {
            var qb = CreateQueryStringBuilder(modality, subject, product, issue, authtype, formname, language, country, partnerId, mode, disability, preview, isLink);
            var builder = CreateUriBuilder(host, linkPath);
            return qb.AddToUri(builder.Uri);
        }

        private UriQueryBuilder CreateQueryStringBuilder(
            string modalityName, int? subjectId, string product, string issue, string authtype, string formname,
            string language, string country, string partnerId, string mode, bool? disability, bool? preview, bool isLink)
        {
            var qb = new UriQueryBuilder();
            AddToQueryBuilder(qb, "modality", modalityName);
            AddToQueryBuilder(qb, isLink ? "subjectid" : "subject", subjectId?.ToString(CultureInfo.InvariantCulture));
            AddToQueryBuilder(qb, "product", product);
            AddToQueryBuilder(qb, "issue", issue);
            AddToQueryBuilder(qb, "authtype", authtype);
            AddToQueryBuilder(qb, "formname", formname);
            AddToQueryBuilder(qb, "language", language);
            AddToQueryBuilder(qb, "country", country);
            AddToQueryBuilder(qb, "disability", disability?.ToString().ToLowerInvariant());
            AddToQueryBuilder(qb, "mode", mode);
            AddToQueryBuilder(qb, "partnerId", partnerId);

            if (preview != null && preview.Value)
            {
                AddToQueryBuilder(qb, "preview", "true");
            }
            return qb;
        }

        private static void AddToQueryBuilder(UriQueryBuilder qb, string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                qb.Add(name, value);
            }
        }

        private static UriBuilder CreateUriBuilder(Uri uri, string path = null)
        {
            return new UriBuilder(uri.Scheme, uri.Host, uri.Port, path);
        }

        public async Task<bool> GetCircuitBreakerFlag(string product, string issue, string locale, string name)
        {
            string id = "circuitbreaker";
            if (_configurationMappingDocDbProvider != null)
            {
                var result = await _configurationMappingDocDbProvider.ExecuteQueryAsync<CircuitBreaker>(id, _sllLogger);
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
                            foreach (var modality in modalities)
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

        public async Task<CircuitBreaker> UpdateCircuitBreakerAsync(CircuitBreaker circuitBreaker)
        {
            string id = "circuitbreaker";
            if (_configurationMappingDocDbProvider != null)
            {
                var result = await _configurationMappingDocDbProvider.ExecuteUpdateQueryAsync<CircuitBreaker>(circuitBreaker, _sllLogger, id);
                return result;
            }
            return null;
        }

        private async Task<string> GenerateToken()
        {
            var managedIdentityId = _configuration.GetSection("KeyvaultConfigSectionName")["ManagedIdentityId"];
            var credential = new Azure.Identity.ManagedIdentityCredential(managedIdentityId);
            var result = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { _ocQueueAvailabilityServiceConfiguration.BueScopeId })).ConfigureAwait(false);
            var token = result.Token;
            return token.ToString();
        }
    }
}