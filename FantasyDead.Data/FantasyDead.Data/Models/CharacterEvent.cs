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

        public int ActionId { get; set; }

        public int ModifierId { get; set; }

        public string Notes { get; set; }
    }
}
