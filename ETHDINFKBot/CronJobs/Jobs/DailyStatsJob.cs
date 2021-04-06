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
        private readonly ulong GeneralChatId = 747752542741725247; // todo config?
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
            foreach (var item in Program.Client.Guilds)
            {
                var generalChannel = item.GetTextChannel(GeneralChatId);
                if (generalChannel != null)
                {
                    //generalChannel.SendMessageAsync($"{Name} is running");
                }
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
