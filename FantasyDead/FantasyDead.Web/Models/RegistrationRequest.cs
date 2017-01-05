using FantasyDead.Data.Documents;

namespace FantasyDead.Web.Models
{
    /// <summary>
    /// DTO object for registering the first time.
    /// </summary>
    public class RegistrationRequest
    {

        public string Username { get; set; }

        public string Email { get; set; }

        public SocialIdentity SocialIdentity { get; set; }
    }



    public enum Platform
    {
        Twitter = 0,
        Facebook = 1,
        Google = 2,
        Microsoft = 3,
        Custom = 4
    }

    public class LoginResponse
    {
        public bool Register { get; set; }
    }


    public class LoginRequest
    {
        public Platform Platform { get; set; }

        public string Token { get; set; }


    }
}