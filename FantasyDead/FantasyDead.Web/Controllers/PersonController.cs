namespace FantasyDead.Web.Controllers
{
    using Data;
    using Data.Documents;
    using Microsoft.Azure.NotificationHubs;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;

    /// <summary>
    /// Controller responsible for all Person CRUD actions
    /// </summary>
    public class PersonController : BaseApiController
    {

        private readonly DataContext db;
        private NotificationHubClient hub;


        /// <summary>
        /// Default constructor.
        /// </summary>
        public PersonController()
        {
            this.db = new DataContext();
        }


        /// <summary>
        /// Retrieves the requestor's profile.
        /// Database caches this result for 30 seconds.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/person")]
        public HttpResponseMessage Profile()
        {
            var person = this.db.GetPerson(this.Requestor.PersonId);
            var stripped = person.StripCreds();
            return this.Request.CreateResponse(HttpStatusCode.OK, stripped);
        }

        /// <summary>
        /// Retrieves another user's profile.
        /// Database caches this result for 30 seconds.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/person/{id}")]
        public HttpResponseMessage Profile(string id)
        {
            var person = this.db.GetPerson(id);
            var stripped = person.StripCreds();
            return this.Request.CreateResponse(HttpStatusCode.OK, stripped);
        }

        /// <summary>
        /// POST api/person/email
        /// Updates a user's email and username if they're a new user.
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/person/email")]
        public async Task<HttpResponseMessage> UpdateUsernameAndEmail([FromBody] UpdateEmailReq req)
        {
            var person = this.db.GetPerson(this.Requestor.PersonId, false);

            if (!string.IsNullOrWhiteSpace(req.Username))
            {
                if (person.Role != (int)PersonRole.NewUser)
                    return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "You cannot change your username.");

                person.Role = (int)PersonRole.Member;

                var alreadyExist = this.db.GetPersonIdByUsername(req.Username);
                if (alreadyExist != null)
                    return this.Request.CreateErrorResponse(HttpStatusCode.Conflict, "That username is already taken.");

                person.Username = req.Username;
            }
            person.Email = req.Email;

            await this.db.UpdatePerson(person);
            return this.Request.CreateResponse(HttpStatusCode.OK);
        }


        /// <summary>
        /// DELETE api/person/friend/{personId}
        /// Removes a friend from the requestor's friends list.
        /// </summary>
        /// <param name="personId"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("api/person/friend/{personId}")]
        public async Task<HttpResponseMessage> RemoveFriend(string personId)
        {
            var person = this.db.GetPerson(this.Requestor.PersonId, false);
            if (person.Friends == null)
                return this.Request.CreateResponse(HttpStatusCode.OK);

            person.Friends.Remove(personId);

            await this.db.UpdatePerson(person);
            return this.Request.CreateResponse(HttpStatusCode.Created);
        }


        /// <summary>
        /// GET api/person/friend/search/{s}
        /// Searches for a person by their username, returns a collection of potential results.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/person/friend/search/{s}")]
        public async Task<HttpResponseMessage> Search(string s)
        {
            var people = await this.db.SearchPeople(s);

            return this.ConvertDbResponse(people);
        }

        /// <summary>
        /// PUT api/person/friend/{personId}
        /// Adds a friend to the requestor's friends list.
        /// </summary>
        /// <param name="personId"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("api/person/friend/{personId}")]
        public async Task<HttpResponseMessage> AddFriend(string personId)
        {
            var person = this.db.GetPerson(this.Requestor.PersonId, false);
            if (person.Friends == null)
                person.Friends = new List<string>();

            person.Friends.Add(personId);

            await this.db.UpdatePerson(person);
            return this.Request.CreateResponse(HttpStatusCode.Created);
        }

        /// <summary>
        /// POST api/person/config/{key}
        /// Updates a person's configuration.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/person/config/{key}")]
        public async Task<HttpResponseMessage> UpdateConfiguration(string key, [FromBody] string value)
        {
            var person = this.db.GetPerson(this.Requestor.PersonId);

            if (value == null)
                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Value was null");

            switch (key)
            {
                default:
                    {
                        return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid key.");
                    }
                case "ReceiveNotifications":
                    {
                        person.Configuration.ReceiveNotifications = Convert.ToBoolean(value);
                        break;
                    }
                case "DeadlineReminderHours":
                    {
                        person.Configuration.DeadlineReminderHours = Convert.ToInt32(value);
                        break;
                    }
                case "NotifyWhenScored":
                    {
                        person.Configuration.NotifyWhenScored = Convert.ToBoolean(value);
                        break;
                    }

            }

            await this.db.UpdatePerson(person);
            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// PUT api/person/push/register
        /// Registers the person for push notifications on their device.
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("api/person/push/register")]
        public async Task<HttpResponseMessage> RegisterPush(PushRequest req)
        {
            this.InitializeHub();
            req.Device = req.Device.ToLowerInvariant().Trim();

            RegistrationDescription reg;
            switch (req.Device)
            {
                case "android":
                    {
                        reg = new GcmRegistrationDescription(req.RegistrationId);
                        break;
                    }
                default:
                    {
                        return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Push notification is not supported for this device");
                    }
            }
            reg.Tags = new HashSet<string>();
            reg.Tags.Add(this.Requestor.PersonId);
            reg = await this.hub.CreateRegistrationAsync(reg);

            var person = this.db.GetPerson(this.Requestor.PersonId);
            person.PushNotificationData = reg.RegistrationId;

            if (person.Configuration == null)
                person.Configuration = new PersonConfiguration();

            person.Configuration.ReceiveNotifications = true;

            await this.db.UpdatePerson(person);

            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// DELETE api/person/push/cancel
        /// Removes push notification subscription.
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        [Route("api/person/push/cancel")]
        public async Task<HttpResponseMessage> CancelPush()
        {
            this.InitializeHub();

            var person = this.db.GetPerson(this.Requestor.PersonId);
            if (string.IsNullOrWhiteSpace(person.PushNotificationData))
                return this.Request.CreateResponse(HttpStatusCode.OK);
            try
            {

                await this.hub.DeleteRegistrationAsync(person.PushNotificationData);

            }
            catch (Exception ex)
            {

                throw;
            }

            person.PushNotificationData = null;
            person.Configuration.ReceiveNotifications = false;
            await this.db.UpdatePerson(person);

            return this.Request.CreateResponse(HttpStatusCode.OK);
        }


        private void InitializeHub()
        {
            this.hub = NotificationHubClient.CreateClientFromConnectionString(ConfigurationManager.AppSettings["notificationHub"], "push");
        }
    }
}
