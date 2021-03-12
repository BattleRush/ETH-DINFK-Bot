using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETHDINFKBot.CronJobs
{
    public class CronJobTest : CronJobService
    {
        private readonly ILogger<CronJobTest> _logger;

        public CronJobTest(IScheduleConfig<CronJobTest> config, ILogger<CronJobTest> logger)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("CronJobTest starts.");
            return base.StartAsync(cancellationToken);
        }

        public override Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} CronJobTest is working.");
            Console.WriteLine("Run");
            foreach (var item in Program.Client.Guilds)
            {
                var spamChannel = item.GetTextChannel(768600365602963496);
                if(spamChannel != null)
                {
                    //spamChannel.SendMessageAsync("Cronjob is running");
                }
            } 
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("CronJobTest is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }


}
