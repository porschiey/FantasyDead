using FantasyDead.Data.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FantasyDead.Web.Models
{
    public class RosterPayload
    {

        public List<RosterSlot> Slots { get; set; }

        public List<Character> Characters { get; set; }

        public Show RelatedShow { get; set; }

        public Episode CurrentEpisode { get; set; }
    }
}