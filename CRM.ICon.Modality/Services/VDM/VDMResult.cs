using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace CRM.ICon.Modality.Services.VDM
{
    public class VDMResult
    {
        public List<VDMResponse> purposefulResults { get; set; }
    }
}