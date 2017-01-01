using FantasyDead.Data.Configuration;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyDead.Data.Documents
{

    /// <summary>
    /// Represents the user's selection for a given week.
    /// </summary>
    public class EpisodePick
    {
        [JsonProperty(PropertyName = "id")]
        public string Id
        {
            get
            {
                return $"{this.PersonId}:{this.EpisodeId}:{this.CharacterId}";
            }
            set
            {
                var props = value.Split(':');
                this.PersonId = props[0];
                this.EpisodeId = props[1];
                this.CharacterId = props[2];
            }
        }

        public string EpisodeId { get; set; }

        public string ShowId { get; set; }

        public string PersonId { get; set; }

        public string CharacterId { get; set; }

        public int SlotType { get; set; }

        [JsonConverter(typeof(DateTimeConvertor))]
        public DateTime SlottedDate { get; set; }
    }


    public enum SlotType
    {

        Classic = 0,

        Death = 1
    }
}
