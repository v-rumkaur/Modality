using Newtonsoft.Json;

namespace CRM.ICon.Modality.Helpers.ModalityCosmos
{
    public class WidgetMappingResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        public WidgetData Primary { get; set; }
        public WidgetData Secondary { get; set; }
        public WidgetData Backup { get; set; }

        public string Source { get; set; }

        public string Language { get; set; }

        public string Country { get; set; }

        public string UserType { get; set; }

        public string Region { get; set; }

        public bool IsMCS { get; set; }

    }
}
