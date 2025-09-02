using CRM.ICon.Modality.Helpers.Repositories;

namespace CRM.ICon.Modality.Services.Modality
{
    public interface ICompassService
    {
        Task<PublishContent> GetCompassContentAsync(HttpClient httpclient, HttpRequestMessage message, string contentPath, string localeName, bool isPreview);
        Dictionary<string, string> GetFallbackLocales();
    }
}
