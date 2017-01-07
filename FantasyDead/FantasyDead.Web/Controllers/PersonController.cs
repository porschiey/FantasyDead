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
            person.StripCreds();
            return this.Request.CreateResponse(HttpStatusCode.OK, person);
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
            person.StripCreds();
            return this.Request.CreateResponse(HttpStatusCode.OK, person);
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

            if (key == "username")
            {
                if (person.Role != (int)PersonRole.NewUser)
                    return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "You cannot change your username.");

                person.Role = (int)PersonRole.Member;
                var alreadyExist = this.db.GetPersonIdByUsername(value);
                if (alreadyExist != null)
                    return this.Request.CreateErrorResponse(HttpStatusCode.Conflict, "That username is already taken.");

                person.Username = value;
            }
            else if (key == "email")
            {
                person.Email = value;
            }
            else
            {
                if (person.Configuration == null)
                    person.Configuration = new Dictionary<string, string>();

                if (!person.Configuration.ContainsKey(key))
                    person.Configuration.Add(key, string.Empty);

                person.Configuration[key] = value;
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
                person.Configuration = new Dictionary<string, string>();

            if (!person.Configuration.ContainsKey("ReceiveNotifications"))
                person.Configuration.Add("ReceiveNotifications", true.ToString());
            else
                person.Configuration["ReceiveNotifications"] = true.ToString();

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
            person.Configuration["ReceiveNotifications"] = false.ToString();
            await this.db.UpdatePerson(person);

            return this.Request.CreateResponse(HttpStatusCode.OK);
        }


        private void InitializeHub()
        {
            this.hub = NotificationHubClient.CreateClientFromConnectionString(ConfigurationManager.AppSettings["notificationHub"], "primary");
        }
    }
}
