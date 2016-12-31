namespace FantasyDead.Web.Parts
{
    using Data;
    using Data.Configuration;
    using Data.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;

    public class PointCalculator
    {
        private readonly DataContext db;
        /// <summary>
        /// Default constructor.
        /// </summary>
        public PointCalculator(DataContext db)
        {
            this.db = db;
        }


        /// <summary>
        /// Spawns a task that is responsible for calculting all the points for a given episode and rewarding points.
        /// </summary>
        /// <param name="episodeId"></param>
        public void StartEpisodeCalculation(string episodeId, string showId)
        {
            Task.Run(this.CalculateEpisode(episodeId, showId));
        }

        /// <summary>
        /// Calculates the number of points this event is worth to the character.
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        public CharacterEvent CalculateEvent(CharacterEvent ev)
        {
            var eventDefinitions = this.db.FetchEventDefinitions().Content as List<EventDefinition>;
            var eventModifiers = this.db.FetchEventModifiers().Content as List<EventModifier>;

            var thisDef = eventDefinitions.First(d => d.Id == ev.ActionId);
            var thisMod = string.IsNullOrWhiteSpace(ev.ModifierId) ? null : eventModifiers.First(m => m.Id == ev.ModifierId);

            ev.Points = thisDef.PointValue;

            if (thisMod == null)
                return ev;


        }

        /// <summary>
        /// Provides a task (Action) that runs in the background to calculate points for an Episode.
        /// </summary>
        /// <param name="episodeId"></param>
        /// <returns></returns>
        public Action CalculateEpisode(string episodeId, string showId)
        {
            return new Action(() =>
            {
                var epId = episodeId; //re-assigning scope-locally for no amazing reason other than sanity

                //fetch event definition data
                var allCharacters = this.db.FetchCharacters(showId);

                //fetch all events for episode
                var events = this.db.FetchEventsForEpisode(episodeId);

                //formulate list of characters that got points
                var affectedCharacters = events.Select(e => e.CharacterId).Distinct().ToList();

                //calculate those character's points earned in the episode

                //discover who selected those characters
                //reward points to users  

                //discover who slotted characters that died
                //reward points to users  


            });
        }
    }
}