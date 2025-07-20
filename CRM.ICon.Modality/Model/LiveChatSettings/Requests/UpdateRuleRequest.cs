// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UpdateRuleRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CRM.ICon.Modality.Model.LiveChatSettings.Requests
{
    /// <summary>
    /// Represents a request to update an existing live chat rule.
    /// </summary>
    public class UpdateRuleRequest
    {
        [Required, MinLength(1)]
        public string Name { get; set; } = string.Empty;

        public List<string>? AllowedServiceLevels { get; set; }

        public bool? AllowRestricted { get; set; }
        public List<string>? AllowedSaps { get; set; }

        public List<int>? ExcludedServiceIds { get; set; }
        public bool? IsChatForced { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "EvaluationOrder must be ≥ 0 if provided.")]
        public int? EvaluationOrder { get; set; }

        [Required, MinLength(1)]
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Applies partial updates from this request to an existing live chat rule.
        /// Only non-null properties will update the existing rule.
        /// </summary>
        /// <param name="existing">The existing live chat rule to update.</param>
        public void PatchToDomainModel(LiveChatRule existing)
        {
            if (AllowedServiceLevels?.Any() == true)
                existing.AllowedServiceLevels = AllowedServiceLevels
                    .Select(s => s.ToLowerInvariant())
                    .ToList();
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
