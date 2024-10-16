
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CRM.ICon.Modality.Services.VDM
{
    public class VDMService : IVDMService
    {
        private readonly HttpClient httpClient;
        private readonly VDMConfiguration vdmConfiguration;

        public VDMService(HttpClient httpClient, IOptions<VDMConfiguration> vdmConfiguration)
        {
            this.httpClient = httpClient;
            this.vdmConfiguration = vdmConfiguration.Value;
        }
        public async Task<VDMResponse> GetVDMSkill(VDMRequest request)
        {
            try
            {   
                var response = await httpClient.PostAsJsonAsync(vdmConfiguration.ServiceEndpoint, request);
                response.EnsureSuccessStatusCode();
                var stringresponse = await response.Content.ReadAsStringAsync();
                var deserializedResponse = JsonConvert.DeserializeObject<VDMResult>(stringresponse);
                return deserializedResponse.purposefulResults.FirstOrDefault();
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
