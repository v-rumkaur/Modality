using System.Globalization;
using System.Net;

namespace CRM.ICon.Modality.Helpers.Repositories
{
    public class PublishingField
    {
        private Dictionary<string, FieldValue> CaselessFields { get; set; }

        private FieldValue FieldValue { get; set; }

        public string FieldName { get; private set; }
        private string FieldNameAsPrefix { get { return (FieldName == null) ? "" : FieldName + "/"; } }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("SDL.AntiXSS", "CA5600:PlatformEncodingShouldNotBeUsed")]
        public string Value
        {
            get
            {
                if (this.FieldValue.Type is FieldDefinitionType.Image)
                {
                    return FieldValue.Value;
                }
                if (this.FieldValue.Type is FieldDefinitionType.RichContent)
                {
                    return FieldValue.Value;
                }
                if (this.FieldValue.Type is FieldDefinitionType.Xml)
                {
                    return FieldValue.Value;
                }
                if (this.FieldValue.Type is FieldDefinitionType.DateTime)
                {
                    return FieldValue.Value;
                }
                if (FieldValue != null)
                {
                    return WebUtility.HtmlEncode(FieldValue.Value);
                }
                return String.Empty;
            }
        }

        public string UnencodedValue
        {
            get
            {
                if (FieldValue != null)
                {
                    return FieldValue.Value;
                }
                return String.Empty;
            }
        }

        public PublishingField(Dictionary<string, FieldValue> caselessFields, string fieldName)
        {
            CaselessFields = caselessFields;
            FieldName = fieldName;

            FieldValue fieldValue = null;
            if (FieldName != null)
            {
                CaselessFields.TryGetValue(FieldName, out fieldValue);
            }
            FieldValue = fieldValue;
        }

        public PublishingField this[string nestedFieldName]
        {
            get { return new PublishingField(CaselessFields, FieldNameAsPrefix + nestedFieldName); }
        }

        public IEnumerable<PublishingField> Items
        {
            get
            {
                IEnumerable<int> generator = GetNumberGenerator();
                return generator
                    .TakeWhile(itemIndex => CaselessFields.ContainsKey(FieldNameAsPrefix + itemIndex + "/#Name"))
                    .Select(itemIndex => this[itemIndex.ToString(CultureInfo.InvariantCulture)]);
            }
        }

        private static IEnumerable<int> GetNumberGenerator()
        {
            int counter = 0;
            while (counter < Int32.MaxValue)
            {
                yield return counter++;
            }
        }
    }
}
