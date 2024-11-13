using System.Configuration;

namespace CRM.ICon.Modality
{
    public static class Constants
    {
        public const string TelemetryConfiguration = "TelemetryConfiguration";
        public const string KeyVaultConfiguration = "KeyVaultConfiguration";
        public const string ApplicationInsightsConnectionString = "ApplicationInsightsConnectionString";
        public const double VDMCacheTimeInMinutes = 30;
 
        //Headers
        public const string Target = "x-msaas-target";

        //RequestId
        public const string RequestId = "RequestId";
    }
}
