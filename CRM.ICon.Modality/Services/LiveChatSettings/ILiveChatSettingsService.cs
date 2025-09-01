// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ILiveChatService.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using CRM.ICon.Modality.Model.LiveChatSettings;
using CRM.ICon.Modality.Model.LiveChatSettings.Requests;

namespace CRM.ICon.Modality.Services.LiveChatSettings
{
    public interface ILiveChatSettingsService
    {
        /// <summary>
        /// Retrieves all live chat rules ordered by evaluation order.
        /// </summary>
        /// <returns>A collection of live chat rules.</returns>
        Task<IEnumerable<LiveChatRule>> GetAllRulesAsync();

        /// <summary>
        /// Retrieves a live chat rule by its name.
        /// </summary>
        /// <param name="name">The name of the rule to retrieve.</param>
        /// <returns>The live chat rule if found, otherwise null.</returns>
        Task<LiveChatRule?> GetRuleByNameAsync(string name);

        /// <summary>
        /// Finds the first live chat rule that matches the specified user criteria by evaluation order (ascending).
        /// </summary>
        /// <param name="user">The user criteria to match against live chat rules.</param>
        /// <returns>The first matching live chat rule, or null if no match is found.</returns>
        Task<LiveChatRule?> MatchRuleAsync(MatchRuleRequest user);

        /// <summary>
        /// Creates a new live chat rule with automatic evaluation order assignment and conflict resolution.
        /// </summary>
        /// <param name="newRule">The rule to create.</param>
        /// <param name="user">The user creating the rule (for audit purposes).</param>
        /// <returns>The created rule with assigned evaluation order and audit information.</returns>
        /// <exception cref="InvalidOperationException">Thrown when a rule with the same name already exists.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the evaluation order is less than 0.</exception>
        Task<LiveChatRule> CreateRuleAsync(LiveChatRule newRule, string user);

        /// <summary>
        /// Updates an existing live chat rule with the provided changes.
        /// Handles evaluation order conflicts if the order is modified.
        /// </summary>
        /// <param name="request">The update request containing the rule name and fields to modify.</param>
        /// <param name="updatedBy">The user performing the update (for audit purposes).</param>
        /// <exception cref="InvalidOperationException">Thrown when the rule is not found.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the new evaluation order is less than 0.</exception>
        Task UpdateRuleAsync(UpdateRuleRequest request, string updatedBy);

        /// <summary>
        /// Deletes a live chat rule by its name.
        /// </summary>
        /// <param name="name">The name of the rule to delete.</param>
        /// <exception cref="InvalidOperationException">Thrown when the rule is not found.</exception>
        Task DeleteRuleByNameAsync(string name);

        /// <summary>
        /// Gets the highest evaluation order currently assigned to any live chat rule.
        /// </summary>
        /// <returns>The maximum evaluation order value, or 0 if no rules exist.</returns>
        Task<int> GetMaxEvaluationOrderAsync();
    }
}
