using FantasyDead.Data.Documents;
using FantasyDead.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FantasyDead.Web.Models
{
    public class HistoryItem
    {

        public string EpisodeName { get; set; }

        public string EpisodeId { get; set; }

        public double TotalScore { get; set; }

        public List<CharacterHistoryPick> Picks { get; set; }

        public static List<HistoryItem> EstablishHistory(List<CharacterEventIndex> events, List<Episode> episodes, List<Character> characters)
        {
            var history = new Dictionary<string,HistoryItem>();

            foreach (var ev in events)
            {
                if (!history.ContainsKey(ev.EpisodeId))
                    history.Add(ev.EpisodeId, new HistoryItem
                    {
                        EpisodeId = ev.EpisodeId,
                        EpisodeName = episodes.First(e => e.Id == ev.EpisodeId).Name,
                        Picks = new List<CharacterHistoryPick>(),
                        TotalScore = 0.0
                    });

                var pick = history[ev.EpisodeId].Picks.FirstOrDefault(p => p.CharacterId == ev.CharacterId);
                if (pick == null)
                {
                    var ch = characters.First(c => c.Id == ev.CharacterId);
                    pick = new CharacterHistoryPick
                    {

                        CharacterId = ev.CharacterId,
                        CharacterName = ch.Name,
                        CharacterPictureUrl = ch.PrimaryImageUrl,
                        Events = new List<CharacterEventIndex>(),
                        TotalScore = 0.0
                    };

                    history[ev.EpisodeId].Picks.Add(pick);
                }

                pick.TotalScore += ev.Points;
                pick.Events.Add(ev);
                history[ev.EpisodeId].TotalScore += ev.Points;
            }

            return history.Select(h=>h.Value).ToList();
        }
    }

    public class CharacterHistoryPick
    {
        public string CharacterId { get; set; }
        public string CharacterName { get; set; }

        public string CharacterPictureUrl { get; set; }

        public double TotalScore { get; set; }

        public List<CharacterEventIndex> Events { get; set; }
    }
}