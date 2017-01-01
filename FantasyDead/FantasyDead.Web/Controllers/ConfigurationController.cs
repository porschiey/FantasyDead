namespace FantasyDead.Web.Controllers
{
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
            throw new NotImplementedException(); //TODO start here
        }
    }
}
