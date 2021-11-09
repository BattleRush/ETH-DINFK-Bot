using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETHDINFKBot.CronJobs.Jobs
{

    public class GitPullMessageJob : CronJobService
    {
        private readonly ILogger<GitPullMessageJob> _logger;
        private readonly string Name = "BackupDBJob";

        public GitPullMessageJob(IScheduleConfig<BackupDBJob> config, ILogger<GitPullMessageJob> logger)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} starts.");
            return base.StartAsync(cancellationToken);
        }

        public async void SendMeme()
        {
            try
            {
                ulong guildId = 747752542741725244;
                ulong eprogChannel = 755401575790280826;
                var guild = Program.Client.GetGuild(guildId);
                var textChannel = guild.GetTextChannel(eprogChannel);

                await textChannel.SendMessageAsync("It's Tuesday again. Dont forget this before you submit your EProg solutions");
                await textChannel.SendMessageAsync("https://memegenerator.net/img/instances/75177399/if-you-dont-learn-to-git-pull-before-your-git-push-youre-gonna-have-a-bad-time.jpg");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed Backup");
                //textChannel.SendMessageAsync("Failed DB Backup: " + ex.Message);
            }
        }

        public override Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} {Name} is working.");

            SendMeme(); 

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
