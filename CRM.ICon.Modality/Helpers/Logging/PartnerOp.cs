namespace CRM.ICon.Modality.Helpers.Logging
{
    public class PartnerOp
    {
        public string System { get; }
        public string Operation { get; }
        public string Description { get; }

        public PartnerOp(string system, string operation, string description)
        {
            System = system;
            Operation = operation;
            Description = description;
        }
    }
}
