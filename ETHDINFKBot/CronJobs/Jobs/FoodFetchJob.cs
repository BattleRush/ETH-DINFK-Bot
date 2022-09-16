using Discord.WebSocket;
using ETHDINFKBot.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETHDINFKBot.CronJobs.Jobs
{
    public class FoodFetchJob : CronJobService
    {
        private readonly ulong GeneralChatId = DiscordHelper.DiscordChannels["spam"]; // todo config?
        private readonly ILogger<FoodFetchJob> _logger;
        private readonly string Name = "DailyStatsJob";

        public FoodFetchJob(IScheduleConfig<DailyStatsJob> config, ILogger<FoodFetchJob> logger)
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

            // TODO Maybe send update message if the fetch was successfull
            Stopwatch watch = new Stopwatch();
            watch.Start();
            FoodHelper.LoadMenus();
            watch.Stop();

            var guild = Program.Client.GetGuild(Program.ApplicationSetting.BaseGuild);
            var spamChannel = guild.GetTextChannel(GeneralChatId);
            if (spamChannel != null)
            {
                spamChannel.SendMessageAsync($"Loaded menus in {watch.ElapsedMilliseconds}ms");
            }
            return Task.CompletedTask;
        }



        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
