using CRM.ICon.Modality.Model.LiveChatSettings;
using CRM.ICon.Modality.Model.LiveChatSettings.Requests;

namespace CRM.ICon.Modality.Services.LiveChatSettings
{
    public interface ILiveChatSettingsService
    {
        Task<LiveChatRule> CreateRuleAsync(LiveChatRule newRule, string user);
        Task<LiveChatRule> UpdateRuleAsync(LiveChatRule request, string user);
        Task<LiveChatRule?> MatchUserAsync(MatchRuleRequest user);
        Task<List<LiveChatRule>> GetAllAsync();
        Task<int> GetMaxEvaluationOrderAsync();
        Task<LiveChatRule> GetRuleByNameAsync(string name);
        Task DeleteRuleAsync(LiveChatRule rule);
    }

}