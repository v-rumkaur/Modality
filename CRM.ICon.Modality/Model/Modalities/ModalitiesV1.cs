namespace CRM.ICon.Modality.Model.Modalities
{
    public class ModalitiesV1
    {
        public IEnumerable<Dictionary<string, string>> Modalities { get; set; }

        public ModalitiesV1(IEnumerable<Dictionary<string, string>> supportChannels)
        {
            Modalities = supportChannels;
        }

        public ModalitiesV1 Emit()
        {
            return this;
        }
    }
}
