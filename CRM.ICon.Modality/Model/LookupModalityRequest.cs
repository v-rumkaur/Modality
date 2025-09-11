using System.ComponentModel.DataAnnotations;

namespace CRM.ICon.Modality.Model
{
    public class LookupModalityRequest
    {
        [Required]
        public string Product { get; set; }
        [Required]
        public string Issue { get; set; }
        [Required]
        public string PartnerId { get; set; }
        [Required]
        public string Platform { get; set; }
        [Required]
        public string Language { get; set; }
        [Required]
        public string Country { get; set; }
        public string Mode { get; set; }
        public string Accessibility { get; set; }
        public bool IsVNext { get; set; }
        public bool IsTest { get; set; }
        public bool Preview { get; set; }
        public bool Fallback { get; set; }
    }
}