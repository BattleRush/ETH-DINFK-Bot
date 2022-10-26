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
        private readonly string Name = "FoodFetchJob";

        public FoodFetchJob(IScheduleConfig<FoodFetchJob> config, ILogger<FoodFetchJob> logger)
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
            Console.WriteLine("Run FoodFetchJob");

            // TODO Maybe send update message if the fetch was successfull
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var foodHelper = new FoodHelper();
            foodHelper.LoadMenus(-1, true);
            watch.Stop();

            var guild = Program.Client.GetGuild(Program.ApplicationSetting.BaseGuild);
            var spamChannel = guild.GetTextChannel(GeneralChatId);
            
            // Message only if it took longer than 10 seconds -> some load happened
            if (spamChannel != null && watch.ElapsedMilliseconds > 10_000)
                spamChannel.SendMessageAsync($"Loaded menus in {Math.Round(watch.ElapsedMilliseconds / 1000d, 2)} sec(s)"); // Output in seconds

            return Task.CompletedTask;
        }



        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
