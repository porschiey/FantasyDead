namespace FantasyDead.Web.Controllers
{
    using App_Start;
    using Data;
    using FantasyDead.Web.Models;
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Web.Http;

    /// <summary>
    /// Base controller for all API calls, except for regristration. Enforces auth.
    /// </summary>
    [ApiAuthorization]
    public class BaseApiController : ApiController
    {

        /// <summary>
        /// Helper method to retrieve the requestor identity on API call.
        /// </summary>
        public SlimPerson Requestor
        {
            get
            {

                if (this.Request == null || ClaimsPrincipal.Current == null || ClaimsPrincipal.Current.Claims.Count() != 4)
                    return null;

                return new SlimPerson(ClaimsPrincipal.Current.Claims.ToList());
            }
        }

        /// <summary>
        /// Helper method to return any response that may be empty.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public HttpResponseMessage ConvertDbResponse(DataContextResponse response)
        {
            if (response.Content != null)
                throw new ArgumentException("Response is not empty and could not be handled.");

            var codeInt = (int)response.StatusCode;
            return (codeInt > 199 && codeInt < 300) ? this.Request.CreateResponse(response.StatusCode, response.Content) : this.Request.CreateErrorResponse(response.StatusCode, response.Message);
        }


        /// <summary>
        /// Helper method to fire back 403 forbidden.
        /// </summary>
        /// <returns></returns>
        public HttpResponseMessage SpitForbidden()
        {
            return this.Request.CreateErrorResponse(HttpStatusCode.Forbidden, "You do not have permission to do this.");
        }

    }
}
