// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IsChatEligibleResponse.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CRM.ICon.Modality.Model.LiveChatSettings.Responses
{
    /// <summary>
    /// Response indicating whether live chat is available for a user.
    /// </summary>
    public class IsChatEligibleResponse
    {
        /// <summary> Whether the user is eligible for live chat based on matching rules. </summary>
        public bool IsChatEligible { get; set; }

        /// <summary> Whether live chat should be forced/always shown for this user. </summary>
        public bool IsChatForced { get; set; }
    }
}
