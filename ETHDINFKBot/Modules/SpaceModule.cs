using Discord;
using Discord.Commands;
using ETHBot.DataLayer.Data.Enums;
using ETHDINFKBot.Classes;
using ETHDINFKBot.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
                var json = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Data", "JWSTDeployments.json"));
                return JsonConvert.DeserializeObject<JWSTDeploymentInfos>(json);
            }

            [Command("status")]
            public async Task JWSTInfo()
            {
                //https://webb.nasa.gov/content/webbLaunch/assets/images/deployment/1000pxWide/122.png

                var currentStatus = GetJWSTStatus().currentState;
                var deploymentInfo = GetJWSTDeployments();

                var currentDeployment = deploymentInfo.info[currentStatus.currentDeployTableIndex];

                EmbedBuilder builder = new();

                builder.WithTitle($"JWST Current Info: {currentDeployment.title}");

                builder.WithDescription($@"**{currentDeployment.subtitle}** {Environment.NewLine} {currentDeployment.details.Substring(0, currentDeployment.details.IndexOf("<"))}");

                builder.WithColor(255, 215, 0);
                builder.WithThumbnailUrl("https://webb.nasa.gov/" + currentDeployment.thumbnailUrl);
                builder.WithImageUrl("https://webb.nasa.gov/" + currentDeployment.stateImageUrl);
                builder.WithCurrentTimestamp();

                builder.AddField($"Temp Warm side", $"{currentStatus.tempWarmSide1C}/{currentStatus.tempWarmSide2C}", true);
                builder.AddField($"Temp Cool side", $"{currentStatus.tempCoolSide1C}/{currentStatus.tempCoolSide2C}", true);

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
