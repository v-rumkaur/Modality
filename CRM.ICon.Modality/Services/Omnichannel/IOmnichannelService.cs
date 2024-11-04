using CRM.ICon.Modality.Model;

namespace CRM.ICon.Modality.Services.Omnichannel
{
    public interface IOmnichannelService
    {
        Task<OmnichannelResponse> GetAgentAvailability(OmnichannelRequest request, string source, string userType, string requestId);

        WidgetDetails GetWidgetDetails(string language, string source, string userType);

        public string GetSkillCharacteristicId(string skill);
    }
}
