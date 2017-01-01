namespace FantasyDead.Web.Controllers
{
{
    using Data;
    using FantasyDead.Data.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;

    /// <summary>
    /// Controller responsible for changing the configuration.
    /// </summary>
    public class ConfigurationController : ApiController
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
        public HttpResponseMessage UpsertEventDefinition([FromBody] EventDefinition evDef)
        {

            if (!string.IsNullOrWhiteSpace(evDef.RowKey))
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
        public HttpResponseMessage UpsertModifier([FromBody] EventModifier evMod)
        {

            if (!string.IsNullOrWhiteSpace(evMod.RowKey))
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
    }
}
