namespace CRM.ICon.Modality.Services.VDM
{
    public interface IVDMService
    {
        Task<VDMResponse> GetVDMSkill(VDMRequest request, string requestId);
    }
}
