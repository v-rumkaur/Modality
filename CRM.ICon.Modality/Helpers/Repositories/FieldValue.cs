using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;

namespace CRM.ICon.Modality.Helpers.Repositories
{
    public class FieldValue
    {
        public string Name { get; set; }
        public virtual string Value { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public FieldDefinitionType Type { get; }
    }
}