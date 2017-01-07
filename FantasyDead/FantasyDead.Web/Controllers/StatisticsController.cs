
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

        /// <summary>
        /// GET api/statistics/leaderboard/friends
        /// Fetches the leaderboard for the requestor, based on their friends.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/statistics/leaderboard/friends")]
        public HttpResponseMessage FriendLeaderboard()
        {
            var person = this.db.GetPerson(this.Requestor.PersonId);

            if (person.Friends == null)
                return this.Request.CreateResponse(HttpStatusCode.OK, new List<Person>());

            var lb = this.db.GetPeopleByList(person.Friends);
            return this.Request.CreateResponse(HttpStatusCode.OK, lb);
        }

        /// <summary>
        /// GET api/statistics/leaderboard/all
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/statistics/leaderboard/all/")]
        public HttpResponseMessage Leaderboard(string contToken = "")
        {
            if (string.IsNullOrWhiteSpace(contToken))
                contToken = null;

            var lb = this.db.Leaderboard(contToken);

            return this.Request.CreateResponse(HttpStatusCode.OK, lb);
        }
    }
}
