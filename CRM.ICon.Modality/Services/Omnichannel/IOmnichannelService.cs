using CRM.ICon.Modality.Model;

namespace CRM.ICon.Modality.Services.Omnichannel
{
    public interface IOmnichannelService
    {
        Task<OmnichannelResponse> GetAgentAvailability(OmnichannelRequest request);

        WidgetDetails GetWidgetDetails(string language);

        string GetSkillCharacteristicId(string skill);
    }
}
