using CSharpMath.Rendering.FrontEnd;
using Discord;
using Discord.WebSocket;
using ETHDINFKBot.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace ETHDINFKBot.CronJobs.Jobs
{
    public class DailyCleanup : CronJobService
    {
        private readonly ulong ServerSuggestion = 816776685407043614; // todo config?
        private readonly ILogger<DailyCleanup> _logger;
        private readonly string Name = "CleanUp";

        public DailyCleanup(IScheduleConfig<DailyCleanup> config, ILogger<DailyCleanup> logger)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} starts.");
            return base.StartAsync(cancellationToken);
        }

        private void CleanupOldEmotes()
        {
            var fileEndings = new List<string>()
            {
                "*.png", "*.gif"
            };

            var guild = Program.Client.GetGuild(747752542741725244);
            var textChannel = guild.GetTextChannel(768600365602963496);

            foreach (var fileEnding in fileEndings)
            {
                var files = Directory.EnumerateFiles(System.IO.Path.Combine(Program.ApplicationSetting.BasePath, "Emotes"), fileEnding, SearchOption.AllDirectories);
                int deletedFiles = 0;
                foreach (var file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    if (fi.LastAccessTime < DateTime.Now.AddMonths(-3))
                    {
                        fi.Delete();
                        deletedFiles++;
                    }
                }

                //if (textChannel != null && deletedFiles > 10)
                //textChannel.SendMessageAsync($"Found {deletedFiles} emotes to be deleted");
            }
        }
        private async void CleanUpOldMessages(SocketTextChannel channel, TimeSpan toDeleteOlderThan)
        {
            try
            {
                DateTime oneWeekAgo = DateTime.Now.Add(toDeleteOlderThan);
                ulong oneWeekAgoSnowflake = SnowflakeUtils.ToSnowflake(oneWeekAgo);
                var oldMessages = await channel.GetMessagesAsync(oneWeekAgoSnowflake, Direction.Before, 100/*100 should be enough for a while*/).FlattenAsync();
                await channel.DeleteMessagesAsync(oldMessages);

            }
            catch (Exception ex)
            {
                // TODO log
            }

            //var messageDelete = await channel.SendMessageAsync($"Deleting {oldMessages.Count()} messages"); // enable when this message is correct
            //Task.Delay(TimeSpan.FromMinutes(5));
            //messageDelete.DeleteAsync();
        }

        private async void CheckVISAmpel()
        {
            string url = "https://ampel.vis.ethz.ch";

            HttpClient client = new HttpClient();
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (content.Contains("green"))
                {
                    // green
                    await Program.Client.SetStatusAsync(UserStatus.Online);
                    // remove game status
                    await Program.Client.SetGameAsync("");
                }
                else if (content.Contains("yellow"))
                {
                    // yellow
                    await Program.Client.SetGameAsync("🟡 Clean up VIS room >:(", null, ActivityType.Watching);
                    await Program.Client.SetStatusAsync(UserStatus.Idle);
                }
                else if (content.Contains("red"))
                {
                    // red
                    await Program.Client.SetGameAsync("🔴 No more coffee for u", null, ActivityType.Watching);
                    await Program.Client.SetStatusAsync(UserStatus.DoNotDisturb);
                }
                else
                {
                    // unknown
                    await Program.Client.SetGameAsync("🟠 Help?!");
                    await Program.Client.SetStatusAsync(UserStatus.AFK);
                }
            }
        }

        private async void CleanupExpiredEvents()
        {
            var guild = Program.Client.GetGuild(747752542741725244);
            var textChannel = guild.GetTextChannel(819864331192631346);
            var logChannel = guild.GetTextChannel(747768907992924192);

            // get last 100 messages
            var messages = await textChannel.GetMessagesAsync(100).FlattenAsync();

            // seach for messages created by the bot
            var botMessages = messages.Where(i => i.Author.Id == Program.Client.CurrentUser.Id);

            foreach (var botMessage in botMessages)
            {
                try
                {
                    var eventId = botMessage.Content.Split("/").LastOrDefault();

                    if (ulong.TryParse(eventId, out ulong eventIdParsed))
                    {
                        // get active events from discord
                        var discordEvent = guild.GetEvent(eventIdParsed);
                        if (discordEvent == null || discordEvent.Status == GuildScheduledEventStatus.Completed)
                        {
                            // event is not active anymore
                            await botMessage.DeleteAsync();
                            await logChannel.SendMessageAsync($"Deleted expired event {eventIdParsed} with status {discordEvent?.Status}");
                        }
                    }
                    else
                    {
                        await logChannel.SendMessageAsync($"Failed to parse event id from {botMessage.Content}");
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    await logChannel.SendMessageAsync($"Failed to delete event {botMessage.Content} | {ex.ToString()}");
                }
            }
        }

        public async void CleanupCDN()
        {
            return; // TODO
            string[] files = Directory.GetFiles(System.IO.Path.Combine(Program.ApplicationSetting.BasePath, "Emotes"));
            var guild = Program.Client.GetGuild(747752542741725244);
            var textChannel = guild.GetTextChannel(768600365602963496);

            if (textChannel != null)
                textChannel.SendMessageAsync($"Found {files.Length} emotes to be deleted");

            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                if (fi.LastAccessTime < DateTime.Now.AddMonths(-3))
                    fi.Delete();
            }
        }

        public async void RemovePingHell()
        {
            var guild = Program.Client.GetGuild(747752542741725244);
            var textChannel = guild.GetTextChannel(768600365602963496);


            // Get users that havent pinged the role in the last 72h
            var sqlQuery = @"
SELECT 
    PH.FromDiscordUserID, 
    MAX(PH.DiscordMessageId)
FROM PingHistory PH 
LEFT JOIN DiscordUsers DU ON PH.FromDiscordUserId = DU.DiscordUserId 
WHERE PH.DiscordRoleId = 895231323034222593 
GROUP BY PH.FromDiscordUserId
ORDER BY MAX(PH.DiscordMessageId)";


            var queryResult = await SQLHelper.GetQueryResults(null, sqlQuery, true, 5000, true);

            var utcNow = DateTime.UtcNow;

            ulong pingHellRoleId = 895231323034222593;
            var rolePingHell = guild.Roles.FirstOrDefault(i => i.Id == pingHellRoleId);

            //await guild.DownloadUsersAsync(); // Download all users

            foreach (var row in queryResult.Data)
            {
                try
                {
                    var dateTimeLastPing = SnowflakeUtils.FromSnowflake(Convert.ToUInt64(row[1]));

                    if ((utcNow - dateTimeLastPing).TotalHours >= 72)
                    {

                        ulong userId = Convert.ToUInt64(row[0]);
                        // last ping is over 72h

                        if (guild == null)
                            throw new InvalidOperationException("Guild is null");

                        var guildUser = guild.GetUser(userId);
                        if (guildUser == null)
                            continue;

                        if (guildUser.Roles.Any(i => i.Id == pingHellRoleId))
                        {
                            // remove the role from user
                            await guildUser.RemoveRoleAsync(rolePingHell);

                            // send in spam that they are free
                            await textChannel.SendMessageAsync($"<@{userId}> finally escaped PingHell. May you never ping it ever again.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Disable for now
                    await textChannel.SendMessageAsync($"Failed to remove pinghell ID: {row[0]} SnowFlakePing: {row[1]} | {ex.ToString()}");
                    //if(ex.InnerException != null)
                    //{
                    //    await textChannel.SendMessageAsync($"InnerException: {ex.InnerException.ToString()}");
                    //}
                }
            }
        }

        public async Task SyncVisEvents()
        {
            await DiscordHelper.SyncVisEvents(
                747752542741725244, // GuildId
                747768907992924192, // AdminBot Channel
                819864331192631346); // Events Channel
        }

        public override Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} {Name} is working.");

            try
            {
                foreach (var item in Program.Client.Guilds)
                {
                    var channel = item.GetTextChannel(ServerSuggestion);
                    if (channel != null)
                    {
                        // dont clean up server suggestions
                        //CleanUpOldMessages(channel, TimeSpan.FromDays(-7));
#if !DEBUG
                        RemovePingHell();
                        CleanupOldEmotes();
                        SyncVisEvents();
                        CleanupExpiredEvents();
                        CheckVISAmpel();
                        //CleanupCDN();
#endif
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error cleaning up suggestions", ex);
            }

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name}  is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
