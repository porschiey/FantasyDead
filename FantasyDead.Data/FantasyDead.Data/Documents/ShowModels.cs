namespace FantasyDead.Data.Documents
{
    using System;
    using System.Collections.Generic;

    public class Show
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Details { get; set; }

        public List<Season> Seasons { get; set; }
    }

    public class Season
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string ShowId { get; set; }

        public List<Episode> Episodes { get; set; }
    }

    public class Episode
    {
        public string Name { get; set; }

        public string ShowId { get; set; }

        public string SeasonId { get; set; }

        public DateTime AirDate { get; set; }

    }
}
