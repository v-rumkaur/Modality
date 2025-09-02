using System;
using System.ComponentModel.DataAnnotations;


namespace CRM.ICon.Modality.Model.Modalities
{
    /// <summary>
    /// Summary description for Class1
    /// </summary>
    public class LookupModalityRequest
    {
        /// <summary>
        /// Product identifier (eg "windows").
        /// </summary>
        [Required]
        public string Product { get; set; }

        /// <summary>
        /// Issue category (eg "tech-services").
        /// </summary>
        [Required]
        public string Issue { get; set; }

        /// <summary>
        /// Caller's Partner Id (issued when onboarding).
        /// </summary>
        [Required]
        public string PartnerId { get; set; }

        /// <summary>
        /// Caller's platform (eg "iOS", "Android", "Web").
        /// </summary>
        [Required]
        public string Platform { get; set; }

        /// <summary>
        /// The requested language (eg "en").
        /// </summary>
        [Required]
        public string Language { get; set; }

        /// <summary>
        /// The requested country (eg "us").
        /// </summary>
        [Required]
        public string Country { get; set; }

        /// <summary>
        /// The mode (eg "live").
        /// </summary>
        public string Mode { get; set; } = "live";

        /// <summary>
        /// Request draft resources (only available within Corpnet). Default is "false".
        /// </summary>
        public bool Preview { get; set; } = false;

        /// <summary>
        /// Request resources for a specific accessibility identifier. Default is "none".
        /// </summary>
        public string Accessibility { get; set; } = "none";

        /// <summary>
        /// Use fallback locales. Default is true.
        /// </summary>
        public bool Fallback { get; set; } = true;

        /// <summary>
        /// Key value pairs for partner specific routing context (eg. customer entitlement).
        /// </summary>
        public Dictionary<string, object> Context { get; set; }

        /// <summary>
        /// If the request is VNext or not
        /// </summary>
        public bool IsVNext { get; set; } = false;

        /// <summary>
        /// When VNext is true, if the request is Test or not
        /// </summary>
        public bool IsTest { get; set; } = false;
    }
}
