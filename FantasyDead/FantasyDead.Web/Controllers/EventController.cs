﻿namespace FantasyDead.Web.Controllers
{
    using App_Start;
    using Data;
    using Data.Documents;
    using Data.Models;
    using Parts;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;

    /// <summary>
    /// Controller responsible for submitting events and calculating scores for users/characters/episodes.
    /// </summary>
    [ApiAuthorization(requiredRole: (int)PersonRole.Admin)]
    public class EventController : ApiController
    {

        private readonly DataContext db;
        private readonly PointCalculator calc;

        public EventController()
        {
            this.db = new DataContext();
            this.calc = new PointCalculator(this.db);
        }

        /// <summary>
        /// PUT api/event
        /// Adds an event to the system. DOES NOT CALCULATE STATS FOR SYSTEM.
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("api/event")]
        public HttpResponseMessage UpsertEvent([FromBody] CharacterEvent ev)
        {
            ev = this.calc.CalculateEvent(ev); //only formulates the amount of points this event is worth

            if (string.IsNullOrWhiteSpace(ev.RowKey))
                ev.RowKey = Guid.NewGuid().ToString();

            this.db.AddEvent(ev);
            return this.Request.CreateResponse(HttpStatusCode.Created);
        }

        /// <summary>
        /// DELETE api/event/{eventId}/character/{characterId}
        /// Removes an event from the system. Does not trigger a re-calculation.
        /// </summary>
        /// <param name="characterId"></param>
        /// <param name="eventId"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("api/event/{eventId}/character/{characterId}")]
        public HttpResponseMessage DeleteEvent(string characterId, string eventId)
        {
            this.db.RemoveEvent(characterId, eventId);
            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// GET api/event/calculate/{episodeId}
        /// Runs calculation on an entire episode, scoring characters and rewarding points to users. 
        /// The most data extensive method in the API.
        /// </summary>
        /// <param name="episodeId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/event/calculate/{episodeId}")]
        public HttpResponseMessage Calculate(string episodeId)
        {
            var showData = this.db.FetchShowData().Content as List<Show>;
            var episode = showData.SelectMany(s => s.Seasons.SelectMany(se => se.Episodes)).First(e=>e.Id == episodeId);

            var id = this.calc.StartEpisodeCalculation(episode);
            return this.Request.CreateResponse(HttpStatusCode.OK, id);
        }

        /// <summary>
        /// GET api/event/calculate/progress/{calculationId}
        /// Fetches the progress of an ongoing calculation.
        /// </summary>
        /// <param name="calculationId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/event/progress/{calculationId}")]
        public HttpResponseMessage FetchProgress(string calculationId)
        {
            return this.Request.CreateResponse(HttpStatusCode.OK, this.calc.GetProgress(calculationId));
        }
    }
}
