
    using System;
    using System.Collections.Generic;
    using System.Web;

    namespace CRM.ICon.Modality.Helpers
    {
        /// <summary>
        /// Simple query builder for URI query strings.
        /// </summary>
        public class UriQueryBuilder
        {
            private readonly Dictionary<string, string> _parameters = new Dictionary<string, string>();

            public void Add(string key, string value)
            {
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    _parameters[key] = value;
                }
            }

            public Uri AddToUri(Uri baseUri)
            {
                var uriBuilder = new UriBuilder(baseUri);
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);

                foreach (var kvp in _parameters)
                {
                    query[kvp.Key] = kvp.Value;
                }

                uriBuilder.Query = query.ToString();
                return uriBuilder.Uri;
            }
        }
    }
