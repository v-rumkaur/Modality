using System.ComponentModel.DataAnnotations;

namespace CRM.ICon.Modality.Model.LiveChatSettings.Requests
{
    public class CreateRuleRequest
    {
        [Required, MinLength(1)]
        public string Name { get; set; } = string.Empty;

        [Required, MinLength(1)]
        public List<string> AllowedServiceLevels { get; set; } = new();

        [Required]
        public bool AllowRestricted { get; set; }

        [Required, MinLength(1)]
        public List<string> AllowedSaps { get; set; } = new();

        [Required]
        public List<int> ExcludedServiceIds { get; set; } = new();

        [Required]
        public bool IsChatForced { get; set; }

        [Required, MinLength(1)]
        public string CreatedBy { get; set; } = string.Empty;

        //[Range(0, int.MaxValue, ErrorMessage = "EvaluationOrder must be >= 0")]
        //[System.ComponentModel.DefaultValue(null)]
        public int? EvaluationOrder { get; set; }

        public LiveChatRule ToDomainModel()
        {
            return new LiveChatRule
            {
                Name = Name.Trim().ToLowerInvariant(),
                AllowedServiceLevels = AllowedServiceLevels
                    .Select(s => s.Trim().ToLowerInvariant())
                    .ToList(),
                AllowRestricted = AllowRestricted,
                AllowedSaps = AllowedSaps.Select(s => s.Trim().ToLowerInvariant()).ToList(),
                ExcludedServiceIds = ExcludedServiceIds,
                IsChatForced = IsChatForced,
                EvaluationOrder = EvaluationOrder,
            };
        }
    }
}
