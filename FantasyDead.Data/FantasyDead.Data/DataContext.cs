using FantasyDead.Data.Documents;
using FantasyDead.Data.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading.Tasks;

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
        private static string picksCol = "picks";
        private static string dbName = "fantasyDb";

        private readonly Uri peopleColUri;
        private readonly Uri showColUri;
        private readonly Uri picksColUri;

        private readonly TelemetryClient telemtry;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DataContext()
        {
            this.cache = MemoryCache.Default;

            this.db = new DocumentClient(new Uri("https://fantasydead.documents.azure.com:443/"), ConfigurationManager.AppSettings["docuDbKey"]);
            this.peopleColUri = UriFactory.CreateDocumentCollectionUri(dbName, peopleCol);
            this.showColUri = UriFactory.CreateDocumentCollectionUri(dbName, showsCol);
            this.showColUri = UriFactory.CreateDocumentCollectionUri(dbName, picksCol);

            this.telemtry = new TelemetryClient();
        }



        #region People CRUD

        /// <summary>
        /// Adds a person(user)
        /// </summary>
        /// <param name="person"></param>
        public async Task<DataContextResponse> Register(Person person)
        {
            if (!person.Identities.Any())
                throw new ArgumentException("Person has no identities to use.", nameof(person));

            try
            {
                //already exists?
                var socId = person.Identities.First();
                var alreadyExistingPerson = this.GetPersonBySocialId(socId);

                if (alreadyExistingPerson != null)
                    return DataContextResponse.Error(HttpStatusCode.Conflict, "You have already registered with that email or social account.");

                //username taken?
                alreadyExistingPerson = this.GetPerson(this.GetPersonIdByUsername(person.Username));
                if (alreadyExistingPerson != null)
                    return DataContextResponse.Error(HttpStatusCode.Conflict, "That username is already taken.");


                person.Id = Guid.NewGuid().ToString();
                foreach (var id in person.Identities)
                {
                    id.PersonId = person.Id;
                }

                await this.db.CreateDocumentAsync(this.peopleColUri, person);

                return new DataContextResponse { StatusCode = HttpStatusCode.Created, Content = person };
            }
            catch (Exception ex)
            {
                this.telemtry.TrackException(ex);
                return DataContextResponse.Error(HttpStatusCode.InternalServerError, "Something went wrong when we tried to register you. Please try again later.");
            }
        }

        /// <summary>
        /// Updates a person (user).
        /// </summary>
        /// <param name="person"></param>
        public async Task<DataContextResponse> UpdatePerson(Person person)
        {
            if (person == null || string.IsNullOrWhiteSpace(person.Id))
                return DataContextResponse.Error(HttpStatusCode.BadRequest, "Invalid request, data is missing.");

            try
            {
                await this.db.ReplaceDocumentAsync(this.peopleColUri, person);
                return DataContextResponse.Ok;
            }
            catch (Exception ex)
            {
                this.telemtry.TrackException(ex);
                return DataContextResponse.Error(HttpStatusCode.InternalServerError, "Something went wrong with the request. Try again later.");
            }

        }

        /// <summary>
        /// Bans a person(user) from the app.
        /// </summary>
        /// <param name="personId"></param>
        public async Task<DataContextResponse> BanPerson(string personId)
        {
            var person = this.GetPerson(personId);
            person.Role = -1;
            await this.UpdatePerson(person);
            return DataContextResponse.Ok;
        }

        /// <summary>
        /// Retrieves a person(user)'s profile.
        /// Cached for 30 seconds based on personId.
        /// </summary>
        /// <param name="personId"></param>
        /// <returns></returns>
        public Person GetPerson(string personId)
        {
            var key = personId;
            var cachedPerson = this.cache.Get(key);
            if (cachedPerson != null)
                return cachedPerson as Person;

            var person = from p in this.db.CreateDocumentQuery<Person>(this.peopleColUri) where p.Id == personId select p;
            var p = person.ToList().FirstOrDefault();

            this.cache.Add(key, p, new CacheItemPolicy { AbsoluteExpiration = DateTime.UtcNow.Add(TimeSpan.FromSeconds(30)) });

            return p;
        }


        /// <summary>
        /// Retrieves a person(user)'s person Id via their username.
        /// Cached for 6 hours.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public string GetPersonIdByUsername(string username)
        {
            var key = $"person-{username}";
            var pId = this.cache.Get(key);

            if (pId != null)
                return pId as string;

            var person = from p in this.db.CreateDocumentQuery<Person>(this.peopleColUri) where p.Username == username select p;
            var personId = person.ToList().FirstOrDefault()?.Id;

            if (!string.IsNullOrWhiteSpace(personId))
                this.cache.Add(key, personId, new CacheItemPolicy { AbsoluteExpiration = DateTime.UtcNow.Add(TimeSpan.FromHours(6)) });

            return personId;
        }

        /// <summary>
        /// Retrieves a person(user)'s profile via a known social identity.
        /// Cached for 30 seconds based on social identity.
        /// </summary>
        /// <param name="socialId"></param>
        /// <returns></returns>
        public Person GetPersonBySocialId(SocialIdentity socialId)
        {
            var key = $"{socialId.PlatformName}:{socialId.PlatformUserId}";

            var cachedPerson = this.cache.Get(key);
            if (cachedPerson != null)
                return cachedPerson as Person;

            var person = this.db.CreateDocumentQuery<Person>(this.peopleColUri,
                $"select p from people p join i in p.identities where i.platformUserId = '{socialId.PlatformUserId}' and i.platformName = '{socialId.PlatformName}'");
            var p = person.ToList().FirstOrDefault();

            this.cache.Add(key, p, new CacheItemPolicy { AbsoluteExpiration = DateTime.UtcNow.Add(TimeSpan.FromSeconds(30)) });

            return p;
        }

        #endregion


        #region Generic Data Contextual Info




        #endregion


        #region User Actions

        /// <summary>
        /// Pushes an episode (character) pick for a user to the data store.
        /// </summary>
        /// <param name="pick"></param>
        /// <returns></returns>
        public async Task<DataContextResponse> PushEpisodePick(EpisodePick pick)
        {
            try
            {
                var doc = await this.db.ReadDocumentAsync(UriFactory.CreateDocumentUri(dbName, picksCol, pick.Id));
            }
            catch (DocumentClientException dce)
            {
                if (dce.StatusCode == HttpStatusCode.NotFound)
                {
                    await this.db.CreateDocumentAsync(picksCol, pick);
                    return DataContextResponse.Ok;
                }

                this.telemtry.TrackException(dce);
                return DataContextResponse.Error((HttpStatusCode)dce.StatusCode, dce.Message);
            }

            return DataContextResponse.Error(HttpStatusCode.InternalServerError, "Could not push your episode pick. Try again later.");
        }

        /// <summary>
        /// Retrieves all episode picks for a current person.
        /// </summary>
        /// <param name="personId"></param>
        /// <param name="episodeId"></param>
        /// <returns></returns>
        public DataContextResponse GetEpisodePicks(string personId, string episodeId)
        {
            var picks = from pk in this.db.CreateDocumentQuery<EpisodePick>(picksColUri) where pk.EpisodeId == episodeId && pk.PersonId == personId select pk);
            var result = picks.ToList();

            return new DataContextResponse { Content = result };
        }

        #endregion
    }


    public class DataContextResponse
    {
        public DataContextResponse()
        {
            this.StatusCode = HttpStatusCode.OK;
        }

        public HttpStatusCode StatusCode { get; set; }

        public string Message { get; set; }

        public object Content { get; set; }


        public static DataContextResponse Ok
        {
            get
            {
                return new DataContextResponse { StatusCode = HttpStatusCode.OK, Message = string.Empty };
            }
        }

        public static DataContextResponse Created
        {
            get
            {
                return new DataContextResponse { StatusCode = HttpStatusCode.Created, Message = string.Empty };
            }
        }

        public static DataContextResponse Error(HttpStatusCode code, string message)
        {
            return new DataContextResponse { StatusCode = code, Message = message };
        }
    }
}
