using Discord;
using Discord.Commands;
using ETHBot.DataLayer.Data.Enums;
using ETHDINFKBot.Classes;
using ETHDINFKBot.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Modules
{
    [Group("space")]
    public class SpaceModule : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public async Task EmoteHelp()
        {
            if (!CommonHelper.AllowedToRun(BotPermissionType.EnableType2Commands, Context.Message.Channel.Id, Context.Message.Author.Id))
                return;

            var author = Context.Message.Author;

            EmbedBuilder builder = new();

            builder.WithTitle($"{Program.Client.CurrentUser.Username} Help");
            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");

            builder.WithDescription($@"Space Help");

            builder.WithColor(64, 64, 255);
            builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
            builder.WithCurrentTimestamp();

            builder.AddField($"{Program.CurrentPrefix}space help", $"This page");
            builder.AddField($"{Program.CurrentPrefix}space jwst status", $"Get the current deployment info and status about the James Webb Space Telescope");

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Group("jwst")]
        public class JWSTModule : ModuleBase<SocketCommandContext>
        {
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
                var cultureInfo = new CultureInfo("en-US");
                for (int i = 0; i < data.info.Length; i++)
                {
                    var curInfo = data.info[i];
                    if (curInfo.timeStampUtc == null)
                        continue;

                    var dateInfo = DateTime.ParseExact(curInfo.timeStampUtc, "yyyy/MM/dd-HH:mm:ss.fff", CultureInfo.InvariantCulture);

                    // Fix broken data info -> shift by 7 days
                    dateInfo = dateInfo.AddDays(7);

                    if (DateTime.UtcNow.AddHours(-1) < dateInfo)
                    {
                        // current flight data
                        return curInfo;
                    }
                }

                return null;
            }

            [Command("status")]
            public async Task JWSTInfo()
            {
                //https://webb.nasa.gov/content/webbLaunch/assets/images/deployment/1000pxWide/122.png

                var currentStatus = GetJWSTStatus().currentState;
                var deploymentInfo = GetJWSTDeployments();

                var flightData = GetJWSTFlightData();
                var currentFlightData = GetCurrentFlightInfo(flightData);

                var currentDeployment = deploymentInfo.info[currentStatus.currentDeployTableIndex];

                EmbedBuilder builder = new();

                builder.WithTitle($"JWST Current Info: {currentDeployment.title}");

                builder.WithDescription($@"**{currentDeployment.subtitle}**{Environment.NewLine}{currentDeployment.nominalEventTime}{Environment.NewLine}{currentDeployment.details.Substring(0, currentDeployment.details.IndexOf("<"))}");

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

                builder.AddField($"Sensor info", $"[Temperature Sensor location image](https://www.jwst.nasa.gov/content/webbLaunch/assets/images/extra/webbTempLocationsGradient1.0-500px.jpg)", false);
               
                builder.AddField($"Altitude", $"{currentFlightData?.altitudeKm:N0} km", true);
                var firstLink = currentDeployment.relatedLinks.FirstOrDefault();
                if (firstLink != null)
                    builder.AddField($"More Info", $"[{firstLink.name}]({(firstLink.url.StartsWith("https://") ? firstLink.url : "https://www.jwst.nasa.gov/" + firstLink.url)})", false);

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            //[Command("deploy")]
            //public async Task JWstDeploy()
            //{

            //    // var timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();


            //}
        }
    }
}
