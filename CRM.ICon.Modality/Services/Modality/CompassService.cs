using Newtonsoft.Json;

namespace CRM.ICon.Modality.Services.Modality
{
    public class CompassService : ICompassService
    {
        public async Task<PublishContent> GetCompassContentAsync(HttpClient httpclient, HttpRequestMessage message, string contentPath, string localeName, bool isPreview)
        {
            using (var response = await httpclient.SendAsync(message))
            {
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var content = JsonConvert.DeserializeObject<PublishContent>(json);
                return content;
            }
        }

        public Dictionary<string, string> GetFallbackLocales()
        {
            return new Dictionary<string, string>();
        }
    }
}