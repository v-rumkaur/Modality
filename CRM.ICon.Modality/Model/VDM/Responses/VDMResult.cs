using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace CRM.ICon.Modality.Model.VDM.Responses
{
    // Used for parsing VDM result into VDM response
    public class VDMResult
    {
        public VDMResultData? Result { get; set; }
    }

    public class VDMResultData
    {
        public List<VDMResponse> purposefulResults { get; set; }
    }
}