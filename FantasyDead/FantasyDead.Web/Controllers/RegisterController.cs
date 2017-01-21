namespace FantasyDead.Web.Controllers
{
    using App_Start;
    using Crypto;
    using Data;
    using Data.Documents;
    using Data.Models;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.Caching;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using System.Web.Http;


    /// <summary>
    /// Only API controller that does not enforce Auth. Used to register and check availability.
    /// </summary>
    public class RegisterController : ApiController
    {

        private readonly DataContext db;
        private readonly TelemetryClient telemetry;
        private readonly ObjectCache cache;
        private readonly Cryptographer crypto;

        public RegisterController()
        {
            this.telemetry = new TelemetryClient();
            this.crypto = new Cryptographer();
            this.cache = MemoryCache.Default;
            try
            {
                this.db = new DataContext();
            }
            catch (Exception ex)
            {
                this.telemetry.TrackException(ex);
                throw;
            }
        }

        /// <summary>
        /// PUT api/register
        /// Attempts to register a user/person. Is async...
        /// </summary>
        /// <returns>On success, it will return an api token.</returns>
        [HttpPut]
        [Route("api/register")]
        public async Task<HttpResponseMessage> Register([FromBody] RegistrationRequest req)
        {
            if (this.HasRegisteredTooManyTimes())
                return this.Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Too many attempts from this source.");

            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Username))
                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Username and email must be provided.");

            req.SocialIdentity.Credentials = this.crypto.Encrypt(req.SocialIdentity.Credentials);

            var person = new Person
            {
                PersonId = Guid.NewGuid().ToString(),
                Identities = new List<SocialIdentity>() { req.SocialIdentity },
                JoinedDate = DateTime.UtcNow,
                Username = req.Username,
                Events = new List<CharacterEventIndex>(),
                AvatarPictureUrl = string.Empty,
                Role = (int)PersonRole.NewUser,
                Email = req.Email,
                Configuration = new PersonConfiguration()
            };

            return await this.CreatePerson(person);
        }

        private async Task<HttpResponseMessage> CreatePerson(Person person, bool continueDespiteUsername = false)
        {
            var response = await this.db.Register(person, continueDespiteUsername);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                return this.Request.CreateErrorResponse(response.StatusCode, response.Message);
            }
            else
            {
                //create token
                var token = this.crypto.CreateToken(person.Id, person.Username, person.Role);
                return this.Request.CreateResponse(response.StatusCode, token);
            }
        }

        /// <summary>
        /// PUT api/register/login
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("api/register/login")]
        public async Task<HttpResponseMessage> Login([FromBody] SocialIdentity id, bool alreadyEncrypted = false)
        {
            try
            {
                if (!alreadyEncrypted)
                    id.Credentials = this.crypto.Encrypt(id.Credentials);

                var person = this.db.GetPersonBySocialId(id);

                if (person == null)
                    return this.Request.CreateErrorResponse(HttpStatusCode.NotFound, "No account found matching those credentials.");

                //check role and creds
                if (person.Role == ((int)PersonRole.Banned))
                    return this.Request.CreateErrorResponse(HttpStatusCode.Forbidden, "This account has been banned.");

                var foundId = person.Identities.FirstOrDefault(i => i.PlatformUserId == id.PlatformUserId);
                if (foundId.Credentials != id.Credentials)
                    return this.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "No account found matching those credentials.");

                var token = this.crypto.CreateToken(person.Id, person.Username, person.Role);

                return this.Request.CreateResponse(HttpStatusCode.OK, token);
            }
            catch (Exception ex)
            {
                this.telemetry.TrackException(ex);
                throw;
            }
        }

        /// <summary>
        /// PUT api/register/social
        /// Registers a user via a social account. For custom register, use api/register
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("api/register/social")]
        public async Task<HttpResponseMessage> SocialRegister(LoginRequest req)
        {
            if (this.HasRegisteredTooManyTimes())
                return this.Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Too many attempts from this source.");

            if (req == null)
                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Request was empty/null.");

            var platId = string.Empty;
            var user = new Person
            {
                Identities = new List<SocialIdentity>(),
                PersonId = Guid.NewGuid().ToString(),
                Configuration = new PersonConfiguration(),
                Events = new List<CharacterEventIndex>(),
                JoinedDate = DateTime.UtcNow,
                Role = (int)PersonRole.NewUser
            };
            switch (req.Platform)
            {
                case Platform.Twitter:
                    {
                        var keys = req.Token.Split(',');
                        Tweetinvi.Auth.SetUserCredentials("EBJIutCaB6XiNvbGe6oexBYKf", "16XiJIz8j9KKFFNYeAkt5EZEMQqE5Rtc3tPBbFadnxR8VaGWzR", keys[0], keys[1]);
                        var twitterUser = Tweetinvi.User.GetAuthenticatedUser();

                        user.AvatarPictureUrl = twitterUser.ProfileImageUrlHttps;
                        user.Username = twitterUser.ScreenName;
                        user.Email = twitterUser.Email;
                        platId = twitterUser.IdStr;
                        break;
                    }
                case Platform.Facebook:
                    {
                        var fb = new HttpClient { BaseAddress = new Uri("https://graph.facebook.com/v2.2") };
                        var response = fb.GetAsync($"/me?access_token={req.Token}&fields=name,picture,email&format=json").Result;
                        if (!response.IsSuccessStatusCode)
                        {
                            var error = response.Content.ReadAsStringAsync().Result;
                            this.telemetry.TrackTrace($"Facebook Login Error: {error}", SeverityLevel.Warning);
                            return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The fb login failed.");
                        }

                        var json = response.Content.ReadAsStringAsync().Result;
                        dynamic data = System.Web.Helpers.Json.Decode(json);

                        if (!data.picture.data.is_silhouette)
                            user.AvatarPictureUrl = data.picture.data.url;

                        user.Username = data.name;
                        user.Email = data.email;
                        platId = data.id;
                        fb.Dispose();
                        break;
                    }
                case Platform.Google:
                    {
                        using (var ggl = new HttpClient { BaseAddress = new Uri("https://www.googleapis.com/oauth2/v1/") })
                        {
                            var response = ggl.GetAsync($"userinfo?alt=json&access_token={req.Token}").Result;
                            if (!response.IsSuccessStatusCode)
                            {
                                var error = response.Content.ReadAsStringAsync().Result;
                                this.telemetry.TrackTrace($"Google Login Error: {error}", SeverityLevel.Warning);
                                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The google login failed.");
                            }

                            var json = response.Content.ReadAsStringAsync().Result;
                            dynamic data = System.Web.Helpers.Json.Decode(json);
                            user.Username = data.name;
                            user.AvatarPictureUrl = data.picture;
                            user.Email = data.email;
                            platId = data.id;
                        }

                        break;

                    }
                case Platform.Microsoft:
                    {
                        using (var msft = new HttpClient { BaseAddress = new Uri("https://apis.live.net/v5.0/") })
                        {
                            var response = msft.GetAsync($"me?access_token={req.Token}").Result;
                            if (!response.IsSuccessStatusCode)
                            {
                                var error = response.Content.ReadAsStringAsync().Result;
                                this.telemetry.TrackTrace($"Microsoft Login Error: {error}", SeverityLevel.Warning);
                                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The Microsoft login failed.");
                            }

                            var json = response.Content.ReadAsStringAsync().Result;
                            dynamic data = System.Web.Helpers.Json.Decode(json);
                            user.Username = data.name;
                            user.Email = data.emails.preferred;
                            platId = data.id;

                            break;
                        }
                    }
                case Platform.Custom:
                default:
                    {
                        return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid platform.");
                    }
            }

            var socialId = new SocialIdentity { PlatformUserId = platId, PlatformName = req.Platform.ToString(), Credentials = this.crypto.Encrypt(req.Token), PersonId = user.PersonId };
            user.Identities.Add(socialId);

            //login if already exists
            var person = this.db.GetPersonBySocialId(socialId);
            if (person != null)
                return await this.Login(socialId, true);

            return await this.CreatePerson(user, true);
        }


        /// <summary>
        /// GET api/register/check/{username}
        /// Checks to see if the username is available.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/register/check/{username}")]
        [ApiAuthorization]
        public HttpResponseMessage Check(string username)
        {
            var personId = this.db.GetPersonIdByUsername(username);
            var requestor = new SlimPerson(ClaimsPrincipal.Current.Claims.ToList());

            if (requestor.PersonId == personId)
                personId = null;

            var result = personId == null; //true if AVAILABLE

            return this.Request.CreateResponse(HttpStatusCode.OK, result);
        }



        private bool HasRegisteredTooManyTimes()
        {
            var ip = System.Web.HttpContext.Current.Request.UserHostAddress;

            if (string.IsNullOrWhiteSpace(ip))
                return false;

            var policy = new CacheItemPolicy { AbsoluteExpiration = DateTime.UtcNow.AddSeconds(60) };
            var key = $"registering-ip-{ip}";
            var counts = this.cache.Get(key);
            if (counts != null)
            {
                var k = (int)counts;
                k++;
                if (k > 3)
                {
                    return true;
                }
                this.cache.Set(key, k, policy);
            }
            else
            {
                this.cache.Add(key, 1, policy);
            }

            return false;
        }
    }
}
