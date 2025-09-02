using CRM.ICon.Modality.Model.Modalities;
using System;
using System.Linq;

namespace CRM.ICon.Modality.Helpers
{
    public static class QueueLengthHelper
    {
        public static void RemoveQueueLengthLinkForCallBackModality(ref ModalitiesV1 modalities)
        {
            if (modalities?.Modalities != null && modalities.Modalities.Count() > 0)
            {
                var modalityList = modalities.Modalities.ToList();
                if (modalityList.Any(m => m.ContainsKey("Name")))
                {
                    var callbackModalityIndex = modalityList.FindIndex(m => string.Equals("callback", m["Name"], StringComparison.OrdinalIgnoreCase));
                    if (callbackModalityIndex > -1)
                    {
                        modalityList[callbackModalityIndex].Remove("QueueLength");
                    }
                }
            }
        }
    }
}