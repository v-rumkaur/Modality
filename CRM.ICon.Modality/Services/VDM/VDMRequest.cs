using System.Text.Json;
using Newtonsoft.Json;

namespace CRM.ICon.Modality.Services.VDM
{
    public class VDMRequest
    {
        [JsonProperty("crmee_text")]
        public String? Text { get; set; }
        
        [JsonProperty("crmee_boundary")]
        public String? Boundary { get; set; }

        [JsonProperty("crmee_sapid")]
        public String? SapId { get; set; }

        [JsonProperty("crmee_predictionpurposes")]
        public String? PredictionPurposes { get; set; }
    }
}