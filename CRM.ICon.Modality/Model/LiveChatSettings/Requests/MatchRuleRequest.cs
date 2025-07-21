// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MatchRuleRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.ComponentModel.DataAnnotations;

namespace CRM.ICon.Modality.Model.LiveChatSettings.Requests
{
    /// <summary>
    /// Represents a request to find the first matching live chat rule for a user's context.
    /// </summary>
    public class MatchRuleRequest
    {
        [Required]
        [RegularExpression(
            @"^[a-zA-Z0-9_-]+$",
            ErrorMessage = "Service level can only contain letters, numbers, underscores, and hyphens."
        )]
        public required string ServiceLevel { get; set; }

        [Required]
        public required bool IsRestricted { get; set; }

        [Required]
        public required Guid SapId { get; set; }
        
        [Required]
        public required int ServiceId { get; set; }
    }
}
