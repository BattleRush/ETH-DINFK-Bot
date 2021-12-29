using Discord;
using Discord.WebSocket;
using ETHDINFKBot.Classes;
using ETHDINFKBot.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETHDINFKBot.CronJobs.Jobs
{
    public class JWSTUpdates : CronJobService
    {
        private readonly ulong ServerSuggestion = 816776685407043614; // todo config?
        private readonly ILogger<JWSTUpdates> _logger;
        private readonly string Name = "JWSTUpdates";

        private readonly ulong GuildId = 747752542741725244;
        private readonly ulong ChannelId = 817846795367481344; // todo config?
        public JWSTUpdates(IScheduleConfig<JWSTUpdates> config, ILogger<JWSTUpdates> logger)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} starts.");
            return base.StartAsync(cancellationToken);
        }

        // Duplicate from SpaceModule
        private JWSTStatus GetJWSTStatus()
        {
            using (WebClient w = new WebClient())
            {
                var timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                var json = w.DownloadString($"https://webb.nasa.gov/content/webbLaunch/flightCurrentState2.0.json?unique={timeStamp}");

                return JsonConvert.DeserializeObject<JWSTStatus>(json);
            }
        }

        private JWSTDeploymentInfos GetJWSTDeployments()
        {
            var json = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Data", "JWSTDeployments.json"));
            return JsonConvert.DeserializeObject<JWSTDeploymentInfos>(json);
        }
        private async void JWSTTask()
        {
            var guild = Program.Client.GetGuild(GuildId);
            var textChannel = guild.GetTextChannel(ChannelId);

            var lastDeploymentIndex = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Data", "CurrentJWSTIndex.txt"));

            int index = Convert.ToInt32(lastDeploymentIndex);

            var currentStatus = GetJWSTStatus().currentState;
            if (currentStatus.currentDeployTableIndex != index)
            {
                // New state
                var deploymentInfo = GetJWSTDeployments();

                var currentDeployment = deploymentInfo.info[currentStatus.currentDeployTableIndex];

                EmbedBuilder builder = new();

                builder.WithTitle($"JWST Next Deployment Stage: {currentDeployment.title}");

                builder.WithDescription($@"**{currentDeployment.subtitle}** {Environment.NewLine} {currentDeployment.details.Substring(0, currentDeployment.details.IndexOf("<"))}");

                builder.WithColor(255, 215, 0);
                builder.WithThumbnailUrl("https://webb.nasa.gov/" + currentDeployment.thumbnailUrl);
                builder.WithImageUrl("https://webb.nasa.gov/" + currentDeployment.stateImageUrl);
                builder.WithCurrentTimestamp();

                builder.AddField($"Temp Warm side", $"{currentStatus.tempWarmSide1C}/{currentStatus.tempWarmSide2C}", true);
                builder.AddField($"Temp Cool side", $"{currentStatus.tempCoolSide1C}/{currentStatus.tempCoolSide2C}", true);

                await textChannel.SendMessageAsync("", false, builder.Build());

                File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "Data", "CurrentJWSTIndex.txt"), currentStatus.currentDeployTableIndex.ToString());
            }
        }

        public override Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} {Name} is working.");

            try
            {
                JWSTTask();
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
