using System.Web;

namespace CRM.ICon.Modality.Helpers
{
    public class UriQueryBuilder
    {
        private readonly Dictionary<string, string> _queryParams = new Dictionary<string, string>();

        /// <summary>
        /// Adds or updates a query parameter.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value.</param>
        public void Add(string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(name) && value != null)
            {
                _queryParams[name] = value;
            }
        }

        /// <summary>
        /// Builds a new URI by adding the stored query parameters to the provided base URI.
        /// </summary>
        /// <param name="uri">The base URI.</param>
        /// <returns>A new URI with query parameters applied.</returns>
        public Uri AddToUri(Uri uri)
        {
            var uriBuilder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            foreach (var kvp in _queryParams)
            {
                query[kvp.Key] = kvp.Value;
            }

            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri;
        }
    }
}