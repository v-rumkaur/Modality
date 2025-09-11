using System.Collections.Generic;
using System.Linq;

namespace CRM.ICon.Modality.Model
{
    public class ModalitiesV0
    {
        public Dictionary<string, Dictionary<string, string>> Modalities { get; set; }

        public ModalitiesV0(IEnumerable<Dictionary<string, string>> supportChannels)
        {
            this.Modalities = new Dictionary<string, Dictionary<string, string>>();
            foreach (var supportChannel in supportChannels)
            {
                var modalityName = supportChannel["Name"];
                var newChannel = new Dictionary<string, string>(supportChannel);
                newChannel.Remove("Name");
                this.Modalities.Add(modalityName, newChannel);
            }
        }

        public Dictionary<string, Dictionary<string, string>> Emit()
        {
            return this.Modalities;
        }
    }
}