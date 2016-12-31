namespace FantasyDead.Web.Controllers
{
    using Data;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;

    /// <summary>
    /// Controller responsible for all Person CRUD actions
    /// </summary>
    public class PersonController : BaseApiController
    {

        private readonly DataContext db;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PersonController()
        {
            this.db = new DataContext();
        }


        /// <summary>
        /// Retrieves the requestor's profile.
        /// Database caches this result for 30 seconds.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/person")]
        public HttpResponseMessage Profile()
        {
            var person = this.db.GetPerson(this.Requestor.PersonId);
            return this.Request.CreateResponse(HttpStatusCode.OK, person);
        }

        /// <summary>
        /// Retrieves another user's profile.
        /// Database caches this result for 30 seconds.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/person/{id}")]
        public HttpResponseMessage Profile(string id)
        {
            var person = this.db.GetPerson(id);
            return this.Request.CreateResponse(HttpStatusCode.OK, person);
        }
    }
}
