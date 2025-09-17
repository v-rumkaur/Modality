namespace CRM.ICon.Modality.Services.Modality
{
    public class PartnerOp
    {
        public string ServiceName { get; }
        public string OperationName { get; }
        public string Description { get; }

        public PartnerOp(string serviceName, string operationName, string description)
        {
            ServiceName = serviceName;
            OperationName = operationName;
            Description = description;
        }
        // Add this property for compatibility
        public string OpName => OperationName;
    }
}