using CRM.ICon.Modality.Services;
using CRM.ICon.Modality.Services.Modality;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CRM.ICon.Modality.Model
{
    public class CircuitBreaker
    {
        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }
        public string Product { get; set; }
        public string Issue { get; set; }
        public bool isGlobal { get; set; }
        [JsonProperty(PropertyName = "modalities")]
        public IList<Modality.Services.Modality.Modality> Modalities
        {
            get;
            set;
        }
       
    }
}