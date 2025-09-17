namespace CRM.ICon.Modality.Model
{
    using System.Collections.Generic;

    public class ModalitiesV2
    {
       // public List<Dictionary<string, object>> Modalities { get; set; }
        public IEnumerable<Dictionary<string, string>> Modalities { get; set; }

        // Optional: Add RenderingContext if needed for your scenario
        public RenderingContext RenderingContext { get; set; }

        public ModalitiesV2(IEnumerable<Dictionary<string, string>> supportChannels)
        {
            Modalities = supportChannels;
            RenderingContext = new RenderingContext(); // If you have a RenderingContext class, otherwise omit
        }

        // Emit method for compatibility (returns this instance)
        public ModalitiesV2 Emit()
        {
            return this;
        }
    }

    // If you don't have RenderingContext, define a stub or use the one from source
    public class RenderingContext
    {
        // Add properties/methods as required
    }
}