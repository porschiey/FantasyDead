using FantasyDead.Data.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FantasyDead.Data.Models
{
    public class LeaderboardResult
    {
        public List<Person> People { get; set; }

        public string ContinuationToken { get; set; }
    }
}