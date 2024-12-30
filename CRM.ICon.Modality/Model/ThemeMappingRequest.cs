using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CRM.ICon.Modality.Model
{
    public class ThemeMappingRequest
    {
        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }

        /// <summary>
        /// Gets or sets theme rule data
        /// </summary>
        public List<string> ThemeRuleData
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets chat
        /// </summary>
        [JsonProperty(PropertyName = "IsChat")]
        public bool isChat
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets C2C
        /// </summary>
        [JsonProperty(PropertyName = "IsC2C")]
        public bool isC2C
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets SubjectId
        /// </summary>
        [JsonProperty(PropertyName = "SubjectId")]
        public string subjectId
        {
            get;
            set;
        }
    }
}
