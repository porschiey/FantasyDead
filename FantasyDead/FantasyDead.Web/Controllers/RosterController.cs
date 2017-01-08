namespace FantasyDead.Web.Controllers
{
    using Data;
    using Data.Documents;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
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

            var dbReponse = await this.db.PushEpisodePick(epPick);

            if (dbReponse.StatusCode != HttpStatusCode.OK)
                return this.ConvertDbResponse(dbReponse);

            var slot = new RosterSlot
            {
                Pick = epPick,
                CharacterName = character.Name,
                CharacterPictureUrl = character.PrimaryImageUrl,
                EpisodeId = openEp.Id,
                Occupied = true,
                DeathSlot = slotType == (int)SlotType.Death,
                Id = Guid.NewGuid().ToString()
            };

            return this.Request.CreateResponse(HttpStatusCode.OK, slot);
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

        /// <summary>
        /// GET api/roster/bulk
        /// Fetches everything the roster page needs to use to populate.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/roster/bulk")]
        public HttpResponseMessage Bulk()
        {
            var payload = new RosterPayload();
            var picks = this.db.GetEpisodePicks(this.Requestor.PersonId).Content as List<EpisodePick>;

            payload.RelatedShow = (this.db.FetchShowData().Content as List<Show>)[0]; //scoping down to first show for now

            payload.Characters = (this.db.FetchCharacters(payload.RelatedShow.Id).Content as List<Character>)
                .Where(c => c.DeadDateIso == null).ToList();

            payload.CurrentEpisode = this.db.FetchNextAvailableEpisode(payload.RelatedShow.Id);

            payload.Slots = new List<RosterSlot>();
            foreach (var p in picks)
            {
                var slot = new RosterSlot
                {
                    DeathSlot = (p.SlotType == (int)SlotType.Death),
                    Pick = p
                };

                var character = payload.Characters.FirstOrDefault(c => c.Id == p.CharacterId);
                if (character == null)
                {
                    //bad selection, character has already died (likely),
                    //needs to be revoked


                    continue;
                }

                slot.CharacterName = character.Name;
                slot.CharacterPictureUrl = character.PrimaryImageUrl;
                slot.Occupied = true;
                slot.EpisodeId = p.EpisodeId;
                slot.Id = Guid.NewGuid().ToString();
                payload.Slots.Add(slot);
            }

            var classicSlots = Convert.ToInt32(ConfigurationManager.AppSettings["classicSlots"]);
            var deathSlots = Convert.ToInt32(ConfigurationManager.AppSettings["deathSlots"]);

            while (payload.Slots.Count(s => !s.DeathSlot) < classicSlots)
            {
                payload.Slots.Add(new RosterSlot
                {
                    CharacterName = "Not Selected",
                    Occupied = false,
                    DeathSlot = false,
                    CharacterPictureUrl = "images/slotEmpty.png",
                    Id = Guid.NewGuid().ToString(),
                    EpisodeId = payload.CurrentEpisode.Id
                });
            }
            while (payload.Slots.Count(s => s.DeathSlot) < deathSlots)
            {
                payload.Slots.Add(new RosterSlot
                {
                    Occupied = false,
                    DeathSlot = true,
                    CharacterPictureUrl = "images/deathSlotEmpty.png",
                    Id = Guid.NewGuid().ToString(),
                    EpisodeId = payload.CurrentEpisode.Id
                });
            }

            return this.Request.CreateResponse(HttpStatusCode.OK, payload);
        }
    }
}
