using FantasyDead.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FantasyDead.Web.Models
{
    public class StatEvent : CharacterEvent
    {
        public StatEvent(CharacterEvent ev)
        {
            this.ActionId = ev.ActionId;

            this.DeathEvent = ev.DeathEvent;
            this.Description = ev.Description;
            this.EpisodeId = ev.EpisodeId;
            this.EpisodeTimestamp = ev.EpisodeTimestamp;

            this.ModifierId = ev.ModifierId;
            this.PartitionKey = ev.PartitionKey;

            this.Points = ev.Points;
            this.RowKey = ev.RowKey;
            this.ShowId = ev.ShowId;
            this.Timestamp = ev.Timestamp;
        }
        public string CharacterName { get; set; }

        public string EpisodeName { get; set; }

        public string ActionType { get; set; }

    }
}