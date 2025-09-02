namespace CRM.ICon.Modality.Helpers.Logging
{
    public class SllLogger : ISllLogger
    {
        private readonly ILogger<SllLogger> _logger;

        public SllLogger(ILogger<SllLogger> logger)
        {
            _logger = logger;
        }

        public void WriteInformationalTelemetry(string eventName, string message)
        {
            _logger.LogInformation("[{Event}] {Message}", eventName, message);
        }

        public void WriteErrorTelemetry(string eventName, string message)
        {
            _logger.LogError("[{Event}] {Message}", eventName, message);
        }
    }
}
