namespace CRM.ICon.Modality.Services.Omnichannel
{
    public class OmnichannelResponse
    {
        public bool IsAgentAvailable { get; set; }
        public string AverageWaitTime { get; set; }

        public bool IsQueueAvailable { get; set; }

        public string QueueId { get; set; }
    }
}