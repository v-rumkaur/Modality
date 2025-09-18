namespace CRM.ICon.Modality.Model
{
    public class ModalityResult
    {
        /// <summary>
        /// The SupportChannels that were requested
        /// </summary>
        public IEnumerable<Dictionary<string, string>> SupportChannels { get; set; }

        /// <summary>
        /// Constructor of SupportChannelsResult which takes the SupportChannels
        /// </summary>
        /// <param name="supportChannels"></param>
        public ModalityResult(IEnumerable<Dictionary<string, string>> supportChannels)
        {
            this.SupportChannels = supportChannels;
        }
    }
}
