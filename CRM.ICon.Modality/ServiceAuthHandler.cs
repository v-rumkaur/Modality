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
        private readonly string vdmAppRegistrationId;
        private readonly string authMethod;

        public ServiceAuthHandler(AuthTokenClient tokenClient, ITelemetryService telemetryService, string clientId, string fpaClientId, string managedIdentityClientId, string resource, string tenantId, string OrgId, string certificateSubjectName, string dfmTenantId, bool useCertificateAuth, string vdmAppRegistrationId = "", string authMethod = "Certificate")
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
            this.vdmAppRegistrationId = vdmAppRegistrationId;
            this.authMethod = authMethod;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["TENANT_DEBUG_UseCertificateAuth"] = useCertificateAuth.ToString();
            logProperties["TENANT_DEBUG_AuthMethod"] = authMethod;
            logProperties["TENANT_DEBUG_TargetHost"] = request.RequestUri?.Host ?? "Unknown";
            logProperties["TENANT_DEBUG_RequestPath"] = request.RequestUri?.AbsolutePath ?? "";
            logProperties["TENANT_DEBUG_TargetTenant"] = tenantId;
            logProperties["TENANT_DEBUG_Resource"] = resource;
            
            string token = "";
            try
            {
                if (authMethod == "ClientAssertion" && !string.IsNullOrEmpty(vdmAppRegistrationId))
                {
                    // VDM calls using ClientAssertionCredential with cross-tenant authentication
                    this.telemetryService.LogTrace<ServiceAuthHandler>("Using ClientAssertionCredential for VDM cross-tenant call", logProperties);
                    logProperties["VDM_APP_REGISTRATION_ID"] = vdmAppRegistrationId;
                    logProperties["VDM_MANAGED_IDENTITY_ID"] = managedIdentityClientId;
                    
                    // Log all parameters for debugging
                    this.telemetryService.LogTrace<ServiceAuthHandler>($"ServiceAuthHandler DEBUG - Calling GetVDMTokenAsync with TenantId: {tenantId}, AppRegId: {vdmAppRegistrationId}, ManagedIdentityId: {managedIdentityClientId}, Resource: {resource}", logProperties);
                    
                    token = await tokenClient.GetVDMTokenAsync(tenantId, vdmAppRegistrationId, managedIdentityClientId, resource);
                    
                    // Log the token received
                    logProperties["ServiceAuthHandler_RECEIVED_TOKEN"] = token;
                    this.telemetryService.LogTrace<ServiceAuthHandler>($"ServiceAuthHandler DEBUG - Received token: {token}", logProperties);
                }
                else if (useCertificateAuth)
                {
                    // Omnichannel calls use certificate with FPA client ID
                    this.telemetryService.LogTrace<ServiceAuthHandler>("Using certificate authentication for cross-tenant call", logProperties);
                    
                    token = await tokenClient.GetTokenWithCertAsync(fpaClientId, certificateSubjectName, resource, tenantId);
                    if (!string.IsNullOrEmpty(OrgId))
                    {
                        request.Headers.Add("OrganizationId", OrgId);
                        request.Headers.Add("x-ms-organizationid", OrgId);
                        logProperties["TENANT_DEBUG_OrgId"] = OrgId;
                    }
                    if (!string.IsNullOrEmpty(dfmTenantId))
                    {
                        request.Headers.Add("TenantId", dfmTenantId);
                        logProperties["TENANT_DEBUG_DFMTenantId"] = dfmTenantId;
                    }
                }
                else
                {
                    // Managed identity authentication (tends to produce v1.0 tokens from home tenant)
                    logProperties["TENANT_DEBUG_AuthMethod"] = "ManagedIdentity";
                    logProperties["TENANT_DEBUG_ClientId"] = clientId;
                    logProperties["TENANT_DEBUG_ManagedIdentityClientId"] = managedIdentityClientId;
                    
                    this.telemetryService.LogTrace<ServiceAuthHandler>("TENANT_DEBUG: Using managed identity authentication for cross-tenant call", logProperties);
                    
                    token = await tokenClient.GetToken(clientId, managedIdentityClientId, resource, tenantId);
                    
                    // Log full token for debugging (REMOVE AFTER TROUBLESHOOTING!)
                    if (!string.IsNullOrEmpty(token))
                    {
                        logProperties["FullAccessToken"] = token;
                    }
                }

                request.Headers.Add("Authorization", $"Bearer {token}");
                
                // Log the final authorization header value for debugging
                logProperties["FINAL_AUTHORIZATION_HEADER"] = $"Bearer {token}";
                this.telemetryService.LogTrace<ServiceAuthHandler>("Successfully added authentication to request", logProperties);
                this.telemetryService.LogTrace<ServiceAuthHandler>($"ServiceAuthHandler DEBUG - Added Authorization header: Bearer {token}", logProperties);
                
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
        public string? VDMAppRegistrationId { get; set; } = ""; // VDM app registration ID for ClientAssertion
        public string? AuthMethod { get; set; } = "Certificate"; // Certificate or ClientAssertion
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
                    p.UseCertificateAuth,
                    p.VDMAppRegistrationId ?? "",
                    p.AuthMethod ?? "Certificate"
                );
            });
        }
    }
}