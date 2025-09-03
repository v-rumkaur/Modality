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
        public void WriteInformationalTelemetry(string eventName, string format, params object[] args)
        {
            // Example implementation: You can customize the actual telemetry/logging logic
            string message = string.Format(format, args);
            // Replace this with your telemetry/logging infrastructure:
            Console.WriteLine($"[INFO][{eventName}] {message}");
        }

        public void WriteErrorTelemetry(string eventName, string format, params object[] args)
        {
            string message = string.Format(format, args);
            // Replace this with your error telemetry/logging infrastructure:
            Console.WriteLine($"[ERROR][{eventName}] {message}");
        }

        public void WriteErrorTelemetry(string eventName, string message)
        {
            _logger.LogError("[{Event}] {Message}", eventName, message);
        }
    }
}
