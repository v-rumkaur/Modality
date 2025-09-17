namespace CRM.ICon.Modality.Helpers
{
    public class SllLogger : ISllLogger
    {
        public void WriteInformationalTelemetry(string key, string message, params object[] args)
        {
            try
            {
                Console.WriteLine($"Info: {key} - {string.Format(message, args)}");
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Info: {key} - [FormatException] Message='{message}' Args='{string.Join(",", args ?? new object[0])}' Exception={ex}");
            }
        }

        public void WriteErrorTelemetry(string eventName, string message, params object[] args)
        {
            try
            {
                Console.WriteLine($"Error: {eventName} - {string.Format(message, args)}");
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Error: {eventName} - [FormatException] Message='{message}' Args='{string.Join(",", args ?? new object[0])}' Exception={ex}");
            }
        }

        public void TrackOutgoingRequest(string opName, object op, Action<object> qosEventCallback)
        {
            qosEventCallback?.Invoke(null);
        }
    }
}