using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FantasyDead.Data.Models
{
    public class LeaderboardItem
    {

        public string PersonId { get; set; }

        public string Username { get; set; }

        public string AvatarUrl { get; set; }

        public int CurrentRank { get; set; }

        public int PreviousRank { get; set; }

        public double PreviousEpScore { get; set; }

        public double TotalScore => this.EpisodeScores.Sum(e => e.EpisodeScore);

        public List<LeaderboardEpisodeItem> EpisodeScores { get; set; }
    }

    public class LeaderboardEpisodeItem
    {
        public string EpisodeId { get; set; }

        public string EpisodeName { get; set; }

        public double EpisodeScore { get; set; }

        public DateTime EpisodeDate { get; set; }
    }
}