namespace CRM.ICon.Modality.Model
{
    public class WidgetRequest
    {
        public string Source { get; set; }
        public string RequestId { get; set; }
        public string Locale { get; set; }
        public int? UserLcid { get; set; }
        public string? UserType { get; set; }
        public string? Theme { get; set; }
    }
}
