using CRM.ICon.Modality.Helpers;
using System;
using System.Collections.Generic;

namespace CRM.ICon.Modality.Services.Modality
{
    public class OCQueueAvailabilityServiceConfiguration
    {
        private const string ConfigSectionName = "OCQueueAvailabilityServiceConfig";
        private const string KVConfigSectionName = "KeyvaultConfig";
        private const string AppId = "SupportChannels.AppId";
        private const string SubjectName = "SupportChannels.Certificate";
        private const string TenantId = "SupportChannels.TenantId";
        private const string ResourceId = "SupportChannels.ResourceId";
        private const string DocdbUrl = "Docdb.Url";
        private const string DocdbPreferredLoction = "Docdb.PreferredLocation";
        private const string DocdbDatabase = "Docdb.Database";
        private const string DocdbContainer = "Docdb.Container";
        private const string Url = "Vnext.QueueStatusUrl";
        private const string MsTenant = "Vnext.Tenant";
        private const string VAppId = "Vnext.AppId";
        private const string BueTenant = "Bue.Tenant";
        private const string BueEndpoint = "Bue.Endpoint";
        private const string BueScope = "Bue.Scope";
        private const string ManagedIdentityId = "ManagedIdentityId";
        private const string SupportChannelsFirstPartyApp = "SupportChannelsFirstParty.AppId";
        private const string SupportChannelsFirstPartyAppTenant = "SupportChannelsFirstParty.Tenant";
        private const string AADRegion = "AAD.Region";

        /// <summary>
        /// Initializes a new instance of the <see cref="OCQueueAvailabilityServiceConfiguration"/> class.
        /// </summary>
        public OCQueueAvailabilityServiceConfiguration(string tenant, string resource, string app, string certificate, Dictionary<string, string> ocEndpoints, string docdbUrl, string docdbPreferredLocation, string docdbDatabase, string docdbContainer, string vnextUrl, string vnextTenant, string vnextAppId, string CRBSOCPSubscriptionKey, string bueEndpoint, string bueTenant, string bueScope, string managedIdentityId, string firstPartyAppId, string firstpartyTenant, string aadregion)
        {
            this.Tenant = tenant;
            this.Resource = resource;
            this.App = app;
            this.Certificate = certificate;
            this.OCEndpoints = ocEndpoints;
            this.Docdb = docdbUrl;
            this.DocdbPreferredLocation = docdbPreferredLocation;
            this.DocdbData = docdbDatabase;
            this.DocdbTable = docdbContainer;
            this.QueueStatusUrl = vnextUrl;
            this.VnextTenant = vnextTenant;
            this.VnextAppId = vnextAppId;
            this.CRBSOCPSubscriptionKey = CRBSOCPSubscriptionKey;
            this.BueUrl = bueEndpoint;
            this.BueTenantId = bueTenant;
            this.BueScopeId = bueScope;
            this.ManagedIdentityClientId = managedIdentityId;
            this.SupportChannelsFirstPartyAppId = firstPartyAppId;
            this.SupportChannelsFirstPartyTenant = firstpartyTenant;
            this.AAdRegion = aadregion;
        }

        public OCQueueAvailabilityServiceConfiguration()
        {
            // This enables config binding. You can leave it empty.
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="OCQueueAvailabilityServiceConfiguration"/> class.
        /// </summary>
        /// <param name="configuration"></param>
        public OCQueueAvailabilityServiceConfiguration(IConfiguration configuration)
            : this(
                  configuration.GetStringValue(ConfigSectionName, TenantId),
                  configuration.GetStringValue(ConfigSectionName, ResourceId),
                  configuration.GetStringValue(ConfigSectionName, AppId),
                  configuration.GetStringValue(ConfigSectionName, SubjectName),
                  getOCEndpoints(),
                  configuration.GetStringValue(ConfigSectionName, DocdbUrl),
                  configuration.GetStringValue(ConfigSectionName, DocdbPreferredLoction),
                  configuration.GetStringValue(ConfigSectionName, DocdbDatabase),
                  configuration.GetStringValue(ConfigSectionName, DocdbContainer),
                  configuration.GetStringValue(ConfigSectionName, Url),
                  configuration.GetStringValue(ConfigSectionName, MsTenant),
                  configuration.GetStringValue(ConfigSectionName, VAppId),
                  ConfigHelper.GetConfigValue(Constants.Common.CRBSOCPSubscriptionValue, configuration),
                  configuration.GetStringValue(ConfigSectionName, BueEndpoint),
                  configuration.GetStringValue(ConfigSectionName, BueTenant),
                  configuration.GetStringValue(ConfigSectionName, BueScope),
                  configuration.GetStringValue(KVConfigSectionName, ManagedIdentityId),
                  configuration.GetStringValue(ConfigSectionName, SupportChannelsFirstPartyApp),
                  configuration.GetStringValue(ConfigSectionName, SupportChannelsFirstPartyAppTenant),
                  configuration.GetStringValue(ConfigSectionName, AADRegion))
        {
        }

        /// <summary>
        /// Gets or sets the resource of OC Queue Availability Service
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        /// Gets or sets the tenant of OC Queue Availability Service
        /// </summary>
        public string Tenant { get; set; }

        /// <summary>
        /// Gets or sets the certificate of OC Queue Availability Service
        /// </summary>
        public string Certificate { get; set; }

        /// <summary>
        /// Gets or sets the app of OC Queue Availability Service
        /// </summary>
        public string App { get; set; }
        public Dictionary<string, string> OCEndpoints { get; set; }

        /// <summary>
        /// Gets or sets the docdb url of Cantilever Queue Availability Service
        /// </summary>
        public string Docdb { get; set; }

        /// <summary>
        /// Gets or sets the docdb preferred location  of Cantilever Queue Availability Service
        /// </summary>
        public string DocdbPreferredLocation { get; set; }

        /// <summary>
        /// Gets or sets the docdb database of Cantilever Queue Availability Service
        /// </summary>
        public string DocdbData { get; set; }

        /// <summary>
        /// Gets or sets the Url of Vnext Queue Service
        /// </summary>
        public string QueueStatusUrl { get; set; }

        /// <summary>
        /// Gets or sets the Path of Cantilever Channels Service
        /// </summary>
        public string VnextTenant { get; set; }

        /// <summary>
        /// Gets or sets the Url of Vnext Queue Service
        /// </summary>
        public string VnextAppId { get; set; }

        /// <summary>
        /// Gets the CRBSOCPSubscriptionKey
        /// <summary>
        public string CRBSOCPSubscriptionKey { get; set; }

        /// <summary>
        /// Gets or sets the docdb container  of Cantilever Queue Availability Service
        /// </summary>
        public string DocdbTable { get; set; }

        /// <summary>
        /// Gets or sets the BUE tenant Id
        /// </summary>
        public string BueTenantId { get; set; }

        /// <summary>
        /// Gets or sets the BUE tenant Id
        /// </summary>
        public string BueScopeId { get; set; }

        /// <summary>
        /// Gets or sets the BUE tenant Id
        /// </summary>
        public string BueUrl { get; set; }

        /// <summary>
        /// Gets the ManagedIdentityClientId
        /// <summary>
        public string ManagedIdentityClientId { get; set; }

        /// <summary>
        /// Gets or sets the SupportChannelsFirstPartyAppId
        /// <summary>
        public string SupportChannelsFirstPartyAppId { get; set; }

        /// <summary>
        /// Gets or sets the SupportChannelsFirstPartyTenant
        /// <summary>
        public string SupportChannelsFirstPartyTenant { get; set; }

        /// <summary>
        /// Gets or sets the AADRegion
        /// <summary>
        public string AAdRegion { get; set; }

        private static Dictionary<string, string> getOCEndpoints()
        {
            Dictionary<string, string> ocEndpoints = new Dictionary<string, string>();
            ocEndpoints.Add("35f1b5b1-5c93-47d5-84eb-84b547878103", "https://a-35f1b5b1-5c93-47d5-84eb-84b547878103.us.omnichannelengagementhub.com");
            ocEndpoints.Add("35f1b5b1-5c93-47d5-84eb-84b547878103.Tenant", "72f988bf-86f1-41af-91ab-2d7cd011db47");
            ocEndpoints.Add("a8c967f2-d0c0-4bfc-a40f-6c7f603fb3b8", "https://a-a8c967f2-d0c0-4bfc-a40f-6c7f603fb3b8.us.omnichannelengagementhub.com");
            ocEndpoints.Add("a8c967f2-d0c0-4bfc-a40f-6c7f603fb3b8.Tenant", "b4c546a4-7dac-46a6-a7dd-ed822a11efd3");
            ocEndpoints.Add("dfa57d40-f828-4327-b44b-59f1647b1fb5", "https://a-dfa57d40-f828-4327-b44b-59f1647b1fb5.us.omnichannelengagementhub.com");
            ocEndpoints.Add("dfa57d40-f828-4327-b44b-59f1647b1fb5.Tenant", "72f988bf-86f1-41af-91ab-2d7cd011db47");
            ocEndpoints.Add("79c20b4b-277c-458b-8d64-8bedea472d15", "https://a-79c20b4b-277c-458b-8d64-8bedea472d15.us.omnichannelengagementhub.com");
            ocEndpoints.Add("79c20b4b-277c-458b-8d64-8bedea472d15.Tenant", "72f988bf-86f1-41af-91ab-2d7cd011db47");
            return ocEndpoints;
        }
    }
}