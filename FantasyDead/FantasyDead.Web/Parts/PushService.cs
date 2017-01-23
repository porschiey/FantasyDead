using FantasyDead.Data.Documents;
using FantasyDead.Web.Models;
using Microsoft.Azure.NotificationHubs;
using Newtonsoft.Json;
using StackExchange.Redis;
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
        private readonly IDatabase cache;

        public PushService()
        {
            this.hub = NotificationHubClient.CreateClientFromConnectionString(ConfigurationManager.AppSettings["notificationHub"], "push");
            this.cache = RedisCache.Connection.GetDatabase();
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
            try
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

                reg = await this.hub.CreateRegistrationAsync(reg);
                return reg.RegistrationId;

            }
            catch (Exception ex)
            {
                //log it
                throw;
            }
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


        public async Task ScheduleReminders(DateTime locktime)
        {
            const string redisKey = "reminder-ids";
            //first, fetch pre-existing reminders
            if (this.cache.KeyExists(redisKey))
            {
                var reminderIdsJson = this.cache.StringGet(redisKey);
                var reminderIds = JsonConvert.DeserializeObject<List<string>>(reminderIdsJson);
                foreach (var id in reminderIds)
                {
                    await this.hub.CancelNotificationAsync(id);
                }

                this.cache.KeyDelete(redisKey);
            }

            var tags = new List<int>
            {
                1, 2, 4, 6, 12, 24, 48, 72
            };

            var scheduledIds = new List<string>();
            foreach (var tag in tags)
            {
                try
                {

                    var gcmReminder = @"{'data':{'message':'Don\'t forget to select your characters this week.', 'title':'Rosters lock in " + tag + " hour(s)!'}}";
                    var appleReminder = @"{'aps':{'alert':'Don\'t forget, rosters lock in " + tag + " hour(s)!'}}";

                    var sendDate = locktime.Subtract(TimeSpan.FromMinutes(tag));
                    if (sendDate < DateTime.UtcNow)
                        continue;

                    Notification gcmNote = new GcmNotification(gcmReminder);
                    var gcmScheduled = await this.hub.ScheduleNotificationAsync(gcmNote, sendDate, DeadlineTag(tag));

                    Notification appleNote = new GcmNotification(appleReminder);
                    var appleScheduled = await this.hub.ScheduleNotificationAsync(gcmNote, sendDate, DeadlineTag(tag));

                    scheduledIds.Add(gcmScheduled.ScheduledNotificationId);
                    scheduledIds.Add(appleScheduled.ScheduledNotificationId);

                }
                catch (Exception ex)
                {

                    throw;
                }
            }

            var newReminderIdsJson = JsonConvert.SerializeObject(scheduledIds);
            this.cache.StringSet(redisKey, newReminderIdsJson);
        }

        public async Task SendNotification(string tag, string message, string title, string device)
        {

            switch (device)
            {
                case "android":
                    {
                        var json = @"{'data':{'message':'" + message + "', 'title':'" + title + "'}}";
                        await this.hub.SendGcmNativeNotificationAsync(json, tag);
                        break;
                    }
                case "ios":
                    {
                        var json = @"{'aps':{'alert':'" + message + "'}}";
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