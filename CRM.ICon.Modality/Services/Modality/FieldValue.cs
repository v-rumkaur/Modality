using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CRM.ICon.Modality.Services.Modality
{
    public class FieldValue
    {
        public string Name { get; set; }
        public virtual string Value { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public FieldDefinitionType Type { get; }
    }
}
