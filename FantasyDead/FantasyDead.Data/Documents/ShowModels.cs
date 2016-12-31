namespace FantasyDead.Data.Documents
{
    using Configuration;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    public class Show
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public string Name { get; set; }

        public string Details { get; set; }

        public List<Season> Seasons { get; set; }
    }

    public class Season
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public string Name { get; set; }

        [JsonConverter(typeof(DateTimeConvertor))]
        public DateTime StartDate { get; set; }

        [JsonConverter(typeof(DateTimeConvertor))]
        public DateTime EndDate { get; set; }

        public string ShowId { get; set; }

        public List<Episode> Episodes { get; set; }
    }

    public class Episode
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Name { get; set; }

        public string ShowId { get; set; }

        public string SeasonId { get; set; }

        [JsonConverter(typeof(DateTimeConvertor))]
        public DateTime AirDate { get; set; }

        [JsonConverter(typeof(DateTimeConvertor))]
        public DateTime LockDate { get; set; }

    }
}
