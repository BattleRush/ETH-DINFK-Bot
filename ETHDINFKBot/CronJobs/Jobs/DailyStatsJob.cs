using Discord.WebSocket;
using ETHDINFKBot.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Discord;

namespace ETHDINFKBot.CronJobs.Jobs
{
    public class DailyStatsJob : CronJobService
    {
        private readonly ulong GeneralChatId = DiscordHelper.DiscordChannels["spam"]; // todo config?
        private readonly ILogger<DailyStatsJob> _logger;
        private readonly string Name = "DailyStatsJob";

        // TODO Move to Config
        private ulong[] StudyChannels = new ulong[] {747753178208141313
,755401302032515074
,755401370596671520
,755401537706000534
,755401575790280826
,852332052950024243
,987382016104337509
,772551551818268702
,772551583610699808
,772551681870659604
,772551717149343765
,810501721419677736
,819853233514217492
,852331408327835748
,810246017600585820
,810246055207239751
,810246094088831066
,810246149465964544
,852331040814792704
,810245745071357962
,810245786590773339
,810245889247019048
,810501814112747530
,889495223510655016
,810271044010115113
,810501877593931777
,814454253115932734
,814454274913337385
,814454297517097060
,814454319557902336
,810501900775849995
,814453980758802452
,814454012937109584
,814454064044572702
,881631622322073690 };

        public DailyStatsJob(IScheduleConfig<DailyStatsJob> config, ILogger<DailyStatsJob> logger)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} starts.");
            return base.StartAsync(cancellationToken);
        }



        // TODO Cutoff at midnight
        // TODO X Axis is scaled wrong
        public override async Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} {Name} is working.");
            Console.WriteLine("Run DailyStatsJob");



            // Clear /tmp folder
            DirectoryInfo directoryInfo = new DirectoryInfo("/tmp/");
            foreach (FileInfo file in directoryInfo.GetFiles())
                file.Delete();

            // Ensure clean disk before starting
            string currentBasePath = Path.Combine(Program.ApplicationSetting.BasePath, "MovieData");

            if (Directory.Exists(currentBasePath))
                Directory.Delete(currentBasePath, true);

            var guild = Program.Client.GetGuild(Program.ApplicationSetting.BaseGuild);
            var spamChannel = guild.GetTextChannel(GeneralChatId);

            try
            {
                await ILikePingingStaff();
            }
            catch (Exception e)
            {
                await spamChannel.SendMessageAsync("Error: " + e.ToString().Substring(0, Math.Min(e.ToString().Length, 1980)));
            }

            if (spamChannel != null)
            {
                try
                {
                    var res = GenerateMovieLastDay(Program.ApplicationSetting.BaseGuild, spamChannel).Result;
                    //res = GenerateMovieLastDayStudy(Program.ApplicationSetting.BaseGuild, spamChannel).Result;

                    //res = GenerateMovieLastWeek(Program.ApplicationSetting.BaseGuild, spamChannel).Result;
                    //res = GenerateMovieLastWeekStudy(Program.ApplicationSetting.BaseGuild, spamChannel).Result;

                    if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday)
                    {
                        // Send on each saturday last week
                        //res = GenerateMovieLastMonth(Program.ApplicationSetting.BaseGuild, spamChannel).Result;
                        //res = GenerateMovieLastMonthStudy(Program.ApplicationSetting.BaseGuild, spamChannel).Result;
                    }

                    if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday && DateTime.Now.Day < 8)
                    {
                        // On the first saturday of the month send last year
                        //res = GenerateMovieLastYear(Program.ApplicationSetting.BaseGuild, spamChannel).Result;
                        //res = GenerateMovieLastYearStudy(Program.ApplicationSetting.BaseGuild, spamChannel).Result;
                    }
                }
                catch (Exception e)
                {
                    await spamChannel.SendMessageAsync("Error: " + e.ToString());
                }
            }

            return;
        }

        private async Task ILikePingingStaff()
        {
            // get users with role id 747753814723002500 (admin) or 815932497920917514 (mod) -> TODO move to config

            // Get Messages from pull request
            var guild = Program.Client.GetGuild(Program.ApplicationSetting.BaseGuild);

            List<ulong> usersToPing = new List<ulong>();

            var adminRole = guild.GetRole(747753814723002500);
            var modRole = guild.GetRole(815932497920917514);

            var usersWithAdminRole = guild.Users.Where(x => x.Roles.Contains(adminRole));
            var usersWithModRole = guild.Users.Where(x => x.Roles.Contains(modRole));

            foreach (var user in usersWithAdminRole)
                usersToPing.Add(user.Id);

            foreach (var user in usersWithModRole)
                usersToPing.Add(user.Id);

            usersToPing = usersToPing.Distinct().ToList();

            var pullRequestChannel = guild.GetTextChannel(816279194321420308); // TODO 
            if (pullRequestChannel != null)
            {
                var currentUsersToPing = usersToPing.ToList();

                var threads = await pullRequestChannel.GetActiveThreadsAsync();
                var messages = await pullRequestChannel.GetMessagesAsync(100).FlattenAsync();

                foreach (IMessage message in messages)
                {
                    if (message.Timestamp < DateTimeOffset.UtcNow.AddDays(-3))
                        continue;

                    bool skip = false;
                    // loop trough users that reacted with any reaction
                    foreach (var reaction in message.Reactions)
                    {
                        // if the reaction is either plusplus or minusminus skip
                        if (reaction.Key.Name == "plusplus" || reaction.Key.Name == "minusminus")
                        {
                            skip = true;
                            break;
                        }

                        // get people who reacted with this reaction
                        var users = await message.GetReactionUsersAsync(reaction.Key, 100).FlattenAsync();

                        // remove this users from usersToPing
                        foreach (var user in users)
                            currentUsersToPing.Remove(user.Id);
                    }

                    if (skip)
                        continue;

                    if (message.Type == MessageType.ThreadCreated || message.Type == MessageType.ThreadStarterMessage || message.Type == MessageType.ThreadCreated)
                        continue;

                    // check if any users are left to be pinged
                    try
                    {
                        if (currentUsersToPing.Count > 0)
                        {
                            var thread = threads.FirstOrDefault(i => i.Id == message.Id);

                            // ping users
                            string pingString = "Following admins are too lazy to react: ";
                            foreach (var user in currentUsersToPing)
                                pingString += $"<@{user}> ";

                            if (thread != null)
                            {
                                await thread.SendMessageAsync(pingString);
                            }
                            else
                            {
                                // thread not found create for the current message
                                var threadCreated = await pullRequestChannel.CreateThreadAsync("Discussion (Non automatic)", message: message);
                                await threadCreated.SendMessageAsync(pingString);
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
        }


        // TODO Alot of duplicate code rework that

        private async Task<bool> GenerateMovieLastDay(ulong guildId, SocketTextChannel channel)
        {
            string fileName = await MovieHelper.GenerateMovieForMessages(guildId, 24, 24, -1, 10, true, true, "");
            await channel.SendFileAsync(fileName, "Message graph for last day");

            // Delete file
            File.Delete(fileName);

            return true;
        }
        private async Task<bool> GenerateMovieLastDayStudy(ulong guildId, SocketTextChannel channel)
        {
            string fileName = await MovieHelper.GenerateMovieForMessages(guildId, 24, 30, -1, 2, true, true, "", StudyChannels);
            await channel.SendFileAsync(fileName, "Message graph for last day (Only study channels)");

            // Delete file
            File.Delete(fileName);

            return true;
        }

        private async Task<bool> GenerateMovieLastWeek(ulong guildId, SocketTextChannel channel)
        {
            string fileName = await MovieHelper.GenerateMovieForMessages(guildId, 24 * 7, 30, -1, 12, true, true, "");
            await channel.SendFileAsync(fileName, "Message graph for last week");

            // Delete file
            File.Delete(fileName);

            return true;
        }

        private async Task<bool> GenerateMovieLastWeekStudy(ulong guildId, SocketTextChannel channel)
        {
            // TODO Load from config
            string fileName = await MovieHelper.GenerateMovieForMessages(guildId, 24 * 7, 30, -1, 12, true, true, "", StudyChannels);
            await channel.SendFileAsync(fileName, "Message graph for last week (Only study channels)");

            // Delete file
            File.Delete(fileName);

            return true;
        }

        private async Task<bool> GenerateMovieLastMonth(ulong guildId, SocketTextChannel channel)
        {
            string fileName = await MovieHelper.GenerateMovieForMessages(guildId, 24 * 30, 60, -1, 20, true, true, "");
            await channel.SendFileAsync(fileName, "Message graph for last month");

            // Delete file
            File.Delete(fileName);

            return true;
        }


        private async Task<bool> GenerateMovieLastMonthStudy(ulong guildId, SocketTextChannel channel)
        {
            string fileName = await MovieHelper.GenerateMovieForMessages(guildId, 24 * 30, 60, -1, 20, true, true, "", StudyChannels);
            await channel.SendFileAsync(fileName, "Message graph for last month (Only study channels)");

            // Delete file
            File.Delete(fileName);

            return true;
        }

        private async Task<bool> GenerateMovieLastYear(ulong guildId, SocketTextChannel channel)
        {
            string fileName = await MovieHelper.GenerateMovieForMessages(guildId, 24 * 365, 60, 2, -1, true, true, "");
            await channel.SendFileAsync(fileName, "Message graph for last year");

            // Delete file
            File.Delete(fileName);

            return true;
        }
        private async Task<bool> GenerateMovieLastYearStudy(ulong guildId, SocketTextChannel channel)
        {
            string fileName = await MovieHelper.GenerateMovieForMessages(guildId, 24 * 365, 60, 2, -1, true, true, "", StudyChannels);
            await channel.SendFileAsync(fileName, "Message graph for last year (Only study channels)");

            // Delete file
            File.Delete(fileName);

            return true;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
