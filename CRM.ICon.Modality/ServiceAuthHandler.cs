using Microsoft.Extensions.Options;

namespace CRM.ICon.Modality
{
    public class ServiceAuthHandler : DelegatingHandler
    {
        private readonly AuthTokenClient tokenClient;
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string resource;
        private readonly string tenantId;
        private readonly string OrgId;
        private readonly string managedIdentityClientId;
        private readonly string certificateSubjectName;
        private readonly string dfmTenantId;

        public ServiceAuthHandler(AuthTokenClient tokenClient, string clientId, string managedIdentityClientId, string resource, string tenantId, string OrgId, string certificateSubjectName, string dfmTenantId)
        {
            this.tokenClient = tokenClient;
            this.clientId = clientId;
            this.managedIdentityClientId = managedIdentityClientId;
            this.resource = resource;
            this.tenantId = tenantId;
            this.OrgId = OrgId;
            this.certificateSubjectName = certificateSubjectName;
            this.dfmTenantId = dfmTenantId;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string token = "";
            if (string.IsNullOrEmpty(OrgId))
            {
                token = await tokenClient.GetToken(clientId, managedIdentityClientId, resource, tenantId);
            }
            else
            {
                token = await tokenClient.GetTokenWithCertAsync(clientId, certificateSubjectName, resource, tenantId);
                request.Headers.Add("OrganizationId", OrgId);
                request.Headers.Add("TenantId", dfmTenantId);
            }
       
            request.Headers.Add("Authorization", $"Bearer {token}");
            
            return await base.SendAsync(request, cancellationToken);
        }
    }

    public record ServiceAuthHandlerParams
    {
        public string? ClientId;
        public string? ManagedIdentityClientId;
        public string? TenantId;
        public string? OrgId;
        public string? Resource;
        public string? clientCertSubjectName;
        public string? DFMTenantId;
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
                    p.ClientId ?? authConfig.ClientId,
                    p.ManagedIdentityClientId ?? authConfig.ManagedIdentityClientId,
                    p.Resource ?? "",
                    p.TenantId ?? authConfig.TenantId,
                    p.OrgId ?? "",
                    p.clientCertSubjectName ?? authConfig.ClientCertSubjectName,
                    p.DFMTenantId ?? ""
                );
            });
        }
    }
}