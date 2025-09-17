namespace CRM.ICon.Modality.Services.Modality
{
    public class CompassService : ICompassService
    {
        public async Task<PublishContent> GetCompassContentAsync(HttpClient httpclient, HttpRequestMessage message, string contentPath, string localeName, bool isPreview)
        {
            // Stub: return a default/null PublishContent for now
            await Task.CompletedTask;
            return null;
        }

        public Dictionary<string, string> GetFallbackLocales()
        {
            // Stub: return empty dictionary
            return new Dictionary<string, string>();
        }
    }
}