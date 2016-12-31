namespace FantasyDead.Web.Controllers
{
    using Crypto;
    using Data;
    using Data.Documents;
    using Data.Models;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
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
        public HttpResponseMessage Register([FromBody] RegistrationRequest req)
        {
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

            var response = this.db.Register(person).Result;

            if (response.StatusCode != HttpStatusCode.Created)
            {
                return this.Request.CreateErrorResponse(response.StatusCode, response.Message);
            }
            else
            {
                //create token
                var token = new Cryptographer().CreateToken(person.Id, person.Username, person.Role);
                return this.Request.CreateResponse(response.StatusCode, token);
            }
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
