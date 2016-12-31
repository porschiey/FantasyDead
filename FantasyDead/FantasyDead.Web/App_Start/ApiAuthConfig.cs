namespace FantasyDead.Web.App_Start
{
    using Crypto;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;
    using System.Web.Http.Filters;

    /// <summary>
    /// Backend authorization for the API commands.
    /// </summary>
    public class ApiAuthorization : Attribute, IAuthenticationFilter
    {
        private readonly TelemetryClient telemetry;

        public ApiAuthorization()
        {
            this.telemetry = new TelemetryClient();
        }

        public bool AllowMultiple => false;

        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var header = context.Request.Headers.Authorization;

            if (header.Scheme != "Bearer" || string.IsNullOrWhiteSpace(header.Parameter))
            {
                context.ErrorResult = new AuthenticationFailureResult("Invalid Headers", context.Request);
                this.telemetry.TrackTrace($"API Attempt failure, reason: 401: invalid headers", SeverityLevel.Warning);
                return;
            }

            var token = header.Parameter;

            var crypto = new Cryptographer();

            var latchKey = crypto.DecipherToken(token);
            if (latchKey == null)
            {
                context.ErrorResult = new AuthenticationFailureResult("Unauthorized - invalid or malformed token.", context.Request);
                this.telemetry.TrackTrace($"API Attempt failure, reason: 401: invalid or malformed token", SeverityLevel.Warning);
                return;
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, latchKey.Username),
                new Claim(ClaimTypes.Role, latchKey.Role.ToString(), ClaimValueTypes.Integer32),
                new Claim("PersonId", latchKey.PersonId),
                new Claim(ClaimTypes.Expiration, latchKey.Expiration.ToString(), ClaimValueTypes.DateTime)
            };

            var id = new ClaimsIdentity(claims);
            context.Principal = new ClaimsPrincipal(id);
        }

        public async Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            //blank on purpose
        }
    }


    public class AuthenticationFailureResult : IHttpActionResult
    {
        public AuthenticationFailureResult(string reasonPhrase, HttpRequestMessage request)
        {
            ReasonPhrase = reasonPhrase;
            Request = request;
        }

        public string ReasonPhrase { get; private set; }

        public HttpRequestMessage Request { get; private set; }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute());
        }

        private HttpResponseMessage Execute()
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            response.RequestMessage = Request;
            response.ReasonPhrase = ReasonPhrase;
            return response;
        }
    }
}