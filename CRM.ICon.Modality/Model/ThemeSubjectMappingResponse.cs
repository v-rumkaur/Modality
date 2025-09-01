using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CRM.ICon.Modality.Model
{
    public class ThemeSubjectMappingResponse
    {
        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }

        /// <summary>
        /// Gets or sets modalities
        /// </summary>
        [JsonProperty(PropertyName = "SubjectId")]
        public string subjectId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets modalities
        /// </summary>
        [JsonProperty(PropertyName = "IsChat")]
        public bool isChat
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets modalities
        /// </summary>
        [JsonProperty(PropertyName = "IsC2C")]
        public bool isC2C
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets modalities
        /// </summary>
        [JsonProperty(PropertyName = "Theme")]
        public string theme
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets modalities
        /// </summary>
        [JsonProperty(PropertyName = "ThemeL1")]
        public string themeL1
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets modalities
        /// </summary>
        [JsonProperty(PropertyName = "ThemeL2")]
        public string themeL2
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets modalities
        /// </summary>
        [JsonProperty(PropertyName = "ThemeL3")]
        public string themeL3
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets modalities
        /// </summary>
        [JsonProperty(PropertyName = "CountryName")]
        public string countryName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets modalities
        /// </summary>
        [JsonProperty(PropertyName = "LanguageName")]
        public string languageName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets modalities
        /// </summary>
        [JsonProperty(PropertyName = "Entitlement")]
        public string entitlement
        {
            get;
            set;
        }
    }
}

