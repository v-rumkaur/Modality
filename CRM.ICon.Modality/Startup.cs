using CRM.ICon.Modality.Model;
using CRM.ICon.Modality.Services.Omnichannel;
using CRM.ICon.Modality.Services.VDM;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.S2S.Extensions.AspNetCore;

namespace CRM.ICon.Modality
{
    public class Startup
    {
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

            this.Configuration = builder.Build();

        }

        /// <summary>
        /// This method is to configure the application
        /// </summary>
        /// <param name="app">Application builder object</param>
        /// <param name="env">Hosting environment object</param>
        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwaggerUI();
                app.UseSwagger();
            }
            else
            {
                app.UseHsts();
            }

            app.UseDefaultFiles()
              .UseAuthentication()
              .UseStaticFiles()
              .UseRouting()
              .UseAuthorization()
              .UseEndpoints(endpoints =>
              {
                  endpoints.MapControllers();
              })
              .UseHttpsRedirection();

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
            services.Configure<Dictionary<string, WidgetDetails>>(this.Configuration.GetSection("WidgetDetails"));
            services.Configure<Dictionary<string, string>>(this.Configuration.GetSection("SkillCharacteristicConfiguration"));

            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            var aadConfiguration = services.BuildServiceProvider()!.GetService<IOptions<AzureAdConfiguration>>()!.Value;
#pragma warning restore ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'
            var authority = $"{aadConfiguration.Instance}{aadConfiguration.TenantId}";

            if (aadConfiguration.OAuthVersion != null)
            {
                authority = $"{authority}/{aadConfiguration.OAuthVersion}";
            }

            //services.AddAuthentication(S2SAuthenticationDefaults.AuthenticationScheme)
              //              .AddMiseWithDefaultModules(Configuration, authenticationSectionName: "AzureAdConfiguration");


            services.AddSingleton<AuthTokenClient>();

            services.AddHttpClient<IVDMService, VDMService>().ConfigureServiceAuthHandler<VDMConfiguration>((options) => new ServiceAuthHandlerParams
            {
                Resource = options.Resource,
                TenantId = options.TenantId,
            });

            services.AddHttpClient<IOmnichannelService, OmnichannelService>().ConfigureServiceAuthHandler<OmnichannelConfiguration>((options) => new ServiceAuthHandlerParams
            {
                Resource = options.Resource,
                TenantId = options.TenantId,
                OrgId = options.OrgId,
            });
        }
    }
}
