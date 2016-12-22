namespace FantasyDead.Data.Documents
{
    using Models;
    using System.Collections.Generic;

    public class Person
    {
        public string Id { get; set; }

        public string Username { get; set; }

        public string AvatarPictureUrl { get; set; }

        public double TotalScore { get; set; }

        public List<SocialIdentity> Identities { get; set; }

        public List<CharacterEvent> Events { get; set; }
    }

    public class SocialIdentity
    {
        public string PlatformUserId { get; set; }

        public string PersonId { get; set; }

        public string PlatformName { get; set; }

        public string Credentials { get; set; }
    }

}
