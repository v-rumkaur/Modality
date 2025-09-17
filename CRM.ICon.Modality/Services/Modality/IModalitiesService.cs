using System.Collections.Generic;
using System.Threading.Tasks;
using CRM.ICon.Modality.Model;

namespace CRM.ICon.Modality.Services.Modality
{
    public interface IModalitiesService
    {
        Task<ModalitiesV2> GetModalitiesAsync(
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
        );
        // Add this for direct access to raw modalities for V0/V1
        //Task<List<Dictionary<string, object>>> GetRawModalitiesAsync(
        //    string product,
        //    string issue,
        //    string partnerId,
        //    string platform,
        //    string language,
        //    string country,
        //    string mode,
        //    bool preview,
        //    bool disability,
        //    bool fallback,
        //    string host,
        //    bool isVNext,
        //    bool isTest
        //);
        Task<CircuitBreaker> UpdateCircuitBreakerAsync(CircuitBreaker circuitBreaker);
    }
}