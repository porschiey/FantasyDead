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
        public string Description { get; set; }

        public string EpisodeId { get; set; }

        public string ShowId { get; set; }

        public int EpisodeTimestamp { get; set; }

        public string CharacterId => this.PartitionKey;

        public string Id => this.RowKey;

        public double Points { get; set; }

        public string ActionId { get; set; }

        public string ModifierId { get; set; }

        public string Notes { get; set; }

        public bool DeathEvent { get; set; }


        public static CharacterEvent Copy(CharacterEvent og)
        {
            return new CharacterEvent
            {
                ActionId = og.ActionId,
                PartitionKey = og.CharacterId,
                RowKey = og.RowKey,
                Points = og.Points,
                Description = og.Description,
                DeathEvent = og.DeathEvent,
                EpisodeTimestamp = og.EpisodeTimestamp,
                EpisodeId = og.EpisodeId,
                Notes = og.Notes,
                ShowId = og.ShowId,
                ModifierId = og.ModifierId
            };
        }
    }


    /// <summary>
    /// Same object as <see cref="CharacterEvent"/> except the PartitionKey is different for searchability's sake.
    /// </summary>
    public class CharacterEventIndex : CharacterEvent
    {
        public new string CharacterId { get; set; }

        public new string EpisodeId => this.PartitionKey;

        public static CharacterEventIndex Copy(CharacterEventIndex og)
        {
            return new CharacterEventIndex
            {
                ActionId = og.ActionId,
                PartitionKey = og.PartitionKey,
                RowKey = og.RowKey,
                Points = og.Points,
                Description = og.Description,
                DeathEvent = og.DeathEvent,
                EpisodeTimestamp = og.EpisodeTimestamp,
                CharacterId = og.CharacterId,
                Notes = og.Notes,
                ShowId = og.ShowId,
                ModifierId = og.ModifierId
            };
        }
    }
}
