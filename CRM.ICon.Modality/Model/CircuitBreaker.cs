using System.Collections.Generic;

namespace CRM.ICon.Modality.Model
{
    public class CircuitBreaker
    {
        public string Product { get; set; }
        public string Issue { get; set; }
        public Dictionary<string, object> Modalities { get; set; }
    }
}