
using Microsoft.Extensions.Options;

namespace CRM.ICon.Modality.Services.VDM
{
    public class VDMService : IVDMService
    {
        private readonly HttpClient httpClient;

        public VDMService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }
        public async Task<VDMResponse> GetVDMSkill(VDMRequest request)
        {
            var response = await httpClient.PostAsJsonAsync("https://vdmsdksvc-pb-ppe.azurewebsites.net/api/crmee_PredictSupportAreaPathCategories", request);
            response.EnsureSuccessStatusCode();
            
            var deserializedResponse = await response.Content.ReadFromJsonAsync<VDMResult>();

            if (deserializedResponse == null)
            {
                throw new InvalidDataException();
            }

            return deserializedResponse.results.FirstOrDefault();
        }
    }
}
