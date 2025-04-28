namespace CRM.ICon.Modality.Model
{
    public class CustomContext
    {
        public ServiceLevel ServiceLevel { get; set; }
        public Skill Skill { get; set; }

        public EnrichRoutingContext EnrichRoutingContext { get; set; }

        public ACE ACE { get; set; }

        public LCID LCID { get; set; }

        public Source Source { get; set; }

    }
}