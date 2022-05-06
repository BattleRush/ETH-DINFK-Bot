﻿using Discord;
using Discord.WebSocket;
using ETHDINFKBot.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        public async void CleanupCDN()
        {

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
                        CleanUpOldMessages(channel, TimeSpan.FromDays(-7));
#if !DEBUG
                        RemovePingHell();
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
