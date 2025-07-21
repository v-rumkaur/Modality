// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreateRuleRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.ComponentModel.DataAnnotations;

namespace CRM.ICon.Modality.Model.LiveChatSettings.Requests
{
    /// <summary>
    /// Represents a request to create a new live chat rule.
    /// </summary>
    public class CreateRuleRequest
    {
        [Required, MinLength(1)]
        [RegularExpression(
            @"^[a-zA-Z0-9_-]+$",
            ErrorMessage = "Rule name can only contain letters, numbers, hyphens, and underscores (no spaces)."
        )]
        public string Name { get; set; } = string.Empty;

        [Required]
        public List<string> AllowedServiceLevels { get; set; } = new();

        [Required]
        public bool AllowRestricted { get; set; }

        [Required]
        public List<Guid> AllowedSaps { get; set; } = new();

        [Required]
        public List<int> ExcludedServiceIds { get; set; } = new();

        [Required]
        public bool IsChatForced { get; set; }

        [Required, MinLength(1)]
        public string CreatedBy { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "EvaluationOrder must be >= 0")]
        public int? EvaluationOrder { get; set; }

        /// <summary>
        /// Converts this request to a domain model with normalized string values.
        /// </summary>
        /// <returns> A new LiveChatRule instance with normalized string values. </returns>
        public LiveChatRule ToDomainModel()
        {
            var rule = new LiveChatRule
            {
                Name = Name,
                AllowedServiceLevels = AllowedServiceLevels,
                AllowRestricted = AllowRestricted,
                AllowedSaps = AllowedSaps,
                ExcludedServiceIds = ExcludedServiceIds,
                IsChatForced = IsChatForced,
                EvaluationOrder = EvaluationOrder,
            };
            return rule;
        }
    }
}
