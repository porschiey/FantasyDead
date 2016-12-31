namespace FantasyDead.Web.Controllers
{
    using Data;
    using Data.Documents;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;

    /// <summary>
    /// Controller responsible for handling character administrative actions. (Character CRUD)
    /// </summary>
    public class CharacterController : BaseApiController
    {
        private readonly DataContext db;

        /// <summary>
        /// Default.
        /// </summary>
        public CharacterController()
        {
            this.db = new DataContext();
        }


        /// <summary>
        /// PUT api/character
        /// Adds or updates a character in the system.
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [Route("api/character")]
        public HttpResponseMessage CreateOrUpdate([FromBody] Character character)
        {
            if (this.Requestor.Role != PersonRole.Admin)
                return this.SpitForbidden();

            if (string.IsNullOrWhiteSpace(character.Id))
                character.RowKey = Guid.NewGuid().ToString();

            this.db.UpsertConfigurationItem(character);

            return this.Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}
