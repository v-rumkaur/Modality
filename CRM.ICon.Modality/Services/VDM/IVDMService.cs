using CRM.ICon.Modality.Model.VDM.Requests;
using CRM.ICon.Modality.Model.VDM.Responses;

namespace CRM.ICon.Modality.Services.VDM
{
    public interface IVDMService
    {
        Task<VDMResponse> GetVDMSkill(VDMRequest request, string requestId);
    }
}
