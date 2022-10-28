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
        public override Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} {Name} is working.");
            Console.WriteLine("Run DailyStatsJob");

            var guild = Program.Client.GetGuild(Program.ApplicationSetting.BaseGuild);
            var spamChannel = guild.GetTextChannel(GeneralChatId);
            if (spamChannel != null)
            {
                var res = GenerateMovieLastDay(Program.ApplicationSetting.BaseGuild, spamChannel).Result;
                res = GenerateMovieLastWeek(Program.ApplicationSetting.BaseGuild, spamChannel).Result;
                res = GenerateMovieLastWeekStudy(Program.ApplicationSetting.BaseGuild, spamChannel).Result;

                if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday)
                {
                    // Send on each saturday last week
                    res = GenerateMovieLastMonth(Program.ApplicationSetting.BaseGuild, spamChannel).Result;
                    res = GenerateMovieLastMonthStudy(Program.ApplicationSetting.BaseGuild, spamChannel).Result;
                }

                if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday && DateTime.Now.Day < 8)
                { 
                    // On the first saturday of the month send last year
                    res = GenerateMovieLastYear(Program.ApplicationSetting.BaseGuild, spamChannel).Result;
                    res = GenerateMovieLastYearStudy(Program.ApplicationSetting.BaseGuild, spamChannel).Result;
                }
            }

            return Task.CompletedTask;
        }

        private async Task<bool> GenerateMovieLastDay(ulong guildId, SocketTextChannel channel)
        {
            string fileName = await MovieHelper.GenerateMovieForMessages(guildId, 24, 60, -1, 1, true, true, "");
            await channel.SendFileAsync(fileName, "Message graph for last day");
            return true;
        }

        private async Task<bool> GenerateMovieLastWeek(ulong guildId, SocketTextChannel channel)
        {
            string fileName = await MovieHelper.GenerateMovieForMessages(guildId, 24 * 7, 60, -1, 10, true, true, "");
            await channel.SendFileAsync(fileName, "Message graph for last week");
            return true;
        }

        private async Task<bool> GenerateMovieLastWeekStudy(ulong guildId, SocketTextChannel channel)
        {
            // TODO Load from config
            string fileName = await MovieHelper.GenerateMovieForMessages(guildId, 24 * 7, 60, -1, 10, true, true, "", StudyChannels);
            await channel.SendFileAsync(fileName, "Message graph for last week (Only study channels)");
            return true;
        }

        private async Task<bool> GenerateMovieLastMonth(ulong guildId, SocketTextChannel channel)
        {
            string fileName = await MovieHelper.GenerateMovieForMessages(guildId, 24 * 30, 60, 1, -1, true, true, "");
            await channel.SendFileAsync(fileName, "Message graph for last month");
            return true;
        }


        private async Task<bool> GenerateMovieLastMonthStudy(ulong guildId, SocketTextChannel channel)
        {
            string fileName = await MovieHelper.GenerateMovieForMessages(guildId, 24 * 30, 60, 1, -1, true, true, "", StudyChannels);
            await channel.SendFileAsync(fileName, "Message graph for last month (Only study channels)");
            return true;
        }

        private async Task<bool> GenerateMovieLastYear(ulong guildId, SocketTextChannel channel)
        {
            string fileName = await MovieHelper.GenerateMovieForMessages(guildId, 24 * 365, 60, 2, -1, true, true, "");
            await channel.SendFileAsync(fileName, "Message graph for last year");
            return true;
        }
        private async Task<bool> GenerateMovieLastYearStudy(ulong guildId, SocketTextChannel channel)
        {
            string fileName = await MovieHelper.GenerateMovieForMessages(guildId, 24 * 365, 60, 2, -1, true, true, "", StudyChannels);
            await channel.SendFileAsync(fileName, "Message graph for last year (Only study channels)");
            return true;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
