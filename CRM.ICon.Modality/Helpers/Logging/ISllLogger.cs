namespace CRM.ICon.Modality.Helpers.Logging
{
    public interface ISllLogger
    {
        void WriteInformationalTelemetry(string eventName, string message);
        void WriteErrorTelemetry(string eventName, string message);
    }
}
