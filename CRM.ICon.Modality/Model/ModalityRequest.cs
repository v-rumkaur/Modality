namespace CRM.ICon.Modality.Model
{
    public class ModalityRequest
    {
        public string Source { get; set; }
        public string RequestId { get; set; }
        public string? Locale { get; set; }
        public string Country { get; set; }
        public int? UserLcid { get; set; }
        public string? UserType { get; set; }
        public bool IsCopilot { get; set; }
        public Dictionary<string, string>? ExtensionAttributes { get; set; }
        public string? Theme { get; set; }
        public SupportTicketAttribute SupportTicketAttributes { get; set; }

        public CustomerAttribute? CustomerAttributes { get; set; }
    }
}
