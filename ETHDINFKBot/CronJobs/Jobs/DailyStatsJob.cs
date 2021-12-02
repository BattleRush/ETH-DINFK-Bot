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

        public override Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} {Name} is working.");
            Console.WriteLine("Run DailyStatsJob");

            var guild = Program.Client.GetGuild(Program.BaseGuild);
            var spamChannel = guild.GetTextChannel(GeneralChatId);
            if (spamChannel != null)
            {
                var res = GenerateMovieLastDay(Program.BaseGuild, spamChannel).Result;
                res = GenerateMovieLastWeek(Program.BaseGuild, spamChannel).Result;
            }

            return Task.CompletedTask;
        }

        private async Task<bool> GenerateMovieLastDay(ulong guildId, SocketTextChannel channel)
        {
            string fileName = await MovieHelper.GenerateMovieForMessages(guildId, 24, 30, -1, 2, true, true, "");
            await channel.SendFileAsync(fileName, "Message graph for last day");
            return true;
        }

        private async Task<bool> GenerateMovieLastWeek(ulong guildId, SocketTextChannel channel)
        {
            string fileName = await MovieHelper.GenerateMovieForMessages(guildId, 24 * 7, 30, -1, 10, true, true, "");
            await channel.SendFileAsync(fileName, "Message graph for last week");
            return true;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
