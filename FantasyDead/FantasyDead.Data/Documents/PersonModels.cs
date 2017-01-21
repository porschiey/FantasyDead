namespace FantasyDead.Data.Documents
{
    using Configuration;
    using Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    public class Person
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public string PersonId { get { return this.Id; } set { this.Id = value; } }

        public string Username { get; set; }

        public string AvatarPictureUrl { get; set; }

        public double TotalScore { get; set; }

        public string Email { get; set; }

        public List<SocialIdentity> Identities { get; set; }

        public List<CharacterEventIndex> Events { get; set; }

        [JsonConverter(typeof(DateTimeConvertor))]
        public DateTime JoinedDate { get; set; }

        public string PushNotificationData { get; set; }

        public int Role { get; set; } //0 = member, 1 = NewUser, 2 = admin, -1 = banned

        public PersonConfiguration Configuration { get; set; }

        public List<string> Friends { get; set; }

        public Person StripCreds()
        {
            var p = new Person()
            {
                Id = this.Id,
                Username = this.Username,
                AvatarPictureUrl = this.AvatarPictureUrl,
                TotalScore = this.TotalScore,
                Email = this.Email,
                Identities = new List<SocialIdentity>(),
                Events = this.Events,
                JoinedDate = this.JoinedDate,
                PushNotificationData = this.PushNotificationData,
                Role = this.Role,
                Configuration = this.Configuration,
                Friends = this.Friends
            };


            foreach (var cred in this.Identities)
            {
                var id = new SocialIdentity
                {
                    PlatformName = cred.PlatformName,
                    PlatformUserId = cred.PlatformUserId,
                    PersonId = cred.PersonId,
                };
                p.Identities.Add(id);
            }

            return p;
        }
    }


    public class PersonConfiguration
    {
        public bool ReceiveNotifications { get; set; }

        public int DeadlineReminderHours { get; set; }

        public bool NotifyWhenScored { get; set; }

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
        NewUser = 1,
        Admin = 2
    }

}
