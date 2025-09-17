namespace CRM.ICon.Modality.Helpers
{
    public class SllLogger : ISllLogger
    {
        public void WriteInformationalTelemetry(string key, string message, params object[] args)
        {
            Console.WriteLine($"Info: {key} - {string.Format(message, args)}");
        }

        public void WriteErrorTelemetry(string eventName, string message, params object[] args)
        {
            Console.WriteLine($"Error: {eventName} - {string.Format(message, args)}");
        }
        // Stub implementation (does nothing)
        public void TrackOutgoingRequest(string opName, object op, Action<object> qosEventCallback)
        {
            // If you want, you can invoke the callback with null or a dummy object
            qosEventCallback?.Invoke(null);
        }
    }
}