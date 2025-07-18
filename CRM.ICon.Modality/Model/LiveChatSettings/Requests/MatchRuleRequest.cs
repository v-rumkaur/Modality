namespace CRM.ICon.Modality.Model.LiveChatSettings.Requests
{
    /// <summary>
    /// Represents a request to match a user's live chat context to a rule in live chat settings.
    /// Determines if user can see the Chat modality if the rule matches and IsChatEligible is true.
    /// </summary>
    public class MatchRuleRequest
    {
        public required string ServiceLevel { get; set; }
        public required bool IsRestricted { get; set; }
        public required string SapId { get; set; }
        public required int ServiceId { get; set; }

        /// <summary>
        /// Normalizes ServiceLevel and SapId properties to lowercase for consistent matching
        /// </summary>
        public void Normalize()
        {
            ServiceLevel = ServiceLevel?.ToLowerInvariant() ?? string.Empty;
            SapId = SapId?.ToLowerInvariant() ?? string.Empty;
        }
    }
}
