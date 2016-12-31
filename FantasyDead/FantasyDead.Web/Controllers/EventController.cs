namespace FantasyDead.Web.Controllers
{
    using App_Start;
    using Data.Documents;
    using Data.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;

    /// <summary>
    /// Controller responsible for submitting events and calculating scores for users/characters/episodes.
    /// </summary>
    [ApiAuthorization(requiredRole: (int)PersonRole.Moderator)]
    public class EventController : ApiController
    {

        /// <summary>
        /// PUT api/event
        /// Adds an event to the system. DOES NOT CALCULATE STATS.
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("api/event")]
        public HttpResponseMessage AddEvent([FromBody] CharacterEvent ev)
        {
            throw new NotImplementedException();
        }
    }
}
