namespace FantasyDead.Web.App_Start
{
    using Crypto;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Http;
    using System.Web.Http.Controllers;
    using System.Web.Http.Filters;

    /// <summary>
    /// Backend authorization for the API commands.
    /// </summary>
    public class ApiAuthorization : AuthorizeAttribute
    {

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            /// START HERE, LOOK AT WTA AUTH
            var req = actionContext.Request;

            if (req.Headers.Authorization.Scheme != "Bearer" || string.IsNullOrWhiteSpace(req.Headers.Authorization.Parameter))
            {
                actionContext.Response = req.CreateErrorResponse(HttpStatusCode.Unauthorized, "Invalid Authorization, expected bearer token.");
                return;
            }

            var token = req.Headers.Authorization.Parameter;

            var crypto = new Cryptographer();

            var latchKey = crypto.DecipherToken(token);
            if (latchKey == null)


                base.OnAuthorization(actionContext);
        }
    }
}