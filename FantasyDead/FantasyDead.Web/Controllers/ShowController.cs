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
    /// Controller responsible for configuration the shows, seasons, and episodes. (Episode CRUD)
    /// </summary>
    public class ShowController : BaseApiController
    {
        private readonly DataContext db;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ShowController()
        {
            this.db = new DataContext();
        }

        /// <summary>
        /// PUT api/show
        /// Upserts a show configuration.
        /// </summary>
        [HttpPut]
        [Route("api/show")]
        public HttpResponseMessage UpsertShow([FromBody] Show show)
        {
            if (this.Requestor.Role != PersonRole.Admin)
                return this.SpitForbidden();

            if (show == null)
                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "You cannot submit nothing to be a show.");

            //more vallidation here?

            if (string.IsNullOrWhiteSpace(show.Id))
                show.Id = Guid.NewGuid().ToString();

            show.Seasons = show.Seasons.Select(s => this.ForceIdMapping(s, show.Id)).ToList();

            return this.ConvertDbResponse(this.db.UpsertShow(show).Result);
        }

        /// <summary>
        /// PUT api/show/season
        /// Upserts a season configuration.
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [Route("api/show/season")]
        public HttpResponseMessage UpsertSeason([FromBody]Season season)
        {
            if (this.Requestor.Role != PersonRole.Admin)
                return this.SpitForbidden();

            if (season == null || string.IsNullOrWhiteSpace(season.ShowId))
                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Season is null or show id is invalid.");

            season = this.ForceIdMapping(season, season.ShowId);
            return this.ConvertDbResponse(this.db.UpsertSeason(season).Result);
        }

        /// <summary>
        /// PUT api/show/season/episode
        /// Upserts an episode configiuration.
        /// </summary>
        /// <param name="episode"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("api/show/season/episode")]
        public HttpResponseMessage UpsertEpisode([FromBody]Episode episode)
        {
            if (this.Requestor.Role != PersonRole.Admin)
                return this.SpitForbidden();

            if (episode == null || string.IsNullOrWhiteSpace(episode.ShowId) || string.IsNullOrWhiteSpace(episode.SeasonId))
                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Episode is null or missing show/season data.");

            if (string.IsNullOrWhiteSpace(episode.Id))
                episode.Id = Guid.NewGuid().ToString();

            return this.ConvertDbResponse(this.db.UpsertEpisode(episode).Result);
        }

        private Season ForceIdMapping(Season season, string showId)
        {
            if (string.IsNullOrWhiteSpace(season.Id))
            {
                season.Id = Guid.NewGuid().ToString();

                foreach (var ep in season.Episodes)
                {
                    if (string.IsNullOrWhiteSpace(ep.Id))
                        ep.Id = Guid.NewGuid().ToString();

                    ep.ShowId = showId;
                    ep.SeasonId = season.Id;
                }
            }

            season.ShowId = showId; //forcing connection

            return season;
        }
    }
}
