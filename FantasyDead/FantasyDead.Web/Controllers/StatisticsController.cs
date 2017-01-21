
namespace FantasyDead.Web.Controllers
{
    using Data;
    using Data.Documents;
    using Data.Models;
    using Models;
    using Newtonsoft.Json;
    using Parts;
    using StackExchange.Redis;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;


    /// <summary>
    /// Controller responsible for fetching statistics.
    /// </summary>
    public class StatisticsController : BaseApiController
    {

        private readonly DataContext db;
        private readonly IDatabase cache;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public StatisticsController()
        {
            this.db = new DataContext();
            this.cache = RedisCache.Connection.GetDatabase();
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
        [HttpPost]
        [Route("api/statistics/leaderboard/friends")]
        public async Task<HttpResponseMessage> FriendLeaderboard()
        {
            var person = this.db.GetPerson(this.Requestor.PersonId, false);

            if (person.Friends == null)
                return this.Request.CreateResponse(HttpStatusCode.OK, new List<Person>());

            var ids = person.Friends;
            ids.Add(person.PersonId);
            var lb = await this.db.GetPeopleByList(ids);
            return this.Request.CreateResponse(HttpStatusCode.OK, lb);
        }

        /// <summary>
        /// GET api/statistics/leaderboard/all
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("api/statistics/leaderboard/all/")]
        public async Task<HttpResponseMessage> Leaderboard([FromBody] LeaderboardContinue contToken)
        {
            string tok = null;
            if (contToken?.Token != null)
                tok = contToken.Token;

            var redisKey = tok == null ? "first" : tok;

            try
            {
                if (this.cache.KeyExists(redisKey))
                {
                    var cachedLbJson = this.cache.StringGet(redisKey);
                    var cachedLb = JsonConvert.DeserializeObject<LeaderboardResult>(cachedLbJson);
                    return this.Request.CreateResponse(cachedLb);
                }
            }
            catch (Exception)
            {
                //swallow for now, in favor of continuing
            }

            var lb = await this.db.Leaderboard(tok);

            var cachedLbJsonNew = JsonConvert.SerializeObject(lb);
            this.cache.StringSet(redisKey, cachedLbJsonNew);

            //lb keys also into cache
            await Task.Run(() =>
            {
                try
                {
                    const string lbKeys = "lbKeys";
                    var keysJson = this.cache.StringGet(lbKeys);

                    var allKeys = ((string)keysJson == null) ? new List<string>() : JsonConvert.DeserializeObject<List<string>>(keysJson);

                    if (!allKeys.Contains(redisKey))
                        allKeys.Add(redisKey);

                    keysJson = JsonConvert.SerializeObject(allKeys);
                    this.cache.StringSet(lbKeys, keysJson);
                }
                catch (Exception)
                {
                    //swallow for now, if cache isn't running.
                }
            });

            return this.Request.CreateResponse(HttpStatusCode.OK, lb);
        }
    }
}
