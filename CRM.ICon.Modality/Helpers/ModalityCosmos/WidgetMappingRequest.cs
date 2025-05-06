namespace CRM.ICon.Modality.Helpers.ModalityCosmos
{
    public class WidgetMappingRequest
    {
        public string Source { get; set; }
        public string RequestId { get; set; }
        public string Locale { get; set; }
        public int? UserLcid { get; set; }
        public string? UserType { get; set; }

        public string? Ring { get; set; }

        public bool IsMCS { get; set; }

        public WidgetData Primary { get; set; }
        public WidgetData? Backup { get; set; }
        public bool IsBackUp { get; set; }
    }
}
