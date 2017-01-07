namespace FantasyDead.Web.Controllers
{
    using Data;
    using Data.Documents;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;

    /// <summary>
    /// Controller responsible for setting up a person's roster for an episode.
    /// </summary>
    public class RosterController : BaseApiController
    {
        private readonly DataContext db;

        public RosterController()
        {
            this.db = new DataContext();
        }

        /// <summary>
        /// PUT api/roster/pick/{characterId}
        /// Attempts to slot a character in for the currently active week.
        /// </summary>
        /// <param name="characterId"></param>
        /// <returns>
        /// 201 - character slotted successfully
        /// 409 - character is already slotted or been used too many times
        /// </returns>
        [HttpPut]
        [Route("api/roster/pick/show/{showId}/character/{characterId}/slot/{slotType}")]
        public async Task<HttpResponseMessage> SetPick(string characterId, string showId, int slotType)
        {
            var character = (this.db.FetchCharacters(showId).Content as List<Character>).FirstOrDefault(c => c.Id == characterId);

            if (character == null)
                return this.Request.CreateErrorResponse(HttpStatusCode.NotFound, "Cannot slot character: they don't exist.");

            //currently open episode
            var openEp = this.db.FetchNextAvailableEpisode(showId);
            if (openEp == null)
                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "There is no currently open episode.");

            var epPick = new EpisodePick
            {
                ShowId = showId,
                PersonId = this.Requestor.PersonId,
                CharacterId = characterId,
                EpisodeId = openEp.Id,
                SlotType = slotType,
                SlottedDate = DateTime.UtcNow
            };

            return this.ConvertDbResponse(await this.db.PushEpisodePick(epPick));
        }

        /// <summary>
        /// GET api/roster/picks
        /// Fetches all of the requestor's slotted characters for any show/episode.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/roster/picks")]
        public HttpResponseMessage GetPicks()
        {
            var picks = this.db.GetEpisodePicks(this.Requestor.PersonId);
            return this.Request.CreateResponse(HttpStatusCode.OK, picks);
        }
    }
}
