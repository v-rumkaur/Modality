namespace CRM.ICon.Modality.Model
{
    public class SupportTicketAttribute
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? SapId { get; set; }
        public string? Severity { get; set; }
        public EntitlementInformation? EntitlementInformation { get; set; }
        public string? SupportAreaName { get; set; }
        public string? SkillName {  get; set; }

        public string? SkillValue { get; set; }
    }
}