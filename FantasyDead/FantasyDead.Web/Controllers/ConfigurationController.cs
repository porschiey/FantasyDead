namespace FantasyDead.Web.Controllers
{
    using App_Start;
    using Data;
    using Data.Documents;
    using FantasyDead.Data.Configuration;
    using Microsoft.WindowsAzure.Storage;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;

    /// <summary>
    /// Controller responsible for changing the configuration.
    /// </summary>
    public class ConfigurationController : BaseApiController
    {

        private readonly DataContext db;

        private readonly List<string> acceptedExtensions = new List<string> { ".jpg", ".jpeg", ".gif", ".png" };

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ConfigurationController()
        {
            this.db = new DataContext();
        }

        /// <summary>
        /// PUT api/configuration/definition
        /// Adds or updates a configuration in the data store.
        /// </summary>
        /// <param name="evDef"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("api/configuration/definition")]
        [ApiAuthorization(requiredRole: 2)]
        public HttpResponseMessage UpsertEventDefinition([FromBody] EventDefinition evDef)
        {
            if (string.IsNullOrWhiteSpace(evDef.ShowId))
                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An event must belong to a show. The ShowID was null.");

            if (string.IsNullOrWhiteSpace(evDef.RowKey))
                evDef.RowKey = Guid.NewGuid().ToString();

            this.db.UpsertConfigurationItem(evDef);
            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// PUT api/configuration/modifier
        /// Adds or updates a configuration in the data store.
        /// </summary>
        /// <param name="evMod"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("api/configuration/modifier")]
        [ApiAuthorization(requiredRole: 2)]
        public HttpResponseMessage UpsertModifier([FromBody] EventModifier evMod)
        {
            if (string.IsNullOrWhiteSpace(evMod.ShowId))
                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An modifier must belong to a show. The ShowID was null.");

            if (string.IsNullOrWhiteSpace(evMod.RowKey))
                evMod.RowKey = Guid.NewGuid().ToString();

            this.db.UpsertConfigurationItem(evMod);
            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// DELETE api/configuration/definition/{id}
        /// Removes a event definition from the system.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("api/configuration/definition/{id}")]
        [ApiAuthorization(requiredRole: 2)]
        public HttpResponseMessage DeleteEventDefinition(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The id is null or empty");

            var def = (this.db.FetchEventDefinitions().Content as List<EventDefinition>).FirstOrDefault(d => d.Id == id);
            if (def == null)
                return this.Request.CreateErrorResponse(HttpStatusCode.NotFound, "That event definition could not be found.");

            this.db.DeleteConfiguration(def);
            return this.Request.CreateResponse(HttpStatusCode.OK);
        }


        /// <summary>
        /// DELETE api/configuration/modifier/{id}
        /// Removes a event modifier from the system.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("api/configuration/modifier/{id}")]
        [ApiAuthorization(requiredRole: 2)]
        public HttpResponseMessage DeleteEventModifier(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The id is null or empty");

            var mod = (this.db.FetchEventModifiers().Content as List<EventModifier>).FirstOrDefault(d => d.Id == id);
            if (mod == null)
                return this.Request.CreateErrorResponse(HttpStatusCode.NotFound, "That event modifier could not be found.");

            this.db.DeleteConfiguration(mod);
            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// GET api/configuration/definitions/{showId}
        /// Lists all the definitions for a given show.
        /// </summary>
        /// <param name="showId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/configuration/definitions/{showId}")]
        public HttpResponseMessage FetchAllDefinitions(string showId)
        {
            return this.ConvertDbResponse(this.db.FetchEventDefinitions(showId));
        }

        /// <summary>
        /// GET api/configuration/modifiers/{showId}
        /// Lists all the modifiers for a given show.
        /// </summary>
        /// <param name="showId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/configuration/modifiers/{showId}")]
        public HttpResponseMessage FetchAllModifiers(string showId)
        {
            return this.ConvertDbResponse(this.db.FetchEventModifiers(showId));
        }

        /// <summary>
        /// GET api/configuration/shows
        /// Lists all the basic show data.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/configuration/shows")]
        public HttpResponseMessage FetchShowData()
        {
            return this.ConvertDbResponse(this.db.FetchShowData());
        }

        /// <summary>
        /// GET api/configuration/characters/{showId}
        /// Lists all the characters of a show.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/configuration/characters/{showId}")]
        public HttpResponseMessage FetchCharacters(string showId)
        {
            return this.ConvertDbResponse(this.db.FetchCharacters(showId));
        }

        /// <summary>
        /// POST api/configuration/image/{folder}
        /// Uploads an image and places it in the cloud blob storage in that folder.
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/configuration/image/{folder}")]
        public async Task<HttpResponseMessage> UploadImage(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || (folder != "chars" && folder != "avs"))
                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "You must provide a valid folder.");

            if (this.Requestor.Role < PersonRole.Admin && folder == "chars")
                return this.SpitForbidden();

            //get file
            try
            {
                var files = System.Web.HttpContext.Current.Request.Files;
                if (files.Count != 1)
                    return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid file count.");
                var file = files[0];

                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                var extension = Path.GetExtension(file.FileName);
                var fullName = fileName + extension;


                //validate file
                if (folder == "avs")
                {
                    var img = System.Drawing.Image.FromStream(file.InputStream);
                    if (img.Width > 200 || img.Height > 200)
                        return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The avatar dimensions are too large.");

                    if (file.ContentLength > 1000000)
                        return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The file is too large. It must be less than 1MB");

                    if (!acceptedExtensions.Contains(extension.ToLowerInvariant()))
                        return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"The file must be one of the following types: {string.Join(", ", acceptedExtensions)}");
                }


                var act = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["cloudStorage"]);
                var blobClient = act.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference(folder);
                container.CreateIfNotExists();

                var blockBlob = container.GetBlockBlobReference(fullName);
                using (var stream = file.InputStream)
                {
                    blockBlob.UploadFromStream(stream);
                }


                if (folder == "avs")
                {
                    //update user's avatar
                    var person = this.db.GetPerson(this.Requestor.PersonId);
                    person.AvatarPictureUrl = blockBlob.Uri.ToString();
                    await this.db.UpdatePerson(person);
                }

                return this.Request.CreateResponse(HttpStatusCode.OK, blockBlob.Uri.ToString());
            }
            catch (Exception ex)
            {
                return this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to upload the file. Internal error. ");
            }

        }
    }
}
