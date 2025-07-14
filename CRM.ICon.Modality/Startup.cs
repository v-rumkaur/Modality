using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using CRM.ICon.Modality.Helpers.HttpContext;
using CRM.ICon.Modality.Helpers.Repositories;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Model;
using CRM.ICon.Modality.Services.Omnichannel;
using CRM.ICon.Modality.Services.VDM;
using Microsoft.IdentityModel.S2S.Extensions.AspNetCore;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using CRM.ICon.Modality.Helpers.KeyVaultClient;
using Microsoft.Extensions.Caching.Memory;
using CRM.ICon.Modality.Helpers.KeyvaultClient;
using Microsoft.Identity.ServiceEssentials.Extensions.AspNetCoreMiddleware;
using CRM.ICon.Modality.Helpers.ModalityCosmos;
using CRM.ICon.Modality.Helpers.Cosmos;
using CRM.ICon.Modality.Services.LiveChatSettings;

namespace CRM.ICon.Modality
{
    public class Startup
    {

        private const string KeyVaultBaseAddress = "KeyVaultConfiguration:BaseAddress";
        private const string ManagedIdentityClientId = "KeyVaultConfiguration:ManagedIdentityClientId";
        /// <summary>
        /// Gets the configuration object
        /// </summary>
        public IConfiguration Configuration { get; }
        public Startup(IWebHostEnvironment env)
        {

            var builder = new ConfigurationBuilder()
          .SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
          .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
          .AddEnvironmentVariables();

            var configuration = builder.Build();
            builder.AddAzureKeyVault(new SecretClient(new Uri(configuration[KeyVaultBaseAddress]!), new ManagedIdentityCredential(configuration[ManagedIdentityClientId])), new KeyVaultSecretManager());
            this.Configuration = builder.Build();

        }

        /// <summary>
        /// This method is to configure the application
        /// </summary>
        /// <param name="app">Application builder object</param>
        /// <param name="env">Hosting environment object</param>
        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment() || env.IsEnvironment("ppe"))
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection()
                .UseDefaultFiles()
                .UseStaticFiles()
                .UseRouting()
                .UseAuthentication()
                .UseAuthorization()
                .UseMise()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    // Add endpoint for checking the health of this application.
                    // Returns 200 (OK) if the app is healthy, 503 (Service Unavailable) otherwise.
                    endpoints.MapHealthChecks("/health");
                });
        }

        /// <summary>
        /// This method is to configure services
        /// </summary>
        /// <param name="services">Services collection object</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<VDMConfiguration>(this.Configuration.GetSection("VDMConfiguration"));
            services.Configure<AzureAdConfiguration>(this.Configuration.GetSection("AzureAdConfiguration"));
            services.Configure<OmnichannelConfiguration>(this.Configuration.GetSection("OmnichannelConfiguration"));
            services.Configure<OmnichannelEUConfiguration>(this.Configuration.GetSection("OmnichannelEUConfiguration"));
            services.Configure<Dictionary<string, WidgetDetails>>(this.Configuration.GetSection("WidgetDetails"));
            services.Configure<Dictionary<string, string>>(this.Configuration.GetSection("SkillCharacteristicConfiguration"));
            services.Configure<Configuration.TelemetryConfiguration>(Configuration.GetSection(Constants.TelemetryConfiguration));
            services.Configure<Configuration.KeyVaultConfiguration>(Configuration.GetSection(Constants.KeyVaultConfiguration));
            services.Configure<Dictionary<string, WorkstreamDetails>>(this.Configuration.GetSection("WorkstreamConfiguration"));
            services.Configure<CosmosDbConfiguration>(this.Configuration.GetSection("CosmosDbConfiguration"));
            services.Configure<ModalityCosmosDbConfiguration>(this.Configuration.GetSection("ModalityCosmosDbConfiguration"));

            // Initialize Telemetry
            var applicationInsightsServiceOptions = new Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions();
            applicationInsightsServiceOptions.ConnectionString = Configuration.GetSection(Constants.TelemetryConfiguration).GetValue<string>(Constants.ApplicationInsightsConnectionString);
            services.AddApplicationInsightsTelemetry(applicationInsightsServiceOptions);
            services.AddHealthChecks();

            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keeps PascalCase
            }); ;
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddMemoryCache();

            var aadConfiguration = this.Configuration.GetSection("AzureAdConfiguration").Get<AzureAdConfiguration>();
            var authority = $"{aadConfiguration.Instance}{aadConfiguration.TenantId}";

            if (aadConfiguration.OAuthVersion != null)
            {
                authority = $"{authority}/{aadConfiguration.OAuthVersion}";
            }

            services.AddAuthentication(S2SAuthenticationDefaults.AuthenticationScheme)
                    .AddMiseWithDefaultModules(Configuration, authenticationSectionName: "AzureAdConfiguration");

            services.AddSingleton<AuthTokenClient>();


            services.AddHttpClient<IVDMService, VDMService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
                .ConfigureServiceAuthHandler<VDMConfiguration>((options) => new ServiceAuthHandlerParams
                {
                    Resource = options.Resource,
                    TenantId = options.TenantId,

                });

            services.AddHttpClient<IOmnichannelService, OmnichannelService>().ConfigureServiceAuthHandler<OmnichannelConfiguration>((options) => new ServiceAuthHandlerParams
            {
                Resource = options.Resource,
                TenantId = options.TenantId,
                OrgId = options.OrgId,
                DFMTenantId = options.DFMTenantId
            });

            services.AddHttpClient<IOmnichannelEUService, OmnichannelEUService>().ConfigureServiceAuthHandler<OmnichannelEUConfiguration>((options) => new ServiceAuthHandlerParams
            {
                Resource = options.Resource,
                TenantId = options.TenantId,
                OrgId = options.OrgId,
                DFMTenantId = options.DFMTenantId
            });

            services.AddSingleton<ITelemetryService, TelemetryService>();
            services.AddSingleton<ITelemetryRepository, TelemetryRepository>();
            services.AddSingleton<ITelemetryProvider, ApplicationInsightsLogProvider>();
            services.AddSingleton<IHttpContextHandler, HttpContextHandler>();
            services.AddSingleton<IKeyVaultClient, KeyVaultClient>();
            services.AddSingleton<ICosmosDbClient, CosmosDbClient>();
            services.AddSingleton<IModalityCosmosDbClient, ModalityCosmosDbClient>();
            services.AddScoped<ILiveChatSettingsService, LiveChatSettingsService>();

        }
    }
}
