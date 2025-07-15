using CRM.ICon.Modality.Model.LiveChatSettings;
using CRM.ICon.Modality.Model.LiveChatSettings.Requests;

namespace CRM.ICon.Modality.Services.LiveChatSettings
{
    public interface ILiveChatSettingsService
    {
        Task<IEnumerable<LiveChatRule>> GetAllRulesAsync();
        Task CreateRuleAsync(LiveChatRule newRule, string user);
        Task<LiveChatRule?> MatchRuleAsync(MatchRuleRequest request);
    }
}
