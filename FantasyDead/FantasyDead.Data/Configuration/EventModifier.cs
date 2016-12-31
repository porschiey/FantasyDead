namespace FantasyDead.Data.Configuration
{
    using Microsoft.WindowsAzure.Storage.Table;
    using System;

    /// <summary>
    /// A modifier that can be applied to any given event.
    /// Pkey = "modifiers"
    /// Rkey = guid
    /// </summary>
    public class EventModifier : TableEntity
    {
        public static string Pkey = "modifiers";
        public EventModifier()
        {
            this.PartitionKey = Pkey;
            this.RowKey = Guid.NewGuid().ToString();
        }

        public string Id => this.RowKey;

        public string Name { get; set; }

        public string Description { get; set; }

        public string SpecialRules { get; set; }

        public double ModificationValue { get; set; }

        public int ModificationTypeInt { get; set; }

        public ModificationType Type => (ModificationType)this.ModificationTypeInt;
    }

    public enum ModificationType
    {
        Add = 0,

        Subtract = 1,

        Percentage = 2
    }
}
