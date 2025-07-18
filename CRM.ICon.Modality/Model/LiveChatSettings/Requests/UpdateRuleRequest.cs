using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

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
        [JsonProperty("evaluationOrder")]
        [Range(0, int.MaxValue, ErrorMessage = "EvaluationOrder must be ≥ 0 if provided.")]
        public int? EvaluationOrder { get; set; }
        public required string UpdatedBy { get; set; }

        public void PatchToDomainModel(LiveChatRule existing)
        {
            if (AllowedServiceLevels?.Any() == true)
                existing.AllowedServiceLevels = AllowedServiceLevels.Select(s => s.ToLowerInvariant()).ToList();
            if (AllowedSaps?.Any() == true)
                existing.AllowedSaps = AllowedSaps.Select(s => s.ToLowerInvariant()).ToList();
            if (ExcludedServiceIds?.Any() == true)
                existing.ExcludedServiceIds = ExcludedServiceIds;
            if (AllowRestricted.HasValue)
                existing.AllowRestricted = AllowRestricted.Value;
            if (IsChatForced.HasValue)
                existing.IsChatForced = IsChatForced.Value;
            if (EvaluationOrder.HasValue && EvaluationOrder.Value >= 0)
                existing.EvaluationOrder = EvaluationOrder;
        }
    }
}
