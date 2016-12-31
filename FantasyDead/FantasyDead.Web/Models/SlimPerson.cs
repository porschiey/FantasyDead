
namespace FantasyDead.Web.Models
{
    using FantasyDead.Data.Documents;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;

    /// <summary>
    /// API object to hold an identity of a requestor (more usable object than a list of Claims)
    /// </summary>
    public class SlimPerson
    {

        /// <summary>
        /// Default constructor. WARNING: Will throw if claims are missing or invalid.
        /// </summary>
        /// <param name="claims"></param>
        public SlimPerson(List<Claim> claims)
        {
                this.Username = claims.First(c => c.Type == ClaimTypes.Name).Value;
                this.PersonId = claims.First(c => c.Type == "PersonId").Value;
                this.Role = (PersonRole)Convert.ToInt32(claims.First(c => c.Type == ClaimTypes.Role).Value);
                this.TokenExpiration = DateTime.Parse(claims.First(c => c.Type == ClaimTypes.Expiration).Value);
        }

        public string Username { get; set; }

        public string PersonId { get; set; }

        public PersonRole Role { get; set; }

        public DateTime TokenExpiration { get; set; }
    }
}