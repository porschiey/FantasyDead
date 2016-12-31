namespace FantasyDead.Crypto
{
    using System;

    public class BearerToken
    {

        public string RawToken { get; set; }
        public string PersonId { get; set; }

        public string Username { get; set; }

        public DateTime Expiration { get; set; }

        public int Role { get; set; }

        public bool IsExpired => Expiration < DateTime.UtcNow;
    }
}
