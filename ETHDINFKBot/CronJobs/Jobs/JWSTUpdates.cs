using Discord;
using Discord.WebSocket;
using ETHDINFKBot.Classes;
using ETHDINFKBot.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
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
            var json = File.ReadAllText(Path.Combine(Program.ApplicationSetting.BasePath, "Data", "JWSTDeployments.json"));
            return JsonConvert.DeserializeObject<JWSTDeploymentInfos>(json);
        }


        private JWSTFlightData GetJWSTFlightData()
        {
            var json = File.ReadAllText(Path.Combine(Program.ApplicationSetting.BasePath, "Data", "JWSTFlightData.json"));
            return JsonConvert.DeserializeObject<JWSTFlightData>(json);
        }

        private JWSTFlightDataInfo GetCurrentFlightInfo(JWSTFlightData data)
        {
            for (int i = 0; i < data.info.Length; i++)
            {
                var curInfo = data.info[i];
                if (curInfo.timeStampUtc == null)
                    continue;

                var dateInfo = DateTime.ParseExact(curInfo.timeStampUtc, "yyyy/MM/dd-HH:mm:ss.fff", CultureInfo.InvariantCulture);

                if (DateTime.UtcNow.AddHours(-1) < dateInfo)
                {
                    // current flight data
                    return curInfo;
                }
            }

            return null;
        }


        private async void JWSTTask()
        {
            var guild = Program.Client.GetGuild(GuildId);
            var textChannel = guild.GetTextChannel(ChannelId);

            var lastDeploymentIndex = File.ReadAllText(Path.Combine(Program.ApplicationSetting.BasePath, "Data", "CurrentJWSTIndex.txt"));

            int index = Convert.ToInt32(lastDeploymentIndex);

            var currentStatus = GetJWSTStatus().currentState;
            if (currentStatus.currentDeployTableIndex > index)
            {
                // New state
                var deploymentInfo = GetJWSTDeployments();

                var flightData = GetJWSTFlightData();
                var currentFlightData = GetCurrentFlightInfo(flightData);

                var currentDeployment = deploymentInfo.info[currentStatus.currentDeployTableIndex];

                EmbedBuilder builder = new();

                builder.WithTitle($"JWST Next Deployment Stage: {currentDeployment.title}");

                builder.WithDescription($@"**{currentDeployment.subtitle}** {Environment.NewLine} {currentDeployment.details.Substring(0, currentDeployment.details.IndexOf("<"))}");

                builder.WithColor(255, 215, 0);
                builder.WithThumbnailUrl("https://www.jwst.nasa.gov/content/webbLaunch/assets/images/branding/logo/logoOnly-0.5x.png");
                builder.WithImageUrl("https://webb.nasa.gov/" + currentDeployment.stateImageUrl);
                builder.WithCurrentTimestamp();

                decimal hotTemp1C = Convert.ToDecimal(currentStatus.tempWarmSide1C, new CultureInfo("en-US"));
                decimal hotTemp2C = Convert.ToDecimal(currentStatus.tempWarmSide2C, new CultureInfo("en-US"));
                decimal coldTemp1C = Convert.ToDecimal(currentStatus.tempCoolSide1C, new CultureInfo("en-US"));
                decimal coldTemp2C = Convert.ToDecimal(currentStatus.tempCoolSide2C, new CultureInfo("en-US"));

                builder.AddField($"Sunshield UPS Average Temperature (hot)", $"{hotTemp1C:0.00} °C", false);
                builder.AddField($"Spacecraft Equipment Panel Average Temperature (hot)", $"{hotTemp2C:0.00} °C", false);
                builder.AddField($"Primary Mirror Average Temperature (cold)", $"{coldTemp1C:0.00} °C  / {(coldTemp1C + 273.15m):0.00} K", false);
                builder.AddField($"Instrument Radiator Temperature (cold)", $"{coldTemp2C:0.00} °C / {(coldTemp2C + 273.15m):0.00} K", false);

                builder.AddField($"Mission time", $"{currentFlightData?.elapsedDays:0.00} days", true);
                builder.AddField($"Velocity", $"{currentFlightData?.velocityKmSec:0.000} km/s", true);
                builder.AddField($"Altitude", $"{currentFlightData?.altitudeKm:N0} km", true);

                builder.AddField($"Sensor info", $"[Temperature Sensor location image](https://www.jwst.nasa.gov/content/webbLaunch/assets/images/extra/webbTempLocationsGradient1.0-500px.jpg)", false);

                await textChannel.SendMessageAsync("", false, builder.Build());

                // Limit to 1 change per index at once
                File.WriteAllText(Path.Combine(Program.ApplicationSetting.BasePath, "Data", "CurrentJWSTIndex.txt"), (index++).ToString());
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
