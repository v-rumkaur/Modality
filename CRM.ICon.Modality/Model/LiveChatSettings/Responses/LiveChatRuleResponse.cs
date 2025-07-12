namespace CRM.ICon.Modality.Model.LiveChatSettings.Responses
{
    public class LiveChatRuleResponse
    {
        public string Name { get; set; }

        public List<string> AllowedServiceLevels { get; set; }

        public bool AllowRestricted { get; set; }

        public List<string> AllowedSaps { get; set; }

        public List<int> ExcludedServiceIds { get; set; }

        public bool IsChatForced { get; set; }

        public int? EvaluationOrder { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string? CreatedBy { get; set; }

        public string? UpdatedBy { get; set; }

        public static LiveChatRuleResponse FromDomainModel(LiveChatRule rule) => new LiveChatRuleResponse
        {
            Name = rule.Name,
            AllowedServiceLevels = rule.AllowedServiceLevels,
            AllowRestricted = rule.AllowRestricted,
            AllowedSaps = rule.AllowedSaps,
            ExcludedServiceIds = rule.ExcludedServiceIds,
            IsChatForced = rule.IsChatForced,
            EvaluationOrder = rule.EvaluationOrder,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt,
            CreatedBy = rule.CreatedBy,
            UpdatedBy = rule.UpdatedBy
        };
    }
}