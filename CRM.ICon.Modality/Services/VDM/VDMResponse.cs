using Newtonsoft.Json;

namespace CRM.ICon.Modality.Services.VDM
{
    public class VDMResponse
    {
        [JsonProperty(PropertyName = "crmee_name")]
        public string? skillName { get; set; }

        [JsonProperty(PropertyName = "_crmee_skill_value")]
        public string? skillValue { get; set; }
    }
}
