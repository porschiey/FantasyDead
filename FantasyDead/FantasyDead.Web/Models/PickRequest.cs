using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FantasyDead.Web.Models
{
    public class PickRequest
    {
        public string CharacterId { get; set; }

        public string SwappingWithCharacterId { get; set; }

        public string ShowId { get; set; }

        public int SlotType { get; set; }
    }
}