using FantasyDead.Data.Documents;
using FantasyDead.Web.Models;
using Microsoft.Azure.NotificationHubs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace FantasyDead.Web.Parts
{
    public class PushService
    {
        private readonly NotificationHubClient hub;

        public PushService()
        {
            this.hub = NotificationHubClient.CreateClientFromConnectionString(ConfigurationManager.AppSettings["notificationHub"], "push");
        }

        /// <summary>
        /// Helper method for deadline tags.
        /// </summary>
        /// <param name="hours"></param>
        /// <returns></returns>
        public static string DeadlineTag(int hours)
        {
            return $"deadline-{hours}";
        }

        /// <summary>
        /// Adds or updates a registration.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="deadlineReminderHours"></param>
        /// <param name="personId"></param>
        /// <returns></returns>
        public async Task<string> AddOrUpdateRegistration(PushRequest req, int? deadlineReminderHours, string personId)
        {

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
                        throw new ArgumentException("Push notification is not supported for this device");
                    }
            }
            reg.Tags = new HashSet<string>();
            reg.Tags.Add(personId);
            if (deadlineReminderHours.HasValue)
                reg.Tags.Add(PushService.DeadlineTag(deadlineReminderHours.Value));

            reg = await this.hub.CreateOrUpdateRegistrationAsync(reg);
            return reg.RegistrationId;
        }

        public async Task RemoveRegistration(string id)
        {
            await this.hub.DeleteRegistrationAsync(id);
        }


        public async Task AddTag(string id, string tag)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException("Id or tag is null.");

            var regs = await this.hub.GetRegistrationsByTagAsync(id, 1);
            if (!regs.Any())
                throw new ArgumentException("PersonId tag not found.", nameof(id));

            var actual = regs.First();

            if (!actual.Tags.Contains(tag))
            {
                actual.Tags.Add(tag);
                await this.hub.UpdateRegistrationAsync(actual);
            }
        }

        public async Task SendNotification(string tag, string message, string device)
        {
            await this.SendNotification(tag, message, "The Fantasy Dead", device);
        }
        
        public async Task SendNotification(string tag, string message, string title, string device)
        {

            switch (device)
            {
                case "android":
                    {
                        var json = @"{'data':{'message':'"+ message +"', 'title':'"+ title +"'}}";
                        await this.hub.SendGcmNativeNotificationAsync(json, tag);
                        break;
                    }
                case "ios":
                    {
                        var json = @"{'aps':{'alert':'"+ message +"'}}";
                        await this.hub.SendAppleNativeNotificationAsync(json, tag);
                        break;
                    }
                default:
                    {
                        throw new NotSupportedException("Invalid device.");
                    }
            }
        }

        private static Lazy<PushService> instance = new Lazy<PushService>(() =>
        {
            return new PushService();
        });

        public static PushService Instance => instance.Value;
    }
}