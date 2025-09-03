namespace CRM.ICon.Modality.Helpers.Logging
{
    public interface ISllLogger
    {
        void WriteInformationalTelemetry(string eventName, string message);
        void WriteInformationalTelemetry(string eventName, string format, params object[] args);
        void WriteErrorTelemetry(string eventName, string message);
        void WriteErrorTelemetry(string eventName, string format, params object[] args);
    }
}
