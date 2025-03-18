using CRM.ICon.Modality.Helpers.ModalityCosmos;
using CRM.ICon.Modality.Model;

namespace CRM.ICon.Modality.Services.Omnichannel
{
    public interface IOmnichannelService
    {
        Task<OmnichannelResponse> GetAgentAvailability(OmnichannelRequest request, string source, string userType, string requestId);

        WidgetDetails GetWidgetDetails(string language, string source, string userType, bool isMCS, string ring);

        public string GetSkillCharacteristicId(string skill);

        WidgetMappingResponse CreateWidgetDetails(WidgetMappingRequest widgetmappingRequest, string language, string source, string userType);
    }
}
