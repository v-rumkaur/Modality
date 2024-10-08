namespace CRM.ICon.Modality.Model
{
    public class SupportTicketAttribute
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? SapId { get; set; }
        public string? Severity { get; set; }
        public EntitlementInformation? EntitlementInformation { get; set; }
    }
}