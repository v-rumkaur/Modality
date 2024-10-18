using System.Security.Claims;

namespace CRM.ICon.Modality.Helpers.HttpContext
{
    public class HttpContextHandler : IHttpContextHandler
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        private const string CorrelationIdHeader = "X-CorrelationId";

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContextHandler"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">http context accessor object</param>
        public HttpContextHandler(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Gets the http context
        /// </summary>
        public Microsoft.AspNetCore.Http.HttpContext HttpContext => httpContextAccessor.HttpContext;

        /// <summary>
        /// Gets the correlation Id
        /// </summary>
        public string CorrelationId
        {
            get
            {
                var headers = HttpContext.Request.Headers;
                return headers.ContainsKey(CorrelationIdHeader) ? headers[CorrelationIdHeader].ToString() : string.Empty;
            }
        }

        /// <summary>
        /// Gets the User Email
        /// </summary>
        public string UserEmail
        {
            get
            {
                return HttpContext.User.FindFirst(ClaimTypes.Upn)?.Value ?? HttpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the User Object Id
        /// </summary>
        public string UserObjectId
        {
            get
            {
                return HttpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ?? string.Empty;
            }
        }
    }
}
