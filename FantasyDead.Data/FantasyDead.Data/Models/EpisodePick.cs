using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyDead.Data.Models
{

    /// <summary>
    /// Represents the user's selection for a given week.
    /// Pkey - CharacterId (So event engine can pull all picks when a character performs an event)
    /// Rkey - {EpisodeId}:{PersonId}:{SlotType}  -- to keep picks unique
    /// </summary>
    public class EpisodePick : TableEntity
    {
        
        /// <summary>
        /// Helper method to produce a Pkey.
        /// </summary>
        /// <param name="charId"></param>
        /// <returns></returns>
        public static string Pkey(string charId)
        {
            return charId;
        }       
        
        public static string Rkey(string episodeId, string personId, int slotType)
        {
            return $"{episodeId}:{personId}:{slotType}";
        }

        public string EpisodeId => this.RowKey.Split(':')[0];

        public string ShowId { get; set; }

        public string PersonId => this.RowKey.Split(':')[1];

        public string CharacterId => this.PartitionKey;

        public int SlotType => Convert.ToInt32(this.RowKey.Split(':')[2]);

        public DateTime SlottedDate { get; set; }
    }
}
