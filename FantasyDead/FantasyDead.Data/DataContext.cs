namespace FantasyDead.Data
{
    using FantasyDead.Data.Configuration;
    using FantasyDead.Data.Documents;
    using FantasyDead.Data.Models;
    using Microsoft.ApplicationInsights;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.Caching;
    using System.Threading.Tasks;

    /// <summary>
    /// Primary data access context for the system.
    /// </summary>
    public sealed class DataContext
    {
        private readonly CloudTable stats;
        private readonly CloudTable configuration;
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
            this.picksColUri = UriFactory.CreateDocumentCollectionUri(dbName, picksCol);

            this.telemtry = new TelemetryClient();

            var act = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["cloudStorage"]);
            var client = act.CreateCloudTableClient();

            this.stats = client.GetTableReference("stats");
            this.stats.CreateIfNotExists();

            this.configuration = client.GetTableReference("configuration");
            this.configuration.CreateIfNotExists();
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

                return new DataContextResponse { StatusCode = HttpStatusCode.Created, Content = person.PersonId };
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
            if (string.IsNullOrWhiteSpace(personId))
                return null;

            var key = personId;
            var cachedPerson = this.cache.Get(key);
            if (cachedPerson != null)
                return cachedPerson as Person;

            var person = from p in this.db.CreateDocumentQuery<Person>(this.peopleColUri) where p.Id == personId select p;
            var newP = person.ToList().FirstOrDefault();

            if (newP != null)
                this.cache.Add(key, newP, new CacheItemPolicy { AbsoluteExpiration = DateTime.UtcNow.Add(TimeSpan.FromSeconds(30)) });

            return newP;
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
                $"select p.id, p.Username, p.Identities, p.Role from people p join i in p.Identities where i.PlatformUserId = '{socialId.PlatformUserId}' and i.PlatformName = '{socialId.PlatformName}'");
            var p = person.ToList().FirstOrDefault();

            if (p != null)
                this.cache.Add(key, p, new CacheItemPolicy { AbsoluteExpiration = DateTime.UtcNow.Add(TimeSpan.FromSeconds(30)) });

            return p;
        }

        #endregion


        #region Generic Data Contextual Info

        /// <summary>
        /// Fetches the current configuration of event definitions. Cached for 5 minutes; this can be overridden by setting the isCached paramater to false.
        /// </summary>
        /// <param name="isCached"></param>
        /// <returns></returns>
        public DataContextResponse FetchEventDefinitions(bool isCached = true)
        {
            var cached = this.cache.Get(EventDefinition.Pkey);
            if (cached != null && isCached)
            {
                var cachedResult = cached as List<EventDefinition>;
                return new DataContextResponse { Content = cachedResult };
            }

            var pkey = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, EventDefinition.Pkey);
            var q = new TableQuery<EventDefinition>().Where(pkey);
            var results = this.configuration.ExecuteQuery(q).ToList();

            this.cache.Set(EventDefinition.Pkey, results, new CacheItemPolicy { AbsoluteExpiration = DateTime.UtcNow.Add(TimeSpan.FromMinutes(5)) });

            return new DataContextResponse { Content = results };
        }


        /// <summary>
        /// Fetches the current configuration of event modifiers. Cached for 5 minutes; this can be overridden by setting the isCached paramater to false.
        /// </summary>
        /// <param name="isCached"></param>
        /// <returns></returns>
        public DataContextResponse FetchEventModifiers(bool isCached = true)
        {
            var cached = this.cache.Get(EventModifier.Pkey);
            if (cached != null && isCached)
            {
                var cachedResult = cached as List<EventModifier>;
                return new DataContextResponse { Content = cachedResult };
            }

            var pkey = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, EventModifier.Pkey);
            var q = new TableQuery<EventModifier>().Where(pkey);
            var results = this.configuration.ExecuteQuery(q).ToList();
            this.cache.Set(EventModifier.Pkey, results, new CacheItemPolicy { AbsoluteExpiration = DateTime.UtcNow.Add(TimeSpan.FromMinutes(5)) });

            return new DataContextResponse { Content = results };
        }

        /// <summary>
        /// Fetches all the show data. Cached for 5 minutes; cache can be overridden with isCached parameter.
        /// </summary>
        /// <returns></returns>
        public DataContextResponse FetchShowData(bool isCached = true)
        {
            const string key = "show-data";
            var data = this.cache.Get(key);
            if (data != null && isCached)
                return new DataContextResponse { Content = data as List<Show> };

            var shows = from s in this.db.CreateDocumentQuery<Show>(showColUri) select s;
            var result = shows.Take(10).ToList();

            if (result.Any())
                this.cache.Set(key, result, new CacheItemPolicy { AbsoluteExpiration = DateTime.UtcNow.AddMinutes(5) });

            return new DataContextResponse { Content = result };
        }

        /// <summary>
        /// Fetches all characters within a specific show. Cached for 5 minutes; cache can be overridden.
        /// </summary>
        /// <param name="showId"></param>
        /// <returns></returns>
        public DataContextResponse FetchCharacters(string showId, bool isCached = true)
        {
            var cachedCharacters = this.cache.Get(Character.Pkey);
            if (cachedCharacters != null)
            {
                var filtered = (cachedCharacters as List<Character>).Where(c => c.ShowId == showId);
                return new DataContextResponse { Content = filtered.ToList() };
            }

            var pkey = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Character.Pkey);
            var q = new TableQuery<Character>().Where(pkey);
            var results = this.configuration.ExecuteQuery(q);
            var newResults = results.ToList();

            this.cache.Set(Character.Pkey, newResults, new CacheItemPolicy { AbsoluteExpiration = DateTime.UtcNow.Add(TimeSpan.FromMinutes(5)) });
            return new DataContextResponse { Content = newResults.Where(c => c.ShowId == showId).ToList() };
        }

        /// <summary>
        /// Statistical only method; used to retrieve all picks for specific character in a given episode.
        /// </summary>
        /// <param name="characterId"></param>
        /// <param name="episodeId"></param>
        /// <returns></returns>
        internal List<EpisodePick> FetchPicksForCharacter(string characterId, string episodeId)
        {
            var picks = from pk in this.db.CreateDocumentQuery<EpisodePick>(picksColUri) where pk.EpisodeId == episodeId && pk.CharacterId == characterId select pk;
            var result = picks.ToList();
            return result;
        }

        #endregion


        #region User Actions

        /// <summary>
        /// Removes a pick from an episode.
        /// </summary>
        /// <param name="pickId"></param>
        /// <returns></returns>
        public async Task<DataContextResponse> RemoveEpisodePick(string pickId)
        {
            try
            {
                var op = await this.db.DeleteDocumentAsync(UriFactory.CreateDocumentUri(dbName, picksCol, pickId));
                return DataContextResponse.Ok;
            }
            catch (DocumentClientException dce)
            {
                if (dce.StatusCode == HttpStatusCode.NotFound)
                    return DataContextResponse.Ok;

                this.telemtry.TrackException(dce);
                return DataContextResponse.Error((HttpStatusCode)dce.StatusCode, dce.Message);
            }
        }


        /// <summary>
        /// Pushes an episode (character) pick for a user to the data store.
        /// </summary>
        /// <param name="pick"></param>
        /// <returns></returns>
        public async Task<DataContextResponse> PushEpisodePick(EpisodePick pick)
        {
            //check episodes, can't add pick for an episode that's locked.



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
            var picks = from pk in this.db.CreateDocumentQuery<EpisodePick>(picksColUri) where pk.EpisodeId == episodeId && pk.PersonId == personId select pk;
            var result = picks.ToList();

            return new DataContextResponse { Content = result };
        }

        #endregion

        #region Configuration CRUD

        public async Task<DataContextResponse> UpsertShow(Show show)
        {
            try
            {
                //already exists?
                var showData = this.FetchShowData(false).Content as List<Show>;
                if (showData.Any(s => s.Id == show.Id))
                {
                    await this.db.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(dbName, showsCol, show.Id), show);
                    return DataContextResponse.Ok;
                }
                else
                {
                    await this.db.CreateDocumentAsync(this.showColUri, show); //create it
                    return DataContextResponse.Ok;
                }
            }
            catch (DocumentClientException dce)
            {
                this.telemtry.TrackException(dce);
                return DataContextResponse.Error((HttpStatusCode)dce.StatusCode, dce.Message);
            }
        }

        /// <summary>
        /// Adds or updates a season in the data store (adds it to the subsquent show).
        /// </summary>
        /// <param name="season"></param>
        /// <returns></returns>
        public async Task<DataContextResponse> UpsertSeason(Season season)
        {
            var shows = this.FetchShowData(false).Content as List<Show>;
            var relatedShow = shows.FirstOrDefault(s => s.Id == season.ShowId);
            if (relatedShow == null)
                return DataContextResponse.Error(HttpStatusCode.NotFound, "Show not found.");

            var ix = -1;
            for (var k = 0; k < relatedShow.Seasons.Count; k++)
            {
                if (relatedShow.Seasons[k].Id == season.Id)
                {
                    ix = k;
                    break;
                }
            }

            if (ix == -1)
            {
                relatedShow.Seasons.Add(season);
            }
            else
            {
                relatedShow.Seasons[ix] = season;
            }

            return await this.UpsertShow(relatedShow);
        }

        /// <summary>
        /// Adds or updates an episode in the storage (adds it to the season and the subsequent show).
        /// </summary>
        /// <param name="episode"></param>
        /// <returns></returns>
        public async Task<DataContextResponse> UpsertEpisode(Episode episode)
        {
            var shows = this.FetchShowData(false).Content as List<Show>;
            var relatedShow = shows.FirstOrDefault(s => s.Id == episode.ShowId);
            if (relatedShow == null)
                return DataContextResponse.Error(HttpStatusCode.NotFound, "Show not found.");

            var relatedSeason = relatedShow.Seasons.FirstOrDefault(se => se.Id == episode.SeasonId);

            var ix = -1;
            for (var k = 0; k < relatedSeason.Episodes.Count; k++)
            {
                if (relatedSeason.Episodes[k].Id == episode.Id)
                {
                    ix = k;
                    break;
                }
            }

            if (ix == -1)
            {
                relatedSeason.Episodes.Add(episode);
            }
            else
            {
                relatedSeason.Episodes[ix] = episode;
            }

            return await this.UpsertSeason(relatedSeason);
        }

        /// <summary>
        /// Inserts or updates a configuration entity in the cloud data store. 
        /// Used for Characters, Event Definitions, and Event Modifiers
        /// </summary>
        /// <param name="item"></param>
        public void UpsertConfigurationItem(ITableEntity item)
        {
            if (item.PartitionKey != EventDefinition.Pkey && item.PartitionKey != EventModifier.Pkey && item.PartitionKey != Character.Pkey)
                throw new ArgumentException("Invalid configuration item type", nameof(item));

            var op = TableOperation.InsertOrReplace(item);
            this.configuration.Execute(op);
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
