namespace CRM.ICon.Modality.Services.Modality
{
    public class PublishContent
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ContentTypeName { get; set; }
        public string TelemetryId { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public IDictionary<string, FieldValue> Fields { get; set; }
        public string Path { get; set; }
        public string PresentationTemplate { get; set; }
        public bool UsesTargetingTags { get; set; }
        public ICollection<string> TargetingTagsUsed { get; set; }

        public PublishingField this[string fieldname]
        {
            get { return new PublishingField(CaselessFields, fieldname); }
        }

        private Dictionary<string, FieldValue> caselessFields;
        private Dictionary<string, FieldValue> CaselessFields
        {
            get
            {
                if (caselessFields == null)
                {
                    caselessFields = new Dictionary<string, FieldValue>(Fields, StringComparer.OrdinalIgnoreCase);
                }

                return caselessFields;
            }
        }
    }
}
