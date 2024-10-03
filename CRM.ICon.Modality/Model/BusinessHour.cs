namespace CRM.ICon.Modality.Model
{
    public class BusinessHour
    {
        public int StartDayOfWeek { get; set; }
        public int StartHour { get; set; }
        public int EndHour { get; set; }

        public int EndDayOfWeek { get; set; }
        public string TimeZoneName { get; set; }
        public string UtcOffset { get; set; }
        public int StartMin { get; set; }
        public int EndMin { get; set; }
    }
}