namespace CRM.ICon.Modality.Model.LiveChatSettings.Requests
{
    public class UpdateRuleRequest
    {
        public required string Name { get; set; }

        public List<string>? AllowedServiceLevels { get; set; }

        public bool? AllowRestricted { get; set; }

        public List<string>? AllowedSaps { get; set; }

        public List<int>? ExcludedServiceIds { get; set; }

        public bool? IsChatForced { get; set; }

        public int? EvaluationOrder { get; set; }

        // Optional helper to apply this update to a domain model
        public void ApplyUpdatesTo(LiveChatRule rule)
        {
            if (Name is not null)
                rule.Name = Name;

            if (AllowedServiceLevels is not null)
                rule.AllowedServiceLevels = AllowedServiceLevels;

            if (AllowRestricted.HasValue)
                rule.AllowRestricted = AllowRestricted.Value;

            if (AllowedSaps is not null)
                rule.AllowedSaps = AllowedSaps;

            if (ExcludedServiceIds is not null)
                rule.ExcludedServiceIds = ExcludedServiceIds;

            if (IsChatForced.HasValue)
                rule.IsChatForced = IsChatForced.Value;

            if (EvaluationOrder.HasValue)
                rule.EvaluationOrder = EvaluationOrder.Value;
        }
    }
}