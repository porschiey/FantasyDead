namespace FantasyDead.Web.Controllers
{
    using App_Start;
    using Data;
    using FantasyDead.Data.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;

    /// <summary>
    /// Controller responsible for changing the configuration.
    /// </summary>
    public class ConfigurationController : BaseApiController
    {

        private readonly DataContext db;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ConfigurationController()
        {
            this.db = new DataContext();
        }

        /// <summary>
        /// PUT api/configuration/definition
        /// Adds or updates a configuration in the data store.
        /// </summary>
        /// <param name="evDef"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("api/configuration/definition")]
        [ApiAuthorization(requiredRole: 2)]
        public HttpResponseMessage UpsertEventDefinition([FromBody] EventDefinition evDef)
        {
            if (string.IsNullOrWhiteSpace(evDef.ShowId))
                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An event must belong to a show. The ShowID was null.");

            if (string.IsNullOrWhiteSpace(evDef.RowKey))
                evDef.RowKey = Guid.NewGuid().ToString();

            this.db.UpsertConfigurationItem(evDef);
            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// PUT api/configuration/modifier
        /// Adds or updates a configuration in the data store.
        /// </summary>
        /// <param name="evMod"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("api/configuration/modifier")]
        [ApiAuthorization(requiredRole: 2)]
        public HttpResponseMessage UpsertModifier([FromBody] EventModifier evMod)
        {
            if (string.IsNullOrWhiteSpace(evMod.ShowId))
                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An modifier must belong to a show. The ShowID was null.");

            if (string.IsNullOrWhiteSpace(evMod.RowKey))
                evMod.RowKey = Guid.NewGuid().ToString();

            this.db.UpsertConfigurationItem(evMod);
            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// DELETE api/configuration/definition/{id}
        /// Removes a event definition from the system.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("api/configuration/definition/{id}")]
        [ApiAuthorization(requiredRole: 2)]
        public HttpResponseMessage DeleteEventDefinition(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The id is null or empty");

            var def = (this.db.FetchEventDefinitions().Content as List<EventDefinition>).FirstOrDefault(d => d.Id == id);
            if (def == null)
                return this.Request.CreateErrorResponse(HttpStatusCode.NotFound, "That event definition could not be found.");

            this.db.DeleteConfiguration(def);
            return this.Request.CreateResponse(HttpStatusCode.OK);
        }


        /// <summary>
        /// DELETE api/configuration/modifier/{id}
        /// Removes a event modifier from the system.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("api/configuration/modifier/{id}")]
        [ApiAuthorization(requiredRole: 2)]
        public HttpResponseMessage DeleteEventModifier(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The id is null or empty");

            var mod = (this.db.FetchEventModifiers().Content as List<EventModifier>).FirstOrDefault(d => d.Id == id);
            if (mod == null)
                return this.Request.CreateErrorResponse(HttpStatusCode.NotFound, "That event modifier could not be found.");

            this.db.DeleteConfiguration(mod);
            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// GET api/configuration/definitions/{showId}
        /// Lists all the definitions for a given show.
        /// </summary>
        /// <param name="showId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/configuration/definitions/{showId}")]
        public HttpResponseMessage FetchAllDefinitions(string showId)
        {
            return this.ConvertDbResponse(this.db.FetchEventDefinitions(showId));
        }

        /// <summary>
        /// GET api/configuration/modifiers/{showId}
        /// Lists all the modifiers for a given show.
        /// </summary>
        /// <param name="showId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/configuration/modifiers/{showId}")]
        public HttpResponseMessage FetchAllModifiers(string showId)
        {
            return this.ConvertDbResponse(this.db.FetchEventModifiers(showId));
        }

        /// <summary>
        /// GET api/configuration/shows
        /// Lists all the basic show data.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/configuration/shows")]
        public HttpResponseMessage FetchShowData()
        {
            return this.ConvertDbResponse(this.db.FetchShowData());
        }

        /// <summary>
        /// GET api/configuration/characters/{showId}
        /// Lists all the characters of a show.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/configuration/characters/{showId}")]
        public HttpResponseMessage FetchCharacters(string showId)
        {
            return this.ConvertDbResponse(this.db.FetchCharacters(showId));
        }
    }
}
