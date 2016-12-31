using FantasyDead.Data.Documents;

namespace FantasyDead.Web.Models
{
    /// <summary>
    /// DTO object for registering the first time.
    /// </summary>
    public class RegistrationRequest
    {

        public string Username { get; set; }

        public SocialIdentity SocialIdentity { get; set; }
    }
}