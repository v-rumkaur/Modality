namespace CRM.ICon.Modality.Helpers.HttpContext
{
    public interface IHttpContextHandler
    {
        /// <summary>
        /// Gets the http context
        /// </summary>
        Microsoft.AspNetCore.Http.HttpContext HttpContext { get; }

        /// <summary>
        /// Gets the correlation Id
        /// </summary>
        string CorrelationId { get; }

        /// <summary>
        /// Gets the User Email
        /// </summary>
        string UserEmail { get; }

        /// <summary>
        /// Gets the User Object Id
        /// </summary>
        string UserObjectId { get; }
    }
}
