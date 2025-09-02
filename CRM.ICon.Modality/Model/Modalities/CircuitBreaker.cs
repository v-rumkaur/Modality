using Newtonsoft.Json;

namespace CRM.ICon.Modality.Model.Modalities
{
    public class CircuitBreaker
    {
        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }

        /// <summary>
        /// Gets or sets modalities
        /// </summary>
        [JsonProperty(PropertyName = "modalities")]
        public IList<Modality> Modalities
        {
            get;
            set;
        }
        public bool isGlobal { get; set; }

    }
}
