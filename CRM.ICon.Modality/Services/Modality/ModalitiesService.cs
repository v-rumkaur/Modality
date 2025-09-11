using System.Collections.Generic;
using System.Threading.Tasks;
using CRM.ICon.Modality.Model;

namespace CRM.ICon.Modality.Services.Modality
{
    public class ModalitiesService : IModalitiesService
    {
        public async Task<ModalitiesV2> GetModalitiesAsync(
            string product,
            string issue,
            string partnerId,
            string platform,
            string language,
            string country,
            string mode,
            bool preview,
            bool disability,
            bool fallback,
            string host,
            bool isVNext,
            bool isTest
        )
        {
            // TODO: Migrate your actual business logic here
            // For now, stub returns empty
            return await Task.FromResult(new ModalitiesV2(new List<Dictionary<string, object>>()));
        }
    }
}