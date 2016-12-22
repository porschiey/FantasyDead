namespace FantasyDead.Data.Documents
{
    using System;

    /// <summary>
    /// Class/document representing a character.
    /// </summary>
    public class Character
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime DeadDate { get; set; }

        public string Description { get; set; }

        public int TotalScore { get; set; }

        public string ShowId { get; set; }
    }

    public class CharacterDto : Character
    {
        public Show Show { get; set; }
    }

}
