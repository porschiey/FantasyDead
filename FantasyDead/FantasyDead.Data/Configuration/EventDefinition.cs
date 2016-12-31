namespace FantasyDead.Data.Configuration
{
    using Microsoft.WindowsAzure.Storage.Table;
    using System;

    /// <summary>
    /// Defines a type of action or event a character can perform.
    /// PartitionKey = "actions"
    /// Rowkey = guid
    /// </summary>
    public class EventDefinition : TableEntity
    {

        public static string Pkey = "actions";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public EventDefinition()
        {
            this.PartitionKey = Pkey;
            this.RowKey = Guid.NewGuid().ToString();
        }

        public string Id => this.RowKey;
        
        public string Name { get; set; }

        public string Description { get; set; }

        public string SpecialRules { get; set; }

        public double PointValue { get; set; }

        public int CategoryInt { get; set; }        

        /// <summary>
        /// Category for the action.
        /// </summary>
        public EventCategory Category => (EventCategory)this.CategoryInt;
    }


    public enum EventCategory
    {
        Trivial = 0,

        Supportive = 1,

        WalkerKill = 2,

        Moral = 3,

        HumanKill = 4,

        Detrimental = 5,

        Death = 6
    }
}
