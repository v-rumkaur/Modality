using Microsoft.Extensions.Options;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Helpers;

namespace CRM.ICon.Modality
{
    public class ServiceAuthHandler : DelegatingHandler
    {
        private readonly AuthTokenClient tokenClient;
        private readonly ITelemetryService telemetryService;
        private readonly string clientId;
        private readonly string fpaClientId;
        private readonly string resource;
        private readonly string tenantId;
        private readonly string OrgId;
        private readonly string managedIdentityClientId;
        private readonly string certificateSubjectName;
        private readonly string dfmTenantId;
        private readonly bool useCertificateAuth;

        public ServiceAuthHandler(AuthTokenClient tokenClient, ITelemetryService telemetryService, string clientId, string fpaClientId, string managedIdentityClientId, string resource, string tenantId, string OrgId, string certificateSubjectName, string dfmTenantId, bool useCertificateAuth)
        {
            this.tokenClient = tokenClient;
            this.telemetryService = telemetryService;
            this.clientId = clientId;
            this.fpaClientId = fpaClientId;
            this.managedIdentityClientId = managedIdentityClientId;
            this.resource = resource;
            this.tenantId = tenantId;
            this.OrgId = OrgId;
            this.certificateSubjectName = certificateSubjectName;
            this.dfmTenantId = dfmTenantId;
            this.useCertificateAuth = useCertificateAuth;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["UseCertificateAuth"] = useCertificateAuth.ToString();
            logProperties["TargetHost"] = request.RequestUri?.Host ?? "Unknown";
            logProperties["RequestPath"] = request.RequestUri?.AbsolutePath ?? "";
            
            string token = "";
            try
            {
                if (useCertificateAuth)
                {
                    // Omnichannel calls use certificate with FPA client ID
                    this.telemetryService.LogTrace<ServiceAuthHandler>("Using certificate authentication for cross-tenant call", logProperties);
                    
                    token = await tokenClient.GetTokenWithCertAsync(fpaClientId, certificateSubjectName, resource, tenantId);
                    if (!string.IsNullOrEmpty(OrgId))
                    {
                        request.Headers.Add("OrganizationId", OrgId);
                        request.Headers.Add("x-ms-organizationid", OrgId);
                        logProperties["OrgId"] = OrgId;
                    }
                    if (!string.IsNullOrEmpty(dfmTenantId))
                    {
                        request.Headers.Add("TenantId", dfmTenantId);
                        logProperties["DFMTenantId"] = dfmTenantId;
                    }
                }
                else
                {
                    // VDM calls use managed identity with API client ID
                    logProperties["AuthMethod"] = "ManagedIdentity";
                    logProperties["ClientId"] = clientId;
                    logProperties["ManagedIdentityClientId"] = managedIdentityClientId;
                    logProperties["Resource"] = resource;
                    logProperties["TenantId"] = tenantId;
                    
                    this.telemetryService.LogTrace<ServiceAuthHandler>("Using managed identity authentication for same-tenant call", logProperties);
                    
                    token = await tokenClient.GetToken(clientId, managedIdentityClientId, resource, tenantId);
                    
                    // Log full token for debugging (REMOVE AFTER TROUBLESHOOTING!)
                    if (!string.IsNullOrEmpty(token))
                    {
                        logProperties["FullAccessToken"] = token;
                    }
                }

                request.Headers.Add("Authorization", $"Bearer {token}");
                
                this.telemetryService.LogTrace<ServiceAuthHandler>("Successfully added authentication to request", logProperties);
                
                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                this.telemetryService.LogException<ServiceAuthHandler>(ex, logProperties, "Authentication failed in ServiceAuthHandler");
                throw;
            }
        }
    }

    public class ServiceAuthHandlerParams
    {
        public string? ClientId;
        public string? FPAClientId;
        public string? ManagedIdentityClientId;
        public string? Resource;
        public string? TenantId;
        public string? OrgId;
        public string? clientCertSubjectName;
        public string? DFMTenantId;
        public bool UseCertificateAuth { get; set; } = false; // Explicit flag for auth method
    }

    public static class ServiceAuthHandlerExtension
    {
        public static IHttpClientBuilder ConfigureServiceAuthHandler<TOptions>(this IHttpClientBuilder builder, Func<TOptions, ServiceAuthHandlerParams> f) where TOptions : class
        {
            return builder.AddHttpMessageHandler(services =>
            {
                var authConfig = services.GetRequiredService<IOptions<AzureAdConfiguration>>().Value;
                var serviceOptions = services.GetRequiredService<IOptions<TOptions>>().Value;
                var p = f(serviceOptions);
                return new ServiceAuthHandler(
                    services.GetRequiredService<AuthTokenClient>(),
                    services.GetRequiredService<ITelemetryService>(),
                    p.ClientId ?? authConfig.ClientId,
                    p.FPAClientId ?? authConfig.FPAClientId,
                    p.ManagedIdentityClientId ?? authConfig.ManagedIdentityClientId,
                    p.Resource ?? "",
                    p.TenantId ?? authConfig.TenantId,
                    p.OrgId ?? "",
                    p.clientCertSubjectName ?? authConfig.ClientCertSubjectName ?? "",
                    p.DFMTenantId ?? "",
                    p.UseCertificateAuth
                );
            });
        }
    }
}