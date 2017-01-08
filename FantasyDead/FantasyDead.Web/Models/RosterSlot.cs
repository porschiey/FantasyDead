using FantasyDead.Data.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FantasyDead.Web.Models
{
    public class RosterSlot
    {
        public string Id { get; set; }

        public EpisodePick Pick { get; set; }

        public bool DeathSlot { get; set; }

        public bool Occupied { get; set; }

        public string CharacterName { get; set; }

        public string CharacterPictureUrl { get; set; }

        public string EpisodeId { get; set; }
    }
}