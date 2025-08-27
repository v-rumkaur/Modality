using System.Text.Json;
using Newtonsoft.Json;

namespace CRM.ICon.Modality.Services.VDM
{
    public class VDMRequest
    {
        [JsonProperty("Text")]
        public String? Text { get; set; }
        
        [JsonProperty("Boundary")]
        public String? Boundary { get; set; }

        [JsonProperty("SapId")]
        public String? SapId { get; set; }

        [JsonProperty("PredictionPurposes")]
        public String? PredictionPurposes { get; set; }
    }
}
