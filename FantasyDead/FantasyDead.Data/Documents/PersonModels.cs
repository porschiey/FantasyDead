namespace FantasyDead.Data.Documents
{
    using Configuration;
    using Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    public class Person
    {
        public string Id { get; set; }

        public string PersonId { get { return this.Id; } set { this.Id = value; } }

        public string Username { get; set; }

        public string AvatarPictureUrl { get; set; }

        public double TotalScore { get; set; }

        public List<SocialIdentity> Identities { get; set; }

        public List<CharacterEvent> Events { get; set; }

        [JsonConverter(typeof(DateTimeConvertor))]
        public DateTime JoinedDate { get; set; }

        public int Role { get; set; } //0 = member, 1 = mod, 2 = admin, -1 = banned
    }

    public class SocialIdentity
    {
        public string PlatformUserId { get; set; }

        public string PersonId { get; set; }

        public string PlatformName { get; set; }

        public string Credentials { get; set; }
    }


    public enum PersonRole
    {
        Banned = -1,
        Member = 0,
        Moderator = 1,
        Admin = 2
    }

}
