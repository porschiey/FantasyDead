namespace FantasyDead.Web.Parts
{
    using Data;
    using Data.Configuration;
    using Data.Documents;
    using Data.Models;
    using System;
    using System.Collections.Concurrent;
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
        public void StartEpisodeCalculation(Episode episode)
        {
            Task.Run(this.CalculateEpisode(episode));
        }

        /// <summary>
        /// Calculates the number of points this event is worth to the character. This also populates the description value for the event.
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        public CharacterEvent CalculateEvent(CharacterEvent ev)
        {
            var eventDefinitions = this.db.FetchEventDefinitions().Content as List<EventDefinition>;
            var eventModifiers = this.db.FetchEventModifiers().Content as List<EventModifier>;

            var thisChar = (this.db.FetchCharacters(ev.ShowId).Content as List<Character>).FirstOrDefault(c => c.Id == ev.CharacterId);
            var thisDef = eventDefinitions.FirstOrDefault(d => d.Id == ev.ActionId);
            var thisMod = string.IsNullOrWhiteSpace(ev.ModifierId) ? null : eventModifiers.FirstOrDefault(m => m.Id == ev.ModifierId);

            if (thisChar == null || thisDef == null)
            {
                //likely an event where the character/definition no longer exists, and thus should not be calculated.
                ev.Description = "The event related to this character is no longer valid.";
                ev.Points = 0; 
                return ev; //return event as is.
            }

            ev.Points = thisDef.PointValue;

            ev.Description = $"{thisChar.Name} - {thisDef.Name} for {ev.Points}";

            if (thisMod != null)
            {
                //note: this switch case and subsequent enum really isn't necessary to peform modification math, as the value itself can be applied straight to the core value. 
                //The enum exists for ease of the UI's display modes. 

                switch (thisMod.Type)
                {
                    case ModificationType.Add:
                        {
                            ev.Points += thisMod.ModificationValue;
                            ev.Description += $", plus {thisMod.ModificationValue} points w/ mod {thisMod.Name}.";
                            break;
                        }
                    case ModificationType.Subtract:
                        {
                            ev.Points -= thisMod.ModificationValue;
                            ev.Description += $", minus {thisMod.ModificationValue} points w/ mod {thisMod.Name}.";
                            break;
                        }
                    case ModificationType.Percentage:
                        {
                            ev.Points *= thisMod.ModificationValue;
                            ev.Description += $", plus {Math.Round((1 - thisMod.ModificationValue) * 100, 1)}% more points w/ mod {thisMod.Name}.";
                            break;
                        }
                    default:
                        throw new NotSupportedException("Unknown modification type");
                }
            }

            ev.Description += $". Total points attributed: {ev.Points}";

            return ev;
        }

        /// <summary>
        /// Provides a task (Action) that runs in the background to calculate points for an Episode.
        /// </summary>
        /// <param name="episodeId"></param>
        /// <returns></returns>
        public Action CalculateEpisode(Episode episode)
        {
            return new Action(() =>
            {
                var epId = episode.Id; //re-assigning scope-locally for no amazing reason other than sanity

                //fetch event definition data
                var allCharacters = this.db.FetchCharacters(episode.ShowId);

                //fetch all events for episode
                var events = this.db.FetchEventsForEpisode(epId);

                //formulate list of characters that got points
                //and calculate those character's points earned in the episode
                var picksDictionary = new Dictionary<string, List<EpisodePick>>();
                var characterEventDictionary = new Dictionary<string, List<CharacterEventIndex>>();

                var deathEvents = new List<CharacterEvent>();

                foreach (var ev in events)
                {
                    if (!characterEventDictionary.ContainsKey(ev.CharacterId))
                    {
                        picksDictionary.Add(ev.CharacterId, this.db.FetchPicksForCharacter(ev.CharacterId, ev.EpisodeId)); //who picked this character
                        characterEventDictionary.Add(ev.CharacterId, new List<CharacterEventIndex>());
                    }
                    characterEventDictionary[ev.CharacterId].Add(ev);

                    if (ev.DeathEvent)
                        deathEvents.Add(ev);
                }

                var affectedCharacters = events.Select(e => e.CharacterId).Distinct().ToList();
                //reward points to users  

                var users = new ConcurrentDictionary<string, List<CharacterEvent>>();

                Parallel.ForEach(picksDictionary, (picks) =>
                {
                    var characterId = picks.Key;
                    var characterEventsThisEpisode = characterEventDictionary[characterId];

                    foreach (var slot in picks.Value)
                    {

                        var personId = slot.PersonId;
                        if (!users.ContainsKey(personId))
                        {
                            users.GetOrAdd(personId, new List<CharacterEvent>());

                            if (episode.Calculated)
                            {
                                //this episode has already been calculated, but clearly a request has come in to re-calculate.
                                //thus, we've got to revoke previous events already calculated to start over
                                this.db.RevokeEventsFromPerson(epId, personId);
                            }
                        }

                        if (slot.SlotType == (int)SlotType.Death && characterEventsThisEpisode.Any(e => e.DeathEvent)) //is this a death slot, and did the character die?
                        {
                            var deathEvent = characterEventsThisEpisode.First(e => e.DeathEvent);
                            var bonus = CharacterEvent.Copy(deathEvent);
                            bonus.Points = 100;
                            bonus.Description = "Bonus points were awarded because this character was slotted to die.";
                            users[personId].Add(bonus);
                            continue; // don't do normal additive since it's a death slot
                        }
                        users[personId].AddRange(characterEventsThisEpisode); //add character events to that user                        
                    }
                });

                Parallel.ForEach(users, (u) =>
                {
                    this.db.AddEventsToPerson(u.Value, u.Key); //this also tallies all the person's events to generate total score
                });

                episode.Calculated = true;
                episode.CalculationDate = DateTime.UtcNow;
                this.db.UpsertEpisode(episode).Wait();
            });
        }
    }
}