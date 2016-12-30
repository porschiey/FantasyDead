using FantasyDead.Data.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Configuration;
using System.Runtime.Caching;

namespace FantasyDead.Data
{

    /// <summary>
    /// Primary data access context for the system.
    /// </summary>
    public class DataContext
    {

        private readonly CloudTable stats;
        private readonly ObjectCache cache;

        private readonly DocumentClient db;
        private static string peopleCol = "people";
        private static string showsCol = "shows";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DataContext()
        {
            this.cache = MemoryCache.Default;

            this.db = new DocumentClient(new Uri("https://fantasydead.documents.azure.com:443/"), ConfigurationManager.AppSettings["docuDbKey"]);


        }



        #region CRUD

        /// <summary>
        /// Adds or updates a person(user)
        /// </summary>
        /// <param name="person"></param>
        public void UpsertPerson(Person person)
        {
            throw new NotImplementedException();

        }

        /// <summary>
        /// Bans a person(user) from the app.
        /// </summary>
        /// <param name="personId"></param>
        public void BanPerson(string personId)
        {
            throw new NotImplementedException();

        }

        /// <summary>
        /// Retrieves a person(user)'s profile.
        /// Cached for 30 seconds based on personId.
        /// </summary>
        /// <param name="personId"></param>
        /// <returns></returns>
        public Person GetPerson(string personId)
        {
            throw new NotImplementedException();

        }

        /// <summary>
        /// Retrieves a person(user)'s profile via a known social identity.
        /// Cached for 30 seconds based on social identity.
        /// </summary>
        /// <param name="socialId"></param>
        /// <returns></returns>
        public Person GetPersonBySocialId(SocialIdentity socialId)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
