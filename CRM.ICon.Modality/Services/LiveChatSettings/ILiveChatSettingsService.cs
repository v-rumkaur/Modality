using CRM.ICon.Modality.Model.LiveChatSettings;
using CRM.ICon.Modality.Model.LiveChatSettings.Requests;

namespace CRM.ICon.Modality.Services.LiveChatSettings
{
    public interface ILiveChatSettingsService
    {
        /// <summary>
        /// Matches a user request against live chat rules based on priority order
        /// </summary>
        /// <param name="user">User match request containing service level, SAP ID, etc.</param>
        /// <returns>The first matching rule or null if no match found</returns>
        Task<LiveChatRule?> MatchRuleAsync(MatchRuleRequest user);

        /// <summary>
        /// Gets all live chat rules ordered by evaluation order
        /// </summary>
        /// <returns>Collection of all live chat rules</returns>
        Task<IEnumerable<LiveChatRule>> GetAllRulesAsync();

        /// <summary>
        /// Gets a specific rule by its name
        /// </summary>
        /// <param name="name">Rule name to search for</param>
        /// <returns>The rule if found, otherwise null</returns>
        Task<LiveChatRule?> GetRuleByNameAsync(string name);

        /// <summary>
        /// Deletes a rule by its name
        /// </summary>
        /// <param name="name">Name of the rule to delete</param>
        Task DeleteRuleByNameAsync(string name);

        /// <summary>
        /// Creates a new live chat rule with automatic evaluation order management
        /// </summary>
        /// <param name="newRule">The rule to create</param>
        /// <param name="user">User creating the rule</param>
        /// <returns>The created rule with assigned evaluation order</returns>
        Task<LiveChatRule> CreateRuleAsync(LiveChatRule newRule, string user);

        /// <summary>
        /// Gets the maximum evaluation order currently in use
        /// </summary>
        /// <returns>Maximum evaluation order value</returns>
        Task<int> GetMaxEvaluationOrderAsync();

        /// <summary>
        /// Updates an existing rule with new values and handles evaluation order conflicts
        /// </summary>
        /// <param name="request">Update request containing the changes</param>
        /// <param name="updatedBy">User performing the update</param>
        Task UpdateRuleAsync(UpdateRuleRequest request, string updatedBy);
    }
}