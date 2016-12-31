namespace FantasyDead.Data.Models
{
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Pkey = CharacterId
    /// Rkey = Guid
    /// Event describing what a Character has done.
    /// </summary>
    public class CharacterEvent : TableEntity
    {

        public string EpisodeId { get; set; }

        public string ShowId { get; set; }

        public string EpisodeTimestamp { get; set; }

        public string CharacterId => this.PartitionKey;

        public string Id => this.RowKey;

        public double Points { get; set; }

        public string ActionId { get; set; }

        public string ModifierId { get; set; }

        public string Notes { get; set; }
    }


    /// <summary>
    /// Same object as <see cref="CharacterEvent"/> except the PartitionKey is different for searchability's sake.
    /// </summary>
    public class CharacterEventIndex : CharacterEvent
    {
        public new string CharacterId { get; set; }

        public new string EpisodeId => this.PartitionKey;

    }
}
