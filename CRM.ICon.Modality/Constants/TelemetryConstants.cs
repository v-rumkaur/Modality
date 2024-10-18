namespace CRM.ICon.Modality
{
    public static class TelemetryConstants
    {
        // Telemetry providers
        public const string GenevaLogProvider = "GenevaLogProvider";
        public const string ApplicationInsightsLogProvider = "ApplicationInsightsLogProvider";

        // Telemetry context properties
        public const string APIVersion = "APIVersion";
        public const string UserEmail = "UserEmail";
        public const string UserObjectId = "UserObjectId";
        public const string HttpCorrelationId = "HttpCorrelationId";

        // Telemetry properties
        public const string Method = "Method";
        public const string Path = "Path";
        public const string RequestTime = "Request Time";
        public const string RequestBody = "Request Body";
        public const string ResponseTime = "Response Time";
        public const string ResponseStatusCode = "Response Status Code";
        public const string WidgetConfig = "WidgetConfig";
        public const string WidgetConfigRequest = "WidgetConfigRequest";
        public const string ChatCorrelationId = "ChatCorrelationId";
        public const string CaseNumber = "CaseNumber";
        public const string CallerMethodName = "CallerMethodName";

        // Telemetry messages
        public const string RequestReceived = "Request Received";
        public const string ResponseSent = "Response Sent";

    }
}
