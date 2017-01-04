
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
    /// Controller responsible for fetching statistics.
    /// </summary>
    public class StatisticsController : BaseApiController
    {

        private readonly DataContext db;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public StatisticsController()
        {
            this.db = new DataContext();
        }


        /// <summary>
        /// GET api/event/list/{characterId}
        /// Lists all of the events for a character.
        /// </summary>
        /// <param name="characterId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/statistics/events/character/{characterId}")]
        public HttpResponseMessage ListEventsByCharacter(string characterId)
        {
            var events = this.db.FetchEventsForCharacter(characterId);
            return this.Request.CreateResponse(HttpStatusCode.OK, events);
        }

        /// <summary>
        /// GET api/event/list/{episodeId}
        /// Lists all of the events for an episode.
        /// </summary>
        /// <param name="characterId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/statistics/events/episode/{episodeId}")]
        public HttpResponseMessage ListEventsByEpisode(string episodeId)
        {
            var events = this.db.FetchEventsForEpisode(episodeId);
            return this.Request.CreateResponse(HttpStatusCode.OK, events);
        }
    }
}
