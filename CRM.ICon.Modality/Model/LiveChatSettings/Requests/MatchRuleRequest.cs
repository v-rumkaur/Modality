// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MatchRuleRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace CRM.ICon.Modality.Model.LiveChatSettings.Requests
{
    /// <summary>
    /// Represents a request to find the first matching live chat rule for a user's context.
    /// </summary>
    public class MatchRuleRequest
    {
        public required string ServiceLevel { get; set; }
        public required bool IsRestricted { get; set; }
        public required string SapId { get; set; }
        public required int ServiceId { get; set; }
    }
}
