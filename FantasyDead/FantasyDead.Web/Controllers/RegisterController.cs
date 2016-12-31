namespace FantasyDead.Web.Controllers
{
    using Crypto;
    using Data;
    using Data.Documents;
    using Data.Models;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;


    /// <summary>
    /// Only API controller that does not enforce Auth. Used to register and check availability.
    /// </summary>
    public class RegisterController : ApiController
    {

        private readonly DataContext db;

        public RegisterController()
        {
            this.db = new DataContext();
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
            var crypto = new Cryptographer();

            req.SocialIdentity.Credentials = crypto.Encrypt(req.SocialIdentity.Credentials);

            var person = new Person
            {
                PersonId = Guid.NewGuid().ToString(),
                Identities = new List<SocialIdentity>() { req.SocialIdentity },
                JoinedDate = DateTime.UtcNow,
                Username = req.Username,
                Events = new List<CharacterEvent>(),
                AvatarPictureUrl = string.Empty,
                Role = (int)PersonRole.Member
            };



            var response = await this.db.Register(person);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                return this.Request.CreateErrorResponse(response.StatusCode, response.Message);
            }
            else
            {
                //create token
                var token = crypto.CreateToken(person.Id, person.Username, person.Role);
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
        public async Task<HttpResponseMessage> Login([FromBody] SocialIdentity id)
        {
            var crypto = new Cryptographer();
            id.Credentials = crypto.Encrypt(id.Credentials);
            var person = this.db.GetPersonBySocialId(id);

            if (person == null)
                return this.Request.CreateErrorResponse(HttpStatusCode.NotFound, "No account found matching those credentials.");

            //check role and creds
            if (person.Role == ((int)PersonRole.Banned))
                return this.Request.CreateErrorResponse(HttpStatusCode.Forbidden, "This account has been banned.");

            var foundId = person.Identities.FirstOrDefault(i => i.PlatformUserId == id.PlatformUserId);
            if (foundId.Credentials != id.Credentials)
                return this.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "No account found matching those credentials.");

            var token = crypto.CreateToken(person.Id, person.Username, person.Role);

            return this.Request.CreateResponse(HttpStatusCode.OK, token);
        }

        /// <summary>
        /// GET api/register/check/{username}
        /// Checks to see if the username is available.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/register/check/{username}")]
        public HttpResponseMessage Check(string username)
        {
            var personId = this.db.GetPersonIdByUsername(username);
            var result = personId == null; //true if AVAILABLE

            return this.Request.CreateResponse(HttpStatusCode.OK, result);
        }
    }
}
