﻿namespace FantasyDead.Web.Controllers
{
    using App_Start;
    using Data;
    using Data.Documents;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;

    /// <summary>
    /// Controller responsible for handling character administrative actions. (Character CRUD)
    /// </summary>
    [ApiAuthorization(requiredRole: (int)PersonRole.Admin)]
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

            if (string.IsNullOrWhiteSpace(character.Id))
            {
                character.RowKey = Guid.NewGuid().ToString();
                character.CreatedDate = DateTime.UtcNow;
            }

            this.db.UpsertConfigurationItem(character);

            return this.Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}
