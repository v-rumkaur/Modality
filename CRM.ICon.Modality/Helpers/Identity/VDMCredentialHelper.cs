using Azure.Core;
using Azure.Identity;
using CRM.ICon.Modality.Helpers.Telemetry;
using CRM.ICon.Modality.Helpers;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace CRM.ICon.Modality.Helpers.Identity
{
    public static class VDMCredentialHelper
    {
        /// <summary>
        /// Gets a TokenCredential specifically configured for VDM cross-tenant authentication
        /// </summary>
        /// <param name="telemetryService">Telemetry service instance</param>
        /// <param name="vdmTenantId">VDM tenant ID (72f988bf-86f1-41af-91ab-2d7cd011db47)</param>
        /// <param name="appRegistrationId">App registration ID in VDM tenant</param>
        /// <param name="managedIdentityId">Managed identity client ID for assertion</param>
        /// <returns>TokenCredential for VDM authentication</returns>
        public static TokenCredential GetVDMCredential(ITelemetryService telemetryService, string vdmTenantId, string appRegistrationId, string managedIdentityId)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["VDM_TENANT_ID"] = vdmTenantId;
            logProperties["VDM_APP_REGISTRATION_ID"] = appRegistrationId;
            logProperties["VDM_MANAGED_IDENTITY_ID"] = managedIdentityId;
            logProperties["VDM_AUTH_METHOD"] = "ClientAssertionCredential";

            try
            {
                telemetryService.LogTrace<object>("VDM GetCredential - Starting ClientAssertionCredential creation", logProperties);

                // Use ClientAssertionCredential with ManagedIdentityClientAssertion for cross-tenant VDM auth
                var credential = new ClientAssertionCredential(
                    vdmTenantId, 
                    appRegistrationId, 
                    _ => new ManagedIdentityClientAssertion(managedIdentityId).GetSignedAssertionAsync(null)
                );

                telemetryService.LogTrace<object>("VDM GetCredential - ClientAssertionCredential created successfully", logProperties);
                return credential;
            }
            catch (Exception ex)
            {
                telemetryService.LogException<object>(ex, logProperties, "VDM GetCredential - Failed to create ClientAssertionCredential");
                throw;
            }
        }

        /// <summary>
        /// Gets an access token for VDM API calls
        /// </summary>
        /// <param name="telemetryService">Telemetry service instance</param>
        /// <param name="vdmTenantId">VDM tenant ID</param>
        /// <param name="appRegistrationId">App registration ID in VDM tenant</param>
        /// <param name="managedIdentityId">Managed identity client ID</param>
        /// <param name="vdmResource">VDM resource scope (e.g., "https://vdmppe.crm.dynamics.com/.default")</param>
        /// <returns>Access token for VDM API</returns>
        public static async Task<string> GetVDMAccessTokenAsync(ITelemetryService telemetryService, string vdmTenantId, string appRegistrationId, string managedIdentityId, string vdmResource)
        {
            var logProperties = ModalityExtensions.GetRequestProperties();
            logProperties["VDM_TENANT_ID"] = vdmTenantId;
            logProperties["VDM_APP_REGISTRATION_ID"] = appRegistrationId;
            logProperties["VDM_MANAGED_IDENTITY_ID"] = managedIdentityId;
            logProperties["VDM_RESOURCE"] = vdmResource;
            logProperties["VDM_AUTH_METHOD"] = "ClientAssertionCredential";

            try
            {
                telemetryService.LogTrace<object>("VDM GetAccessToken - Starting token acquisition process", logProperties);

                var credential = GetVDMCredential(telemetryService, vdmTenantId, appRegistrationId, managedIdentityId);
                
                logProperties["VDM_TOKEN_CONTEXT_SCOPES"] = string.Join(",", new[] { vdmResource });
                telemetryService.LogTrace<object>("VDM GetAccessToken - Creating TokenRequestContext", logProperties);
                
                var tokenContext = new TokenRequestContext(new[] { vdmResource });
                
                telemetryService.LogTrace<object>("VDM GetAccessToken - Calling credential.GetTokenAsync", logProperties);
                var accessToken = await credential.GetTokenAsync(tokenContext, CancellationToken.None);

                // Add token metadata to logs (but mask the actual token value)
                logProperties["VDM_TOKEN_EXPIRES_ON"] = accessToken.ExpiresOn.ToString("yyyy-MM-dd HH:mm:ss UTC");
                logProperties["VDM_TOKEN_LENGTH"] = accessToken.Token.Length.ToString();
                logProperties["VDM_TOKEN_PREFIX"] = accessToken.Token;
                logProperties["VDM_TOKEN_SUCCESS"] = "true";
                
                telemetryService.LogTrace<object>("VDM GetAccessToken - Token acquired successfully", logProperties);
                return accessToken.Token;
            }
            catch (Exception ex)
            {
                logProperties["VDM_TOKEN_SUCCESS"] = "false";
                logProperties["VDM_ERROR_TYPE"] = ex.GetType().Name;
                logProperties["VDM_ERROR_MESSAGE"] = ex.Message;
                telemetryService.LogException<object>(ex, logProperties, "VDM GetAccessToken - Failed to acquire access token");
                throw;
            }
        }
    }
}