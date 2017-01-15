namespace FantasyDead.Data
{
    using FantasyDead.Data.Configuration;
    using FantasyDead.Data.Documents;
    using FantasyDead.Data.Models;
    using Microsoft.ApplicationInsights;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
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
        private readonly CloudTable events;
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

            this.events = client.GetTableReference("events");
            this.events.CreateIfNotExists();

            this.configuration = client.GetTableReference("configuration");
            this.configuration.CreateIfNotExists();
        }



        #region People CRUD

        /// <summary>
        /// Adds a person(user)
        /// </summary>
        /// <param name="person"></param>
        public async Task<DataContextResponse> Register(Person person, bool continueDespiteUsername = false)
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
                if (alreadyExistingPerson != null && !continueDespiteUsername)
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
        /// Searches for folks.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public async Task<DataContextResponse> SearchPeople(string s)
        {
            s = s.Trim().ToLowerInvariant();
            var queryStr = $"select p.id, p.AvatarPictureUrl, p.Username, p.TotalScore from people p where contains(lower(p.Username), '{s}')";

            var options = new FeedOptions
            {
                MaxItemCount = 100
            };

            var q = this.db.CreateDocumentQuery<Person>
                (this.peopleColUri, queryStr, options)
                .AsDocumentQuery();

            var result = await q.ExecuteNextAsync<Person>();

            return new DataContextResponse() { Content = result.ToList() };
        }

        /// <summary>
        /// Updates a person (user).
        /// </summary>
        /// <param name="person"></param>
        public async Task<DataContextResponse> UpdatePerson(Person person)
        {
            if (person == null || string.IsNullOrWhiteSpace(person.Id))
                return DataContextResponse.Error(HttpStatusCode.BadRequest, "Invalid request, data is missing.");

            if (person.Identities[0].Credentials == "")
            {
                var CULPRIT = 0;
                throw new InvalidOperationException("Credentials can't be null when updating.");
            }

            try
            {
                await this.db.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(dbName, peopleCol, person.Id), person);
                return DataContextResponse.Ok;
            }
            catch (Exception ex)
            {
                this.telemtry.TrackException(ex);
                return DataContextResponse.Error(HttpStatusCode.InternalServerError, "Something went wrong with the request. Try again later.");
            }

        }

        /// <summary>
        /// Adds a subset of events to a person.
        /// </summary>
        /// <param name="events"></param>
        /// <param name="personId"></param>
        public async void AddEventsToPerson(List<CharacterEvent> events, string personId)
        {
            var person = this.GetPerson(personId);
            if (person == null)
                return;

            person.Events.AddRange(events);
            person.TotalScore = person.Events.Sum(e => e.Points);
            await this.UpdatePerson(person);
        }

        /// <summary>
        /// Removes a events from the person that were related to specific episode.
        /// </summary>
        /// <param name="fromEpisodeId"></param>
        /// <param name="personId"></param>
        public async Task RevokeEventsFromPerson(string fromEpisodeId, string personId)
        {
            var person = this.GetPerson(personId);
            if (person == null)
                return;

            person.Events = person.Events.Where(e => e.EpisodeId != fromEpisodeId).ToList();
            person.TotalScore = person.Events.Sum(e => e.Points);
            await this.UpdatePerson(person);
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
        public Person GetPerson(string personId, bool isCached = true)
        {
            if (string.IsNullOrWhiteSpace(personId))
                return null;

            var key = personId;
            var cachedPerson = this.cache.Get(key);
            if (cachedPerson != null && isCached)
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


        /// <summary>
        /// Fetches a full list of people (can be used as Leaderboard).
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<LeaderboardResult> GetPeopleByList(List<string> ids)
        {
            var arr = $"['{string.Join("','", ids)}']";
            var people = this.db.CreateDocumentQuery<Person>(this.peopleColUri,
                $"select * from people p where array_contains({arr}, p.id)").AsDocumentQuery();

            var result = await people.ExecuteNextAsync<Person>();
            return this.GenerateLeaderboard(result);
        }

        /// <summary>
        /// Fetches the leaderboard, in pages 100 people per page.
        /// </summary>
        /// <param name="contToken"></param>
        /// <returns></returns>
        public async Task<LeaderboardResult> Leaderboard(string contToken)
        {
            var options = new FeedOptions
            {
                MaxItemCount = 100,
                RequestContinuation = contToken
            };

            var q = this.db.CreateDocumentQuery<Person>
                (this.peopleColUri, $"SELECT p.id, p.AvatarPictureUrl, p.Username, p.Events, p.TotalScore FROM people p order by p.TotalScore desc", options)
                .AsDocumentQuery();

            var result = await q.ExecuteNextAsync<Person>();

            return this.GenerateLeaderboard(result);
        }

        private LeaderboardResult GenerateLeaderboard(FeedResponse<Person> result)
        {
            var pageResult = new LeaderboardResult { ContinuationToken = result.ResponseContinuation, Items = new List<LeaderboardItem>() };

            var current = this.FetchNextAvailableEpisode();

            var episodes = (this.FetchShowData().Content as List<Show>)[0].Seasons.SelectMany(s => s.Episodes)
                .Where(e=>e.Id != current.Id)
                .ToList();

            foreach (var p in result)
            {
                var lbItem = new LeaderboardItem { PersonId = p.Id, AvatarUrl = p.AvatarPictureUrl, Username = p.Username, EpisodeScores = new List<LeaderboardEpisodeItem>() };
                
                lbItem.EpisodeScores = episodes.Select(ep => new LeaderboardEpisodeItem { EpisodeId = ep.Id, EpisodeName = ep.Name, EpisodeScore = 0, EpisodeDate = ep.AirDate }).ToList();
                string mostRecentEpId = null;
                if (lbItem.EpisodeScores.Count > 1)
                    mostRecentEpId = lbItem.EpisodeScores.Last().EpisodeId;

                foreach (var ev in p.Events)
                {
                    //treat episode list as KvP
                    var epAe = lbItem.EpisodeScores.First(e => e.EpisodeId == ev.EpisodeId);
                    epAe.EpisodeScore += ev.Points;

                    if (!string.IsNullOrWhiteSpace(mostRecentEpId) && mostRecentEpId != ev.EpisodeId)
                        lbItem.PreviousEpScore += ev.Points;
                }

                lbItem.EpisodeScores = lbItem.EpisodeScores.OrderByDescending(e => e.EpisodeDate).ToList();


                pageResult.Items.Add(lbItem);
            }


            var lastWeekOrder = pageResult.Items.OrderByDescending(i => i.PreviousEpScore).ToList();

            for (var k = 0; k < lastWeekOrder.Count; k++)
            {
                lastWeekOrder[k].PreviousRank = k + 1;
            }

            pageResult.Items = pageResult.Items.OrderByDescending(i => i.TotalScore).ToList();

            for (var k = 0; k < pageResult.Items.Count; k++)
            {
                var i = pageResult.Items[k];
                pageResult.Items[k].CurrentRank = k + 1;
            }

            return pageResult;
        }


        #endregion


        #region Generic Data Contextual Info


        /// <summary>
        /// Retrieves the current episode that is open for selection.
        /// </summary>
        /// <returns>Can return null, if no episode is available.</returns>
        public Episode FetchNextAvailableEpisode(string showId = "")
        {
            var sd = (this.FetchShowData().Content as List<Show>);
            var show = sd.FirstOrDefault(s => s.Id == showId);
            if (show == null)
                show = sd.FirstOrDefault();

            var today = DateTime.UtcNow;
            var allEpisodes = show.Seasons.SelectMany(s => s.Episodes);

            var nextOpen = (from ep in allEpisodes where ep.AirDate > today && ep.LockDate > today orderby ep.AirDate ascending select ep).FirstOrDefault();
            return nextOpen;
        }

        /// <summary>
        /// Fetches the current configuration of event definitions. Cached for 5 minutes; this can be overridden by setting the isCached paramater to false.
        /// </summary>
        /// <param name="isCached"></param>
        /// <returns></returns>
        public DataContextResponse FetchEventDefinitions(string showId = "", bool isCached = true)
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

            if (!string.IsNullOrWhiteSpace(showId))
                results = results.Where(eD => eD.ShowId == showId).ToList();

            this.cache.Set(EventDefinition.Pkey, results, new CacheItemPolicy { AbsoluteExpiration = DateTime.UtcNow.Add(TimeSpan.FromMinutes(5)) });

            return new DataContextResponse { Content = results };
        }


        /// <summary>
        /// Fetches the current configuration of event modifiers. Cached for 5 minutes; this can be overridden by setting the isCached paramater to false.
        /// </summary>
        /// <param name="isCached"></param>
        /// <returns></returns>
        public DataContextResponse FetchEventModifiers(string showId = "", bool isCached = true)
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
            if (!string.IsNullOrWhiteSpace(showId))
                results = results.Where(eM => eM.ShowId == showId).ToList();

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
        public List<EpisodePick> FetchPicksForCharacter(string characterId, string episodeId)
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
            //validate pick is not too old
            var pick = (from pk in this.db.CreateDocumentQuery<EpisodePick>(picksColUri) where pk.Id == pickId select pk)
                .ToList().FirstOrDefault();

            if (pick == null)
                return DataContextResponse.Error(HttpStatusCode.NotFound, "Roster slot could not be found.");

            var openEp = this.FetchNextAvailableEpisode(pick.ShowId);
            if (openEp == null || openEp.Id != pick.EpisodeId)
                return DataContextResponse.Error(HttpStatusCode.Forbidden, "You cannot delete old roster slots.");

            try
            {
                var op = await this.db.DeleteDocumentAsync(UriFactory.CreateDocumentUri(dbName, picksCol, pickId));
                return new DataContextResponse { Content = pick, StatusCode = HttpStatusCode.OK };
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
        public async Task<DataContextResponse> PushEpisodePick(EpisodePick pick, string swappingCharacterId = "")
        {


            ////// holy validation batman

            var allPicks = this.GetEpisodePicks(pick.PersonId).Content as List<EpisodePick>;
            var currentPicks = allPicks.Where(pk => pk.EpisodeId == pick.EpisodeId);

            if (currentPicks.Any(c => c.CharacterId == pick.CharacterId))
                return DataContextResponse.Error(HttpStatusCode.Conflict, $"You already have this character slotted elsewhere.");

            var classic = currentPicks.Where(p => p.SlotType == (int)SlotType.Classic).ToList();
            var death = currentPicks.Where(p => p.SlotType == (int)SlotType.Death).ToList();

            if (pick.SlotType == (int)SlotType.Death)
            {
                var limit = Convert.ToInt32(ConfigurationManager.AppSettings["deathSlots"]);
                if (death.Count >= limit)
                    return DataContextResponse.Error(HttpStatusCode.Conflict, $"You cannot slot more than {limit} character(s) in the death slot for this episode.");

                //if (death.FirstOrDefault()?.CharacterId == pick.CharacterId)
                //    return DataContextResponse.Error(HttpStatusCode.Conflict, $"You already have this character slotted elsewhere.");
            }
            else if (pick.SlotType == (int)SlotType.Classic && string.IsNullOrWhiteSpace(swappingCharacterId))
            {
                var limit = Convert.ToInt32(ConfigurationManager.AppSettings["classicSlots"]);
                if (classic.Count >= limit)
                    return DataContextResponse.Error(HttpStatusCode.Conflict, $"You cannot slot more than {limit} character(s) per episode.");
            }


            var usageLimit = Convert.ToInt32(ConfigurationManager.AppSettings["characterUsageLimit"]);
            if (allPicks.Count(pk => pk.CharacterId == pick.CharacterId) == usageLimit)
                return DataContextResponse.Error(HttpStatusCode.Conflict, $"You cannot slot this character any more. You've already slotted them {usageLimit} times.");

            ////// END VALIDATION

            try
            {
                if (!string.IsNullOrWhiteSpace(swappingCharacterId))
                {
                    //swap, don't add
                    var doc = currentPicks.FirstOrDefault(c => c.CharacterId == swappingCharacterId);
                    if (doc == null)
                        return DataContextResponse.Error(HttpStatusCode.BadRequest, "Cannot swap a character that you don't have slotted.");

                    await this.db.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(dbName, picksCol, doc.Id), pick);
                    return DataContextResponse.Ok;
                }


                await this.db.CreateDocumentAsync(picksColUri, pick);
                return DataContextResponse.Ok;
            }
            catch (DocumentClientException dce)
            {
                this.telemtry.TrackException(dce);
                return DataContextResponse.Error((HttpStatusCode)dce.StatusCode, dce.Message);
            }
        }

        /// <summary>
        /// Retrieves all episode picks for a current person for a given episode.
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

        /// <summary>
        /// Retrieves all episode picks for a current person.
        /// </summary>
        /// <param name="personId"></param>
        /// <returns></returns>
        public DataContextResponse GetEpisodePicks(string personId)
        {
            var picks = from pk in this.db.CreateDocumentQuery<EpisodePick>(picksColUri) where pk.PersonId == personId select pk;
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
                    ((MemoryCache)this.cache).Dispose(); //clear cache
                    return DataContextResponse.Ok;
                }
                else
                {
                    await this.db.CreateDocumentAsync(this.showColUri, show); //create it
                    ((MemoryCache)this.cache).Dispose(); //clear cache
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

            ((MemoryCache)this.cache).Dispose(); //clear cache
        }

        /// <summary>
        /// Removes a configuration item from the database. Used for Characters, event definitions, and event modifiers.
        /// WARNING: If a configuration item that is used in a calculation is removed, the event related to that item will fail to calculate during a re-calculation, causing a potential
        /// change in score.
        /// </summary>
        /// <param name="item"></param>
        public void DeleteConfiguration(ITableEntity item)
        {
            if (item.PartitionKey != EventDefinition.Pkey && item.PartitionKey != EventModifier.Pkey && item.PartitionKey != Character.Pkey)
                throw new ArgumentException("Invalid configuration item type", nameof(item));

            var op = TableOperation.Delete(item);
            this.configuration.Execute(op);

            ((MemoryCache)this.cache).Dispose(); //clear cache
        }

        #endregion


        #region Events (CRUD)

        /// <summary>
        /// Inserts or updates an event in the event store for a character. Does NOT trigger a calculation.
        /// </summary>
        /// <param name="ev"></param>
        public void AddEvent(CharacterEvent ev)
        {
            if (ev == null || string.IsNullOrWhiteSpace(ev.ActionId) || ev.EpisodeTimestamp == 0)
                throw new ArgumentNullException("Not enough information for a valid event.", nameof(ev));

            var op = TableOperation.InsertOrReplace(ev);
            this.events.Execute(op);

            var episodeIndex = new CharacterEventIndex
            {
                PartitionKey = ev.EpisodeId,
                RowKey = ev.RowKey,
                CharacterId = ev.CharacterId,
                ModifierId = ev.ModifierId,
                ActionId = ev.ActionId,
                Notes = ev.Notes,
                Points = ev.Points,
                EpisodeTimestamp = ev.EpisodeTimestamp,
                ShowId = ev.ShowId,
                Description = ev.Description,
            };
            var op2 = TableOperation.InsertOrReplace(episodeIndex);
            this.events.Execute(op2);
        }

        /// <summary>
        /// Removes an event from the store. This does NOT trigger a recalculation.
        /// </summary>
        /// <param name="characterId"></param>
        /// <param name="eventId"></param>
        public void RemoveEvent(string characterId, string eventId)
        {
            var ev = this.FetchSingleEvent(characterId, eventId);
            if (ev == null)
                return;
            ev.ETag = "*";
            var op = TableOperation.Delete(ev);
            this.events.Execute(op);

            Task.Run(() =>
            {
                var evIx = this.FetchSingleEventByEpisode(ev.EpisodeId, ev.Id);
                if (evIx == null)
                    return;

                evIx.ETag = "*";
                var op2 = TableOperation.Delete(evIx);
                this.events.Execute(op2);
            });
        }

        /// <summary>
        /// Fetches all events related to a character, irrespective of episode.
        /// </summary>
        /// <param name="characterId"></param>
        /// <returns></returns>
        public List<CharacterEvent> FetchEventsForCharacter(string characterId)
        {
            return this.FetchEventsByPkey(characterId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="episodeId"></param>
        /// <returns></returns>
        public List<CharacterEventIndex> FetchEventsForEpisode(string episodeId)
        {
            return this.FetchEventsByPkeyIndex(episodeId);
        }

        /// <summary>
        /// Returns a single event for a character.
        /// </summary>
        /// <param name="characterId"></param>
        /// <param name="eventId"></param>
        /// <returns></returns>
        public CharacterEvent FetchSingleEvent(string characterId, string eventId)
        {
            var ev = this.FetchSingleEventByKeys(characterId, eventId);
            if (ev == null)
                return null;

            var cEv = new CharacterEvent() { PartitionKey = ev.PartitionKey, RowKey = ev.RowKey };
            cEv.ReadEntity(ev.Properties, null);
            return cEv;
        }


        /// <summary>
        /// Returns a single event for an episode.
        /// </summary>
        /// <param name="characterId"></param>
        /// <param name="eventId"></param>
        /// <returns></returns>
        public CharacterEventIndex FetchSingleEventByEpisode(string episodeId, string eventId)
        {
            var ev = this.FetchSingleEventByKeys(episodeId, eventId);
            if (ev == null)
                return null;

            var cEv = new CharacterEventIndex() { PartitionKey = ev.PartitionKey, RowKey = ev.RowKey }; ;
            cEv.ReadEntity(ev.Properties, null);
            return cEv;
        }


        private List<CharacterEvent> FetchEventsByPkey(string pkey)
        {
            var pkeyF = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, pkey);
            var q = new TableQuery<CharacterEvent>().Where(pkeyF);
            return this.events.ExecuteQuery(q).ToList();
        }

        private List<CharacterEventIndex> FetchEventsByPkeyIndex(string pkey)
        {
            var pkeyF = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, pkey);
            var q = new TableQuery<CharacterEventIndex>().Where(pkeyF);
            return this.events.ExecuteQuery(q).ToList();
        }

        private DynamicTableEntity FetchSingleEventByKeys(string pkey, string rkey)
        {
            var pkeyF = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, pkey);
            var rkeyF = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rkey);
            var filter = TableQuery.CombineFilters(pkeyF, TableOperators.And, rkeyF);
            var q = new TableQuery<DynamicTableEntity>().Where(filter);

            var results = this.events.ExecuteQuery(q).ToList(); //execute
            return results.FirstOrDefault(); //return single
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
