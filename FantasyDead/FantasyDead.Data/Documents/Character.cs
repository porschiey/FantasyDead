namespace FantasyDead.Data.Documents
{
    using Microsoft.WindowsAzure.Storage.Table;
    using System;

    /// <summary>
    /// Class/table entity representing a character.
    /// Pkey = "characters"
    /// Rkey = id
    /// </summary>
    public class Character : TableEntity
    {
        public static string Pkey = "characters";

        public Character()
        {
            this.PartitionKey = Pkey;            
        }

        public string Id => this.RowKey;

        public string Name { get; set; }

        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Nullable.
        /// </summary>
        public string DeadDateIso { get; set; }

        public string Description { get; set; }

        public double TotalScore { get; set; }

        public string ShowId { get; set; }

        public string PrimaryImageUrl { get; set; }

        public int Usage { get; set; }

        public Character Clone()
        {
            var c = this.MemberwiseClone() as Character;
            return c;
        }
    }

    public class CharacterDto : Character
    {
        public Show Show { get; set; }
    }

}
