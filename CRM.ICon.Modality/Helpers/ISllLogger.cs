namespace CRM.ICon.Modality.Helpers
{
    public interface ISllLogger
    {
        void WriteInformationalTelemetry(string key, string message, params object[] args);
        void WriteErrorTelemetry(string eventName, string message, params object[] args);
        void TrackOutgoingRequest(string opName, object op, Action<object> qosEventCallback);
    }
}
