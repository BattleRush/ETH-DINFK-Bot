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


    public class StartAllSubredditsJobs : CronJobService
    {
        private readonly ulong GuildId = 747752542741725244;
        private readonly ulong SpamChannelId = 768600365602963496; // todo config?

        private readonly ILogger<StartAllSubredditsJobs> _logger;

        private readonly string Name = "StartAllSubredditsJobs";

        public StartAllSubredditsJobs(IScheduleConfig<StartAllSubredditsJobs> config, ILogger<StartAllSubredditsJobs> logger)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} starts.");
            return base.StartAsync(cancellationToken);
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} {Name} is working.");
            //Console.WriteLine("Run {Name}");

            var guild = Program.Client.GetGuild(GuildId);
            var textChannel = guild.GetTextChannel(SpamChannelId);


            // TODO Duplicate code from Admin code

            var allSubreddits = DatabaseManager.Instance().GetSubredditsByStatus(false);

            var allNames = allSubreddits.Select(i => i.SubredditName).ToList();

            await textChannel.SendMessageAsync($"Starting Subreddit Reload (CronJob)", false);

            for (int i = 0; i < allNames.Count; i += 100)
            {
                var items = allNames.Skip(i).Take(100);
                await textChannel.SendMessageAsync($"{string.Join(", ", items)}", false);
                // Do something with 100 or remaining items
            }

            await textChannel.SendMessageAsync($"Please wait :)", false);


            await CommonHelper.ScrapReddit(allNames, textChannel);


            _logger.LogInformation($"{Name} is done.");
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
