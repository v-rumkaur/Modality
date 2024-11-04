namespace CRM.ICon.Modality.Model
{
    public class ModalityInfo
    {
        public int Modality { get; set; }
        public string WaitTime { get; set; }
        public bool IsAgentAvailable { get; set; }

        public bool InHoops { get; set; }

        public WidgetDetails WidgetDetails  { get; set; }

        public CustomContext CustomContext { get; set; }

        public HoopsInfo HoopsInfo { get; set; }
    }
}