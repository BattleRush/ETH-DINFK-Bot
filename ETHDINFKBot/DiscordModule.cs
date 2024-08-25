using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using DuckSharp;
using ETHBot.DataLayer;
using ETHDINFKBot.Log;
using NekosSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ETHBot.DataLayer.Data.Enums;
using ETHBot.DataLayer.Data.Discord;
using Reddit;
using RedditScrapper;
using Reddit.Controllers;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;
using System.Net;
using ETHDINFKBot.Helpers;
using SkiaSharp;
using CSharpMath.SkiaSharp;
using System.Globalization;
using System.Diagnostics;

using ETHDINFKBot.Drawing;
using System.Reflection;
using TimeZoneConverter;
using HtmlAgilityPack;
using Google.Apis.CustomSearchAPI.v1;
using Google.Apis.Services;
using Google.Apis.CustomSearchAPI.v1.Data;
using Google;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using ETHDINFKBot.Data;

namespace ETHDINFKBot
{

    //FAA for spacex stuff https://www.faa.gov/data_research/commercial_space_data/launches/
    public class DiscordModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger _logger = new Logger<DiscordModule>(Program.Logger);

        //public static NekoClient NekoClient = new NekoClient("BattleRush's Helper");
        //public static NekosFun NekosFun = new NekosFun();

        static List<RestUserMessage> LastMessages = new List<RestUserMessage>();

        DatabaseManager DatabaseManager = DatabaseManager.Instance();

        LogManager LogManager = new LogManager(DatabaseManager.Instance()); // not needed to pass a singleton actually


        // TODO Remove alot of the redundant code for loggining and stats


        private bool AllowedToRun(BotPermissionType type)
        {
            var channelSettings = DatabaseManager.GetChannelSetting(Context.Message.Channel.Id);
            if (Context.Message.Author.Id != Program.ApplicationSetting.Owner
                && !((BotPermissionType)(channelSettings?.ChannelPermissionFlags ?? 0)).HasFlag(type))
            {
#if DEBUG
                Context.Channel.SendMessageAsync("blocked by perms", false);
#endif
                return true;
            }

            return false;
        }

        [Command("disk")]
        public async Task GetDiskInfo()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            DriveInfo[] allDrives = DriveInfo.GetDrives();
            string diskInfo = "";
            foreach (DriveInfo d in allDrives)
            {
                var freeBytes = d.AvailableFreeSpace;
                var totalBytes = d.TotalSize;
                long usedBytes = totalBytes - freeBytes;
                string driveName = d.Name;

                // convert to GB or TB if needed
                double gb = 1024d * 1024d * 1024d;
                double freeSize = freeBytes / gb;
                double totalSize = totalBytes / gb;
                double usedSize = usedBytes / gb;
                string sizeType = "GB";

                if (totalSize >= 1024)
                {
                    totalSize /= 1024;
                    freeSize /= 1024;
                    usedSize /= 1024;
                    sizeType = "TB";
                }

                diskInfo += $"DISK {driveName}: {Math.Round(usedSize, 2)} / {Math.Round(totalSize, 2)} {sizeType} ({Math.Round(100 * (usedSize / totalSize), 2)}%)" + Environment.NewLine;

                // if disk info over 1500 chars send
                if (diskInfo.Length > 1500)
                {
                    await Context.Channel.SendMessageAsync(diskInfo, false);
                    diskInfo = "";
                }
            }

            await Context.Channel.SendMessageAsync(diskInfo, false);
        }

        [Command("code")]
        [Alias("source")]
        public async Task SourceCode()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Other);

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Source code for BattleRush's Helper (that's me)");
            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");
            builder.WithDescription(@"TODO Create some meaningful text here to go with such an awesome bot.
**Bot Source code: **
**https://github.com/BattleRush/ETH-DINFK-Bot**

**Unity Project Source code: **
**https://github.com/BattleRush/ETHPlaceUnity**");
            builder.WithColor(0, 255, 0);

            //builder.WithThumbnailUrl("https://avatars0.githubusercontent.com/u/11750584");

            var ownerUser = Program.Client.GetUser(Program.ApplicationSetting.Owner);
            builder.WithThumbnailUrl(ownerUser.GetAvatarUrl());
            builder.WithAuthor(ownerUser);
            builder.WithCurrentTimestamp();

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }


        // GET CPU USAGE
        // https://medium.com/@jackwild/getting-cpu-usage-in-net-core-7ef825831b8b
        private static async Task<double> GetCpuUsageForProcess()
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            await Task.Delay(500);

            var endTime = DateTime.UtcNow;
            var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            return cpuUsageTotal * 100;
        }



        [Command("version")]
        [Alias("about")]
        public async Task VersionOutput()
        {
            try
            {
                var currentProcessCpuUsage = GetCpuUsageForProcess();
                var proc = Process.GetCurrentProcess();

                var currentAssembly = Assembly.GetExecutingAssembly();
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                FileVersionInfo fileVersionInfo = Environment.ProcessPath != null ? FileVersionInfo.GetVersionInfo(Environment.ProcessPath) : null;
                string productVersion = fileVersionInfo?.ProductVersion;
                string fileVersion = fileVersionInfo?.FileVersion;
                bool isDebug = fileVersionInfo?.IsDebug ?? false;



                var netCoreVer = Environment.Version;
                var runtimeVer = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
                var osVersion = Environment.OSVersion;
                var applicationOnlineTime = (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime());


                var processorCount = Environment.ProcessorCount;
                var ram = proc.WorkingSet64;
                var threadCount = proc.Threads;
                var totalProcessorTime = proc.TotalProcessorTime;


                // get all partitions available

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"{Program.Client.CurrentUser.Username} Version Info");
                //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");

                string prefix = Program.CurrentPrefix;

                builder.WithDescription($@"For more information about the bot type ""{prefix}help"" or ""{prefix}source""");

                int g = 0;
#if DEBUG
                g = 192;
#endif

                builder.WithColor(0, g, 255);

                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                builder.WithCurrentTimestamp();
                //builder.WithAuthor(author);
                builder.AddField("Version", $"{version?.ToString() ?? "N/A"}", true);
                builder.AddField("Product Version", $"{productVersion ?? "N/A"}", true);
                builder.AddField("File Version", $"{fileVersion ?? "N/A"}", true);
                builder.AddField("Build Mode", $"{(isDebug ? "Debug" : "Release")}", true);
                builder.AddField(".NET Version", $"{netCoreVer?.ToString() ?? "N/A"}", true);
                builder.AddField("Runtime Version", $"{runtimeVer ?? "N/A"}", true);
                builder.AddField("OS Version", $"{osVersion?.ToString() ?? "N/A"}", true);
                builder.AddField("Online for", $"{CommonHelper.ToReadableString(applicationOnlineTime)}", true);
                builder.AddField("Processor Count", $"{processorCount.ToString("N0")}", true);
                builder.AddField("Processor Thread Count", $"{threadCount.Count.ToString("N0")}", true);
                builder.AddField("Processor Total Time", $"{totalProcessorTime}", true);
                builder.AddField("Git Branch", $"{ThisAssembly.Git.Branch}", true);
                builder.AddField("Git Commit", $"{ThisAssembly.Git.Commit}", true);
                builder.AddField("Last Commit", $"{ThisAssembly.Git.CommitDate}", true);
                builder.AddField("Git Tag", $"{ThisAssembly.Git.Tag}", true);

                double cpuUsage = await currentProcessCpuUsage;

                builder.AddField("CPU", $"{Math.Round(cpuUsage, 2)}%", true);
                builder.AddField("RAM", $"{Math.Round(ram / 1024d / 1024d / 1024d, 2)} GB", true);

                DriveInfo[] allDrives = DriveInfo.GetDrives();

                foreach (DriveInfo d in allDrives)
                {
                    var freeBytes = d.AvailableFreeSpace;
                    var totalBytes = d.TotalSize;
                    long usedBytes = totalBytes - freeBytes;
                    string driveName = d.Name;

                    // convert to GB or TB if needed
                    double gb = 1024d * 1024d * 1024d;
                    double freeSize = freeBytes / gb;
                    double totalSize = totalBytes / gb;
                    double usedSize = usedBytes / gb;
                    string sizeType = "GB";

                    if(totalSize < 32)
                    {
                        continue; // skip small disks
                    }

                    if (totalSize >= 1024)
                    {
                        totalSize /= 1024;
                        freeSize /= 1024;
                        usedSize /= 1024;
                        sizeType = "TB";
                    }

                    builder.AddField($"DISK {driveName}", $"{Math.Round(usedSize, 2)} / {Math.Round(totalSize, 2)} {sizeType} ({Math.Round(100 * (usedSize / totalSize), 2)}%)", true);
                }

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message.ToString());
                await Context.Channel.SendMessageAsync(ex.ToString());
            }
        }

        [Command("help")]
        public async Task HelpOutput()
        {
            // _logger.LogError("GET HelpOutput called.");


            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;


            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Other);

            EmbedBuilder builder = new();

            builder.WithTitle($"{Program.Client.CurrentUser.Username} Help");
            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");

            string prefix = ".";

#if DEBUG
            prefix = "dev.";
#endif

            builder.WithDescription($@"Prefix for all commands is ""{prefix}""
Help is in EBNF form, so I hope for you all reading this actually paid attention to Thomas how to use it");



            int g = 0;
#if DEBUG
            g = 192;
#endif

            builder.WithColor(0, g, 255);

            builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

            builder.WithCurrentTimestamp();
            //builder.WithAuthor(author);
            builder.AddField("Misc", $"```{prefix}help {prefix}version {prefix}source {prefix}stats {prefix}ping {prefix}ping {prefix}first {prefix}last {prefix}today```", true);
            builder.AddField("ETH/UZH Food", $"```{prefix}food [ (fav | help | lunch | dinner) ]```", true);
            //builder.AddField("Search", $"```{prefix}google|duck <search term>```", true);
            //builder.AddField("Images", $"```{prefix}neko[avatar] {prefix}fox {prefix}waifu {prefix}baka {prefix}smug {prefix}holo {prefix}avatar {prefix}wallpaper```");
            builder.AddField("Reddit", $"```{prefix}r[p] <subreddit>|all```", true);
            builder.AddField("Rant", $"```{prefix}rant [ (types | new) ]```", true);
            builder.AddField("SQL", $"```{prefix}sql info | (table info) | (query[d] <query>) | dmdb help```", true);
            builder.AddField("Emote Help for more info", $"```{prefix}emote help```");
            builder.AddField("React (only this server's emotes)", $"```{prefix}react <message_id> <emote_name>```", true);
            builder.AddField("Space Min: 1 Max: 5", $"```{prefix}space [<amount>]```", true);
            builder.AddField("Space (for more commands)", $"```{prefix}space help```", true);
            builder.AddField("WIP Command", $"```{prefix}messagegraph [all|lernphase|bp] {prefix}food```", true);
            builder.AddField("ETH DINFK Place", $"```Type '{prefix}place help' for more information```");
            builder.AddField("Create Subject channel", $"```Type '{prefix}create <vvz link>' to create a forumpost for a subject (GESS, Minor or Electives)```");

            /*builder.AddField("Write .study to force yourself away from discord", "```May contain spoilers to old exams! Once you receive the study role you will be only to chat for max of 15 mins at a time." + Environment.NewLine +
               $"If you are in cooldown, the bot will delete all your messages. Every question is designed to be able to solve within 5-10 mins. To recall your message write '.study'" + Environment.NewLine +
               $"To be able to chat you will need to solve a question each time. (All subject channels are exempt from this rule.)```");*/
            //builder.AddField($"Random Exam Question (for now only LinAlg) Total tracking: {new StudyHelper().GetQuestionCount()} question(s)", $"```{prefix}question [Exam] (Exams: {new StudyHelper().GetExams()})```");
            //builder.AddField(".next", "```Regenerate a new question.```");

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("duck")]
        [Summary("Returns info about the current user, or the user parameter, if one passed.")]
        public async Task DuckDuckGo([Remainder] string searchString)
        {

            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            var userInfo = Context.Message.Author;
            //await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");

            LogManager.ProcessMessage(userInfo, BotMessageType.Search);

            var reply = await new DuckSharpClient().GetInstantAnswerAsync(searchString);

            EmbedBuilder builder = new();

            builder.WithTitle("Search for " + searchString);
            builder.WithDescription(reply.AbstractText + Environment.NewLine + reply.AbstractUrl);
            builder.WithColor(128, 128, 0);

            if (reply?.RelatedTopics != null && reply.RelatedTopics.Length > 0)
            {
                builder.Description = reply.RelatedTopics[0].Text;
            }

            if (reply.ImageUrl != null && reply.ImageUrl.Length > 0)
            {
                builder.WithThumbnailUrl($"https://duckduckgo.com{reply.ImageUrl}");
            }

            builder.WithAuthor(userInfo);
            builder.WithFooter($"{userInfo.Username}#{userInfo.Discriminator}");
            builder.WithCurrentTimestamp();

            // TODO Error handling
            foreach (var item in reply.RelatedTopics.Select(i => i).Take(4))
            {
                builder.AddField(item.Text, item.FirstUrl);
            }

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("google")]
        [Summary("Returns info about the current user, or the user parameter, if one passed.")]
        public async Task GoogleSearch([Remainder] string searchString)
        {
            await Context.Channel.SendMessageAsync("Temp disabled", false);

            return; // Disabled for now
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            var userInfo = Context.Message.Author;
            //await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");

            LogManager.ProcessMessage(userInfo, BotMessageType.Search);

            var reply = await new GoogleEngine().Search(searchString);

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Search for " + searchString);
            builder.WithDescription(reply.Description?.Substring(0, reply.Description.Length > 100 ? 100 : reply.Description.Length));
            builder.WithColor(128, 0, 128);

            //builder.Description = reply.RelatedTopics[0].Text;
            if (reply.ImageUrl != null)
            {
                //builder.WithThumbnailUrl(reply.ImageUrl);
            }

            builder.WithAuthor(userInfo);
            builder.WithFooter($"{userInfo.Username}#{userInfo.Discriminator}");
            builder.WithCurrentTimestamp();

            foreach (var item in reply.Results.Take(3))
            {
                //builder.AddField(item.title, item.description + Environment.NewLine + item.url);
            }

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }
        /*
        // TODO Rework
        [Command("neko")]
        public async Task Neko()
        {
            return;
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Neko);

            var req = await NekoClient.Image_v3.Neko();
            var report = GetReportInfoByImage(req.ImageUrl);
            if (report != null)
            {
                var user = DatabaseManager.GetDiscordUserById(report.AddedByDiscordUserId);
                Context.Channel.SendMessageAsync($"The current image has been blocked by {user.Nickname}. Try the command again to get a new image", false);
                return;
            }

            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        [Command("nekogif")]
        public async Task NekoGif()
        {
            return;
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.NekoGif);

            var req = await NekoClient.Image_v3.NekoGif();
            var report = GetReportInfoByImage(req.ImageUrl);
            if (report != null)
            {
                var user = DatabaseManager.GetDiscordUserById(report.AddedByDiscordUserId);
                await Context.Channel.SendMessageAsync($"The current image has been blocked by {user.Nickname}. Try the command again to get a new image", false);
                return;
            }

            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }


        [Command("fox")]
        public async Task Fox()
        {
            return;
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Fox);

            var req = await NekoClient.Image_v3.Fox();
            var report = GetReportInfoByImage(req.ImageUrl);
            if (report != null)
            {
                var user = DatabaseManager.GetDiscordUserById(report.AddedByDiscordUserId);
                Context.Channel.SendMessageAsync($"The current image has been blocked by {user.Nickname}. Try the command again to get a new image", false);
                return;
            }

            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        [Command("waifu")]
        public async Task Waifu()
        {
            return;
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Waifu);

            var req = await NekoClient.Image_v3.Waifu();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        [Command("baka")]
        public async Task Baka()
        {
            return;
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Baka);

            var req = await NekoClient.Image.Baka();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        [Command("smug")]
        public async Task Smug()
        {
            return;
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Smug);

            var req = await NekoClient.Image.Smug();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        [Command("holo")]
        public async Task Holo()
        {
            return;
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            try
            {
                var author = Context.Message.Author;
                LogManager.ProcessMessage(author, BotMessageType.Holo);

                var req = await NekoClient.Image_v3.Holo();

                var report = GetReportInfoByImage(req.ImageUrl);
                if (report != null)
                {
                    var user = DatabaseManager.GetDiscordUserById(report.AddedByDiscordUserId);
                    Context.Channel.SendMessageAsync($"The current image has been blocked by {user.Nickname}. Try the command again to get a new image", false);
                    return;
                }

                Context.Channel.SendMessageAsync(req.ImageUrl, false);
            }
            catch (Exception ex)
            {

            }
        }

        [Command("avatar")]
        public async Task Avatar()
        {
            return;
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Avatar);

            var req = await NekoClient.Image_v3.Avatar();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        [Command("nekoavatar")]
        public async Task NekoAvatar()
        {
            return;
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.NekoAvatar);

            var req = await NekoClient.Image_v3.NekoAvatar();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }*/

        /*[Command("wallpaper")] // TODO INTEGRATE 2 wallpaper endpoints
        public async Task Wallpaper()
        {
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Wallpaper);

            var req = await NekoClient.Image_v3.Wallpaper();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }*/



        [Command("react")]
        public async Task ReactEmote(ulong messageid, string emoteName)
        {
            try
            {
                if (Context.Channel is SocketGuildChannel guildChannel)
                {
                    var emote = guildChannel.Guild.Emotes.FirstOrDefault(i => i.Name.ToLower().Contains(emoteName.ToLower()));

                    if (emote == null)
                        return;

                    var message = await Context.Channel.GetMessageAsync(messageid);
                    await message.AddReactionAsync(emote);
                }
                await Context.Message.DeleteAsync();
            }
            catch (Exception ex)
            {
                // TODO log
            }
        }

        private string GetCountdown(DateTime date)
        {
            var timeSpan = date - DateTime.UtcNow; // calculate off utc timezone

            if (date < DateTime.Now)
                return "ITS OUT NOW !!!!!!";

            var days = timeSpan.Days;
            var hours = timeSpan.Hours;
            var minutes = timeSpan.Minutes;
            var seconds = timeSpan.Seconds;

            var sb = new StringBuilder();
            if (days > 0)
                sb.Append($"{days} days, ");
            if (hours > 0)
                sb.Append($"{hours} hours, ");
            if (minutes > 0)
                sb.Append($"{minutes} minutes, ");

            sb.Append($"{seconds} seconds");

            return sb.ToString();
        }


        // Creating channel is duplicate code from admin module TODO refactor
        private async Task<List<ForumTag>> FindTags(HtmlDocument doc, SocketCommandContext context, List<ForumTag> tags, bool isMaster)
        {
            var list = new List<ForumTag>();

            var table = doc.DocumentNode.SelectSingleNode("//table[@class='wAuto']");
            bool foundAny = false;
            if (table != null)
            {
                var rows = table.Descendants("tr").ToList();
                foreach (var row in rows)
                {
                    // for now we only handle Bachelor case

                    if (!isMaster)
                    {
                        // Check if first column contains "Informatik Bachelor"
                        if (row.ChildNodes[0].InnerText.Contains("Informatik Bachelor"))
                        {
                            foundAny = true;
                            string secondColumnText = row.ChildNodes[1].InnerText;
                            string htmlDecoded = WebUtility.HtmlDecode(secondColumnText);

                            switch (htmlDecoded)
                            {
                                case "Ergänzung":
                                    if (!list.Any(x => x.Name.Contains("BSc Minor")))
                                        list.Add(tags.Find(x => x.Name.Contains("BSc Minor")));
                                    break;
                                case "Wahlfächer":
                                    if (!list.Any(x => x.Name.Contains("BSc Elective")))
                                        list.Add(tags.Find(x => x.Name.Contains("BSc Elective")));
                                    break;
                                case "Seminar":
                                    if (!list.Any(x => x.Name.Contains("BSc Seminar")))
                                        list.Add(tags.Find(x => x.Name.Contains("BSc Seminar")));
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // Check if first column contains "Informatik Master"
                        if (row.ChildNodes[0].InnerText.Contains("Informatik Master"))
                        {
                            // Todo add master tags
                        }
                    }

                    if (row.ChildNodes[0].InnerText.Contains("Wissenschaft im Kontext (Science in Perspective)"))
                    {
                        foundAny = true;
                        // check if list contains this tag already
                        if (!list.Any(x => x.Name.Contains("GESS")))
                            list.Add(tags.Find(x => x.Name.Contains("GESS")));
                    }
                }
            }
            else
            {
                await context.Channel.SendMessageAsync("Could not find table for 'Angeboten in'", false);
                return new List<ForumTag>();
            }

            if (!foundAny && !isMaster)
            {
                await Context.Channel.SendMessageAsync("Could not find any 'Informatik Bachelor' in 'Angeboten in' entries", false);
                return new List<ForumTag>();
            }

            // Discord allows only up to 5 tags
            return list.Distinct().Take(5).ToList();
        }

        private RestThreadChannel CheckIfPostExists(string title)
        {
            List<ulong> forumIds = new List<ulong>();
            forumIds.Add(1067785361062887424); // BSc
            forumIds.Add(1067780019423817779); // MSc

            foreach (var forumId in forumIds)
            {
                var forum = Context.Guild.GetChannel(forumId) as SocketForumChannel;

                var threads = forum.GetActiveThreadsAsync().Result;
                if (threads.Any(t => t.Name.Contains(title)))
                    return threads.First(t => t.Name.Contains(title));

                var archivedThreads = forum.GetPublicArchivedThreadsAsync().Result;
                if (archivedThreads.Any(t => t.Name.Contains(title)))
                    return archivedThreads.First(t => t.Name.Contains(title));
            }

            return null;
        }

        [Command("create")]
        public async Task CreateChannel(string vvzLink)
        {
            Dictionary<string, ulong?> forbiddenSubjects = new Dictionary<string, ulong?>()
            {
                // FS
                { "263-0007-00L", 1077359708635140097 }, // Advanced Systems Lab
                { "263-0008-00L", 1077360555347685500 }, // Computational Intelligence Lab

                // HS
                { "263-0006-00L", 1151807326559412224}, // Algorithms Lab
                { "263-0009-00L", 1151807394259669022}, // Information Security Lab
            };

            // get the domain of the link
            string domain = Regex.Match(vvzLink, @"https?://(www\.)?([^/]*)").Groups[2].Value;

            if (!(domain == "vorlesungen.ethz.ch" || domain == "vvz.ethz.ch"))
            {
                await Context.Channel.SendMessageAsync("Invalid link, only www.vorlesungen.ethz.ch or vvz.ethz.ch is supported, provided domain was: " + domain, false);
                return;
            }

            // template https://www.vorlesungen.ethz.ch/Vorlesungsverzeichnis/lerneinheit.view?semkez=2023S&ansicht=ALLE&lerneinheitId=168463&lang=de

            // get url parameters TODO move to a separate function
            var urlParams = HttpUtility.ParseQueryString(new Uri(vvzLink).Query);

            var semkez = urlParams["semkez"];
            var ansicht = urlParams["ansicht"];
            var lerneinheitId = urlParams["lerneinheitId"];
            var lang = urlParams["lang"];

            ansicht = "ALLE"; // Override view to all
            lang = "de"; // Override language to german

            string newLink = $"https://www.vorlesungen.ethz.ch/Vorlesungsverzeichnis/lerneinheit.view?semkez={semkez}&ansicht={ansicht}&lerneinheitId={lerneinheitId}&lang={lang}";

            // Parse the vvz link html
            string html = new HttpClient().GetStringAsync(newLink).Result;
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            string title = doc.DocumentNode.SelectSingleNode("/html/body/div[2]/section/section[1]/div[1]/h1").InnerText;

            // Find lecture id with regex from title XXX-XXXX-XXL
            string lectureId = Regex.Match(title, @"[0-9]{3}-[0-9]{4}-[0-9A-Z]{3}").Value;
            string lectureName = title.Replace(lectureId, "").Trim();

            // html decode the lecture name
            lectureName = WebUtility.HtmlDecode(lectureName);

            var guildUser = Context.Message.Author as SocketGuildUser;

            // Create the forum channel
            try
            {
                if (Context.Channel is SocketThreadChannel socketThreadChannel)
                {
                    var parent = socketThreadChannel.ParentChannel;
                    if (parent is SocketForumChannel socketForumChannel)
                    {
                        // check if a forum post exists
                        var post = CheckIfPostExists(lectureId);
                        if (post != null)
                        {
                            await Context.Channel.SendMessageAsync($"A forum post for this lecture already exists <#{post.Id}>", false);
                            await post.AddUserAsync(guildUser);
                            return;
                        }
                        /*var posts = await socketForumChannel.GetActiveThreadsAsync();

                        if (posts.Any(p => p.Name.Contains(lectureId)))
                        {
                            var post = posts.First(p => p.Name.Contains(lectureId));
                            await Context.Channel.SendMessageAsync($"A forum post for this lecture already exists <#{post.Id}>", false);
                            await post.AddUserAsync(guildUser);
                            return;
                        }

                        var archivedPosts = await socketForumChannel.GetPublicArchivedThreadsAsync();

                        if (archivedPosts.Any(p => p.Name.Contains(lectureId)))
                        {
                            var post = archivedPosts.First(p => p.Name.Contains(lectureId));
                            await Context.Channel.SendMessageAsync($"An archived forum post for this lecture already exists <#{post.Id}>", false);
                            await post.AddUserAsync(guildUser);
                            return;
                        }*/

                        var tags = socketForumChannel.Tags;

                        // TODO better master forum detection
                        bool isMaster = socketForumChannel.Name.Contains("master");

                        var tagsToAdd = FindTags(doc, Context, tags.ToList(), isMaster).Result;

                        //await Context.Channel.SendMessageAsync($"Found {tagsToAdd.Count} tags to add", false);
                        //await Context.Channel.SendMessageAsync($"Tags: {string.Join(", ", tagsToAdd.Select(i => i.Name))}", false);

                        // If bachelor channel and no tags found then return
                        if ((tagsToAdd == null || tagsToAdd.Count == 0)
                            && !socketForumChannel.Name.Contains("master") && !socketForumChannel.Name.Contains("bot"))
                        {
                            await Context.Channel.SendMessageAsync("Could not find any tags to add", false);
                            return;
                        }

                        // if lectureid is in forbiddenSubjects then return the channel to go for
                        if (forbiddenSubjects.ContainsKey(lectureId))
                        {
                            // todo handle if a course is just banned for example with id = 0
                            
                            await Context.Channel.SendMessageAsync($"This lecture has a dedicated channel, please go to <#{forbiddenSubjects[lectureId]}>", false);
                            return;
                        }

                        var postTitle = $"[{lectureId}] {lectureName}";

                        // title can only be 100 chars
                        if (postTitle.Length > 97)
                            postTitle = postTitle.Substring(0, 97) + "...";

                        var newPost = await socketForumChannel.CreatePostAsync(
                                postTitle,
                                ThreadArchiveDuration.OneWeek,
                                null,
                                "VVZ: " + newLink,
                                tags: tagsToAdd.ToArray());

                        // Get guild user from author
                        await newPost.AddUserAsync(guildUser);

                        await Context.Channel.SendMessageAsync($"Created channel <#{newPost.Id}>", false);
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync("This command can only be used in a forum channel", false);
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync("This command can only be used in a forum channel", false);
                }
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync("Error: " + e.Message, false);
            }
        }

        [Command("ksp2")]
        public async Task Ksp2ReleaseDate()
        {
            var author = Context.Message.Author;

            // Send embed with countdown until KSP2 release on February 24, 2023
            DateTime releaseDate = new DateTime(2023, 2, 24, 14, 0, 0); // 14 UTC launch time
            var embed = new EmbedBuilder();
            embed.WithColor(Color.Blue);

            embed.WithTitle("Kerbal Space Program 2 Release Date");
            embed.WithDescription($"Kerbal Space Program 2 is scheduled to be released on Feb 24, 2023 at 14:00 UTC\n Countdown until release: **{GetCountdown(releaseDate)}**");
            embed.WithImageUrl("https://cdn.cloudflare.steamstatic.com/steam/apps/954850/capsule_616x353.jpg");
            embed.AddField("Steam Store Page", "https://store.steampowered.com/app/954850/Kerbal_Space_Program_2/");
            embed.AddField("Steam DB", "https://steamdb.info/app/954850/history/");

            embed.WithFooter("Kerbal Space Program 2 is a game by Private Division and Squad");

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("pinghell")]
        [Alias("<@&895231323034222593>")]
        public async Task CurrentPinghellMembers()
        {
            try
            {
                var guildChannel = Context.Message.Channel as SocketGuildChannel;
                var guild = guildChannel.Guild;

                // Get users that havent pinged the role in the last 72h
                var sqlQuery = @"
    SELECT 
        PH.FromDiscordUserID, 
        MAX(PH.DiscordMessageId)
    FROM PingHistory PH 
    LEFT JOIN DiscordUsers DU ON PH.FromDiscordUserId = DU.DiscordUserId 
    WHERE PH.DiscordRoleId = 895231323034222593 
    GROUP BY PH.FromDiscordUserId
    ORDER BY MAX(PH.DiscordMessageId)";


                var queryResult = await SQLHelper.GetQueryResults(null, sqlQuery, true, 5000, true);

                var utcNow = DateTime.UtcNow;

                ulong pingHellRoleId = 895231323034222593;
                var rolePingHell = guild.Roles.FirstOrDefault(i => i.Id == pingHellRoleId);

                //await guild.DownloadUsersAsync(); // Download all users


                var usersWithPinghell = rolePingHell.Members.ToList();

                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.WithTitle("Pinghell members");
                embedBuilder.WithColor(Color.Red);
                embedBuilder.WithDescription($"Total members: {usersWithPinghell.Count}");

                int count = 1;
                string messageText = $"**First 10 members to leave**{Environment.NewLine}";
                string currentBuilder = "";

                foreach (var row in queryResult.Data)
                {
                    ulong userId = Convert.ToUInt64(row[0]);

                    if (!usersWithPinghell.Any(i => i.Id == userId))
                        continue;

                    // last ping
                    var datetime = SnowflakeUtils.FromSnowflake(ulong.Parse(row[1]));

                    var dateTimeTillExit = datetime.AddHours(72);
                    // remove minutes and seconds
                    dateTimeTillExit = dateTimeTillExit.AddMinutes(-dateTimeTillExit.Minute);
                    dateTimeTillExit = dateTimeTillExit.AddSeconds(-dateTimeTillExit.Second);

                    // Added 50 mins because the cronjob runs at :50
                    dateTimeTillExit = dateTimeTillExit.AddMinutes(50);

                    var unixTime = dateTimeTillExit.ToUnixTimeSeconds();

                    var user = DatabaseManager.GetDiscordUserById(userId);

                    string line = $"<@{userId}> last pinged at {datetime.AddHours(1).ToString("dd.MM.yyyy HH:mm:ss")} Time left <t:{unixTime}:R>{Environment.NewLine}";

                    if (count <= 10)
                    {
                        messageText += line;
                    }
                    else
                    {
                        currentBuilder += line;

                        if (count % 5 == 0)
                        {
                            embedBuilder.AddField($"First {count - 1} members to leave", currentBuilder, false);
                            currentBuilder = "";
                        }
                    }

                    count++;
                }

                if (!string.IsNullOrEmpty(currentBuilder))
                    embedBuilder.AddField($"First {count - 1} members to leave", currentBuilder, false);

                messageText += Environment.NewLine;
                embedBuilder.WithDescription($"Total Pinghell members: {count - 1}{Environment.NewLine}{Environment.NewLine}{messageText}");

                await Context.Channel.SendMessageAsync("", false, embedBuilder.Build());
            }
            catch (Exception ex)
            {
                await Context.Message.Channel.SendMessageAsync(ex.Message);
            }
        }

        [RequireOwner]
        [Command("today")]
        public async Task TodaysBirthdays()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            try
            {
                DiscordHelper.DiscordUserBirthday(Program.Client, Context.Guild.Id, Context.Message.Channel.Id, false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString());
                _logger.LogError(ex, "Error while DiscordHelper.DiscordUserBirthday");
            }
        }

        [Command("ping")]
        public async Task PingInfo()
        {
            await PingInfo(null, null);
        }

        [Command("ping")]
        public async Task PingInfo(string command)
        {
            await PingInfo(command, null);
        }

        [Command("ping")]
        public async Task PingInfo(ulong? userId)
        {
            await PingInfo(null, userId);
        }

        [Command("ping")]
        public async Task PingInfo(string command = null, ulong? userId = null)
        {
            // TODO allow ping to pass for info
            // TODO Put replies into the ping history table aswell

            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            bool filterPingHell = false;
            bool fullView = false;
            if (!string.IsNullOrWhiteSpace(command))
            {
                if (command.Trim().ToLower() == "-pinghell")
                    filterPingHell = true;
                else if (command.Trim().ToLower() == "full")
                    fullView = true;
            }

            try
            {
                var user = Context.Message.Author as SocketGuildUser;

                // load the user in question
                if (userId.HasValue)
                    user = Program.Client.GetGuild(Program.ApplicationSetting.BaseGuild).GetUser(userId.Value) as SocketGuildUser;

                var pingHistory = DiscordHelper.GetTotalPingHistory(user, 30, filterPingHell);
                var builder = DiscordHelper.GetEmbedForPingHistory(pingHistory, user, fullView ? 30 : 10);

                await Context.Message.Channel.SendMessageAsync("", false, builder.Build());
            }
            catch (Exception ex)
            {
                await Context.Message.Channel.SendMessageAsync(ex.ToString());
            }
        }

        [Command("websocket")]
        public async Task WebsocketInfo()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            string prefix = Program.CurrentPrefix;

            var ws = Program.PlaceServer;

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle($"{Program.Client.CurrentUser.Username} Websocket Stats");

            builder.WithColor(0, 0, 255);

            builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
            builder.WithCurrentTimestamp();
            builder.AddField("Is Accepting", $"{ws.IsListening}", true);
            builder.AddField("Is Secure", $"{ws.IsSecure}", true);
            //builder.AddField("IP Endpoint", $"{ws.Address}", true);
            builder.AddField("Address", $"{ws.Address}", true);
            //builder.AddField("Connected Sessions", $"{ws.ConnectedSessions}", true);
            //builder.AddField("Bytes Pending", $"{ws.BytesPending.ToString("N0")}", true);
            //builder.AddField("Bytes Received", $"{ws.BytesReceived.ToString("N0")}", true);
            //builder.AddField("Bytes Sent", $"{ws.BytesSent.ToString("N0")}", true);

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        // Temp
        [Command("emote"), Priority(1)]
        public async Task EmojiInfo(string search)
        {
            await Context.Channel.SendMessageAsync($"This command has been moved. \"{Program.CurrentPrefix}emote help\" for more info", false);
        }

        // TODO duplicate finder -> fingerprint
        // TODO better selection
        //[Command("emote")]
        //public async Task EmojiInfo(string search, int page = 0, bool debug = false)
        //{


        //    //msg.ModifyAsync(i => i.Attachments.)
        //}

        //[Command("study", RunMode = RunMode.Async)]
        //public async Task Study(ulong confirmId = 0)
        //{
        //    return;
        //    if (confirmId == 0)
        //    {
        //        await Context.Channel.SendMessageAsync($"May contain spoilers to old exams! Once you receive the study role you will be only to chat for max of 15 mins at a time." + Environment.NewLine +
        //            $"If you are in cooldown, the bot will delete all your messages. Every question is designed to be able to solve within 5-10 mins. To recall your message write '.study'" + Environment.NewLine +
        //            $" To be able to chat you will need to solve a question each time. (All subject channels are exempt from this rule.)" + Environment.NewLine +
        //            $"Enter code: .study {Context.Message.Author.Id}", false);
        //    }
        //    else if (confirmId == Context.Message.Author.Id)
        //    {
        //        try
        //        {
        //            var user = Context.User;
        //            var role = Context.Guild.Roles.FirstOrDefault(x => x.Id == 798639212818726952); // study role
        //            await (user as IGuildUser).AddRoleAsync(role);
        //            await Context.Channel.SendMessageAsync("Role assigned. Good luck!");
        //        }
        //        catch (Exception ex)
        //        {

        //        }
        //    }
        //    else
        //    {
        //        await Context.Channel.SendMessageAsync("Wrong code");
        //    }
        //}

        [Command("question", RunMode = RunMode.Async)]
        public void AskQuestion([Remainder] string filter = null)
        {
            return;
            //try
            //{
            //    // TODO disable subjects if the exam is behind

            //    StudyHelper helper = new StudyHelper();

            //    var question = helper.GetRandomLinalgQuestion(filter);
            //    //if (!Program.CurrentActiveQuestion.ContainsKey(Context.Message.Author.Id))
            //    //    Program.CurrentActiveQuestion.Add(Context.Message.Author.Id, question);

            //    PrintQuestion(question);

            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, ex.Message);
            //}

            // TODO place the question into global
        }


        //[Command("repeat", RunMode = RunMode.Async)]
        //public async Task RepeatQuestion()
        //{
        //    return;
        //    // TODO disable subjects if the exam is behind

        //    StudyHelper helper = new StudyHelper();

        //    Question question = null;
        //    if (Program.CurrentActiveQuestion.ContainsKey(Context.Message.Author.Id))
        //        question = Program.CurrentActiveQuestion[Context.Message.Author.Id];

        //    if (question == null)
        //        return;


        //    PrintQuestion(question);

        //    // TODO place the question into global
        //}


        [Command("tex", RunMode = RunMode.Async)]
        public async Task Latex([Remainder] string input)
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            var painter = new MathPainter
            {
                LaTeX = input,
                TextColor = SKColor.Parse("FFFFFF")

            }; // or TextPainter
            painter.FontSize = 20;

            var png = painter.DrawAsStream();


            var msg = await Context.Channel.SendFileAsync(png, $"test.png", "here you go human");

        }

        private async void PrintQuestion(Question question)
        {
            int secWait = 120;

            string obscureAnswer = "";

            Random r = new Random();

            for (int i = 0; i < r.Next(5, 15); i++)
            {
                obscureAnswer += " ";
            }


            string image = "";

            if (!string.IsNullOrWhiteSpace(question.ExamQuestionImage))
            {
                image = $"Image Name: {question.ExamQuestionImage}{Environment.NewLine}";
            }

            //string text = $"({question.Source}) Q: {question.Text}{Environment.NewLine}Expected input format: {question.ExpectedInputFormat}{Environment.NewLine}Hint: ||{question.Hint}||{Environment.NewLine}Question will be gone after {secWait} secs. Screenshot it if you need it";
            string text = $"**({question.Source}) Task: {question.Task} Q: {question.Text}**" +
                $"{Environment.NewLine}{image}Hint: ||{question.Hint}||" +
                $"{Environment.NewLine}" +
                $"{Environment.NewLine}" +
                $"Answer: ||{obscureAnswer}{question.Answer}{obscureAnswer}||" +
                $"{Environment.NewLine}Question will be gone after {secWait} secs. Screenshot it if you need it";

            if (question.Image != null)
            {
                var msg = await Context.Channel.SendFileAsync(question.Image, $"test.png", text);
                await Task.Delay(secWait * 1000);
                await msg.DeleteAsync();
            }
            else
            {
                var msg = await Context.Channel.SendMessageAsync(text);
                await Task.Delay(secWait * 1000);
                await msg.DeleteAsync();
            }
        }

        //[Command("wallpaper", RunMode = RunMode.Async)]
        //[Alias("wp")]
        //public async Task Wallpaper()
        //{
        //    return;
        //    if (AllowedToRun(BotPermissionType.EnableType2Commands))
        //        return;

        //    var author = Context.Message.Author;
        //    LogManager.ProcessMessage(author, BotMessageType.Wallpaper);

        //    var req = NekosFun.GetLink("wallpaper");
        //    BannedLink report = null;

        //    string regenString = "";

        //    do
        //    {
        //        try
        //        {
        //            report = GetReportInfoByImage(req);
        //            if (report != null)
        //            {

        //                var user = DatabaseManager.GetDiscordUserById(report.AddedByDiscordUserId);
        //                regenString += $"An image has been blocked by {user.Nickname}. Regenerating a new image just for you :)" + Environment.NewLine;
        //                req = NekosFun.GetLink("wallpaper");

        //                //return;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            return;
        //        }
        //    } while (report != null);

        //    if (regenString.Length > 0)
        //    {
        //        await Context.Channel.SendMessageAsync(regenString, false);
        //    }

        //    var message = await Context.Channel.SendMessageAsync(req, false);

        //    // disabled for now
        //    if (false)
        //        await AddSaveReact(message);

        //    AddMessageToList(message);

        //    if (new Random().Next(0, 20) == 0)
        //    {
        //        // Send only every x messages
        //        await Context.Channel.SendMessageAsync("wallpaper may still contain some NSFW images. To remove them type '.block link' To get the link, right click the image -> Copy Link. Do not use < > around the link", false);
        //    }
        //}

        /*
        [Command("animalears", RunMode = RunMode.Async)]
        public async Task Animalears()
        {
            return;
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Animalears);

            var req = NekosFun.GetLink("animalears");
            var report = GetReportInfoByImage(req);
            if (report != null)
            {
                var user = DatabaseManager.GetDiscordUserById(report.AddedByDiscordUserId);
                Context.Channel.SendMessageAsync($"The current image has been blocked by {user.Nickname}. Try the command again to get a new image", false);
                return;
            }

            var message = await Context.Channel.SendMessageAsync(req, false);
            await AddSaveReact(message);
            AddMessageToList(message);
        }


        [Command("foxgirl", RunMode = RunMode.Async)]
        public async Task Foxgirl()
        {
            return;
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Foxgirl);

            var req = NekosFun.GetLink("foxgirl");
            var report = GetReportInfoByImage(req);
            if (report != null)
            {
                var user = DatabaseManager.GetDiscordUserById(report.AddedByDiscordUserId);
                Context.Channel.SendMessageAsync($"The current image has been blocked by {user.Nickname}. Try the command again to get a new image", false);
                return;
            }

            var message = await Context.Channel.SendMessageAsync(req, false);
            await AddSaveReact(message);
            AddMessageToList(message);
        }
        */

        public void AddMessageToList(RestUserMessage message)
        {
            if (!message.Author.IsBot)
            {
                // for now only log messages from bots
                return;
            }

            LastMessages.Add(message);
            if (LastMessages.Count() > 100)
                LastMessages.RemoveAt(0);
        }


        [Command("block")]
        public async Task Block(string image)
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.ApplicationSetting.Owner)
            {
                //Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                //return
            }
            var guildUser = author as SocketGuildUser;

            // Remove < > for no preview if used
            image = image.Replace("<", "").Replace("<", "");



            if (image.Contains("discordapp") || !image.StartsWith("https://"))
            {
                await Context.Channel.SendMessageAsync($"You did not provide a valid link.", false);
                return;
            }

            var blockInfo = DatabaseManager.GetBannedLink(image);

            if (blockInfo != null)
            {
                await Context.Message.DeleteAsync();
                var user = DatabaseManager.GetDiscordUserById(blockInfo.AddedByDiscordUserId);
                await Context.Channel.SendMessageAsync($"Image is already in the blacklist (blocked by {user.Nickname}) You were too slow {guildUser.Nickname} <:exmatrikulator:769624058005553152>", false);
                return;
            }

            /* ReportInfo reportInfo = new ReportInfo()
             {
                 ImageUrl = image,
                 ReportedAt = DateTime.Now,
                 ReportedBy = new Stats.DiscordUser()
                 {

                     DiscordId = guildUser.Id,
                     DiscordDiscriminator = guildUser.DiscriminatorValue,
                     DiscordName = guildUser.Username,
                     ServerUserName = guildUser.Nickname ?? guildUser.Username // User Nickname -> Update
                 }

             };
            */
            DatabaseManager.CreateBannedLink(image, guildUser.Id);

            /*
            Program.BlackList.Add(reportInfo);
            Program.SaveBlacklist();*/

            foreach (var message in LastMessages)
            {
                if (message.Content == image)
                {
                    // We are removing this item
                    await message.DeleteAsync();
                }
            }

            await Context.Channel.SendMessageAsync($"Added the image to blacklist by {guildUser.Nickname}", false);
            await Context.Message.DeleteAsync();
        }

        private async Task AddSaveReact(RestUserMessage message)
        {
            await message.AddReactionAsync(Emote.Parse("<:savethis:780179874656419880>"));
        }


        [Command("rant")]
        public async Task Rant(string type = null, [Remainder] string content = "")
        {
            // TODO perm check but for now open everywhere
            //Context.Channel.SendMessageAsync("Ask <@675445762900885515> or <@276462585690193921> or <@124603627833786370> why its disabled. Also ill fix it in the evening.");



            if (type == null)
            {
                // get a random rant
                RandomRant();

            }
            else if (type.ToLower() == "help")
            {
                await HelpOutput();
                return;
            }
            else if (type.ToLower() == "types")
            {
                var typeList = DatabaseManager.GetAllRantTypes();
                string allTypes = "```" + string.Join(", ", typeList.Values) + "```";

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("All Rant types");

                builder.WithColor(0, 0, 255);

                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();
                builder.AddField("Types [Name]", allTypes);

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            else
            {
                int typeId = DatabaseManager.GetRantType(type);
                if (content.Length == 0)
                {
                    // requested a rant from that category
                    RandomRant(type);
                    return;
                }
                else if (content.Length < 5)
                {
                    await Context.Channel.SendMessageAsync($"Rant needs to be atleast 5 characters long", false);
                    return;
                }

                if (typeId < 0)
                {
                    await Context.Channel.SendMessageAsync($"You used a type that doesnt exist yet. But because I'm so nice im adding it for you.", false);
                    bool success = DatabaseManager.Instance().AddRantType(type);
                    await Context.Channel.SendMessageAsync($"Added {type} Success: {success}", false);

                    if (!success)
                        return;

                    typeId = DatabaseManager.GetRantType(type);
                }

                var guildChannel = (SocketGuildChannel)Context.Message.Channel;

                bool successRant = DatabaseManager.AddRant(Context.Message.Id, Context.Message.Author.Id, guildChannel.Id, typeId, content);
                await Context.Channel.SendMessageAsync($"Added rant Success: {successRant}", false);
            }
        }

        private async void RandomRant(string type = null)
        {
            var rant = DatabaseManager.GetRandomRant(type);
            if (rant == null)
            {
                await Context.Channel.SendMessageAsync($"No rant could be loaded"); //for type {type} (To see all types write: '.rant types')." +
                                                                                    // $"If you are trying to add a rant type '.rant {type} <your actual rant>'", false);
                return;
            }

            var byUser = Program.Client.GetUser(rant.DiscordUserId);
            var datePosted = SnowflakeUtils.FromSnowflake(rant.DiscordMessageId);
            var rantType = DatabaseManager.GetRantTypeNameById(rant.RantTypeId);

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle($"Rant about {rantType} on {datePosted:dd.MM.yyyy}");
            builder.Description = rant.Content;
            builder.WithColor(255, 0, 255);

            // Can cause NRE
            if (byUser != null)
                builder.WithAuthor(byUser);

            builder.WithCurrentTimestamp();
            builder.WithFooter($"RantId: {rant.RantMessageId} TypeId: {rant.RantTypeId}");

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        /*
        [Command("graph")]
        public async Task Graph()
        {



        }*/


        [Command("old_stats")]
        public async Task Stats()
        {
            try
            {
                if (AllowedToRun(BotPermissionType.EnableType2Commands))
                    return;
                var author = Context.Message.Author;
                LogManager.ProcessMessage(author, BotMessageType.Other);


                Dictionary<string, CommandStatistic> dbStats = new Dictionary<string, CommandStatistic>();




                // TODO clean up this mess
                /*
                var topCommands = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalCommands).Take(5);
                var topNeko = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalNeko).Take(5);
                var topNekoGif = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalNekoGif).Take(5);
                var topHolo = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalHolo).Take(5);
                var topWaifu = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalWaifu).Take(5);
                var topBaka = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalBaka).Take(5);
                var topSmug = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalSmug).Take(5);
                var topFox = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalFox).Take(5);

                var topAvatar = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalAvatar).Take(5);
                var topNekopAvatar = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalNekoAvatar).Take(5);
                var topWallpaper = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalWallpaper).Take(5);

                var topFoxgirl = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalFoxgirl).Take(5);
                var topAnimalears = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalAnimalears).Take(5);

                var topSearch = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalSearch).Take(5);
                */

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("BattleRush's Helper Stats");
                //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");

                builder.WithColor(0, 100, 175);

                // Profile image of top person -> to update
                //builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");

                builder.WithCurrentTimestamp();


                foreach (BotMessageType type in Enum.GetValues(typeof(BotMessageType)))
                {
                    var userStats = DatabaseManager.Instance().GetTopStatisticByType(type);

                    builder.AddField(type.ToString(), GetStatsRankingString(userStats), true); // TODO take top5 and their count
                }

                /*
                builder.AddField("Total Commands", GetRankingString(topCommands.Select(i => i.ServerUserName + ": " + i.Stats.TotalCommands)));
                builder.AddField("Total Search", GetRankingString(topSearch.Select(i => i.ServerUserName + ": " + i.Stats.TotalSearch)), true);
                builder.AddField("Total Neko", GetRankingString(topNeko.Select(i => i.ServerUserName + ": " + i.Stats.TotalNeko)), true);
                builder.AddField("Total Neko gifs", GetRankingString(topNekoGif.Select(i => i.ServerUserName + ": " + i.Stats.TotalNekoGif)), true);
                builder.AddField("Total Holo", GetRankingString(topHolo.Select(i => i.ServerUserName + ": " + i.Stats.TotalHolo)), true);
                builder.AddField("Total Waifu", GetRankingString(topWaifu.Select(i => i.ServerUserName + ": " + i.Stats.TotalWaifu)), true);
                builder.AddField("Total Baka", GetRankingString(topBaka.Select(i => i.ServerUserName + ": " + i.Stats.TotalBaka)), true);
                builder.AddField("Total Smug", GetRankingString(topSmug.Select(i => i.ServerUserName + ": " + i.Stats.TotalSmug)), true);
                builder.AddField("Total Fox", GetRankingString(topFox.Select(i => i.ServerUserName + ": " + i.Stats.TotalFox)), true);

                builder.AddField("Total Avatar", GetRankingString(topAvatar.Select(i => i.ServerUserName + ": " + i.Stats.TotalAvatar)), true);
                builder.AddField("Total Neko Avatar", GetRankingString(topNekopAvatar.Select(i => i.ServerUserName + ": " + i.Stats.TotalNekoAvatar)), true);
                builder.AddField("Total Wallpaper", GetRankingString(topWallpaper.Select(i => i.ServerUserName + ": " + i.Stats.TotalWallpaper)), true);

                builder.AddField("Total Foxgirl", GetRankingString(topFoxgirl.Select(i => i.ServerUserName + ": " + i.Stats.TotalFoxgirl)), true);
                builder.AddField("Total Animalears", GetRankingString(topAnimalears.Select(i => i.ServerUserName + ": " + i.Stats.TotalAnimalears)), true);
                */
                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            catch (Exception ex)
            {

            }
        }

        private static string GetStatsRankingString(IEnumerable<CommandStatistic> commandStats)
        {
            string rankingString = "";
            int pos = 1;
            foreach (var commandStat in commandStats)
            {
                string boldText = pos == 1 ? " ** " : "";
                rankingString += $"{boldText}{pos}) <@!{commandStat.DiscordUserId}> {commandStat.Count}{boldText}{Environment.NewLine}";
                pos++;
            }
            return rankingString.Length > 0 ? rankingString : "n/a";
        }

        [Command("say")]
        public async Task Say(string message, int amount)
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                //Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            if (amount < 1)
                amount = 1;

            await Context.Message.DeleteAsync();

            for (int i = 0; i < amount; i++)
            {
                await Context.Channel.SendMessageAsync(message, false);
                await Task.Delay(1250);
            }
        }

        [Command("purge")]
        public async Task Purge(int count, bool fromBot = false)
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                //Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            ulong fromUserToDelete = fromBot ? 774276700557148170 : Program.ApplicationSetting.Owner;

            if (fromBot)
            {
                await Context.Message.DeleteAsync();
            }

            var messages = await Context.Channel.GetMessagesAsync(100).FlattenAsync(); //default is 100
            messages = messages.Where(i => i.Author.Id == fromUserToDelete).OrderByDescending(i => i.Id).Take(count);
            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messages);
        }

        [Command("nuke")]
        public async Task Nuke(int count, bool tts = false)
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                return;
            }

            if (count < 0)
                return;


            if (count > 1000)
            {
                List<IMessage> nukeMessages = new List<IMessage>() { Context.Message };

                nukeMessages.Add(await Context.Channel.SendMessageAsync($"Setting up a hydrogen bomb...", tts));
                await Task.Delay(5000);
                nukeMessages.Add(await Context.Channel.SendMessageAsync($"Designated H2-KM-{count}.", tts));
                await Task.Delay(5000);
                nukeMessages.Add(await Context.Channel.SendMessageAsync($"Arming...", tts));
                await Task.Delay(5000);
                nukeMessages.Add(await Context.Channel.SendMessageAsync($"5"));
                await Task.Delay(1000);
                nukeMessages.Add(await Context.Channel.SendMessageAsync($"4"));
                await Task.Delay(1000);
                nukeMessages.Add(await Context.Channel.SendMessageAsync($"3"));
                await Task.Delay(1000);
                nukeMessages.Add(await Context.Channel.SendMessageAsync($"2"));
                await Task.Delay(1000);
                nukeMessages.Add(await Context.Channel.SendMessageAsync($"1"));
                await Task.Delay(1000);
                nukeMessages.Add(await Context.Channel.SendMessageAsync($"H2-KM-{count}. Has been armed. Stand by."));
                await Task.Delay(4000);
                nukeMessages.Add(await Context.Channel.SendMessageAsync($"Detonation in 10 seconds", tts));
                await Task.Delay(2000);

                var messageCountDownHydrogen = await Context.Channel.SendMessageAsync("https://media4.giphy.com/media/tBvPFCFQHSpEI/200.gif");
                await Task.Delay(9250);

                await messageCountDownHydrogen.DeleteAsync();
                var nukeImage = await Context.Channel.SendMessageAsync("https://thumbs.gfycat.com/DeepNegligibleCarpenterant-size_restricted.gif");
                await Task.Delay(7500);
                await nukeImage.DeleteAsync();

                nukeMessages.Add(await Context.Channel.SendMessageAsync($"For now it was a joke, but next time it might not be...", tts));
                await Task.Delay(4000);

                await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(nukeMessages);

                return;
            }


            var messages = Context.Channel.GetMessagesAsync(count).FlattenAsync(); //default is 100

            var messageCountDown = await Context.Channel.SendMessageAsync("https://media4.giphy.com/media/tBvPFCFQHSpEI/200.gif");
            await Context.Channel.SendMessageAsync($"Placing a tactical nuke KMN-{count}. Scheduled to detonate in 10 seconds.");

            await Task.Delay(9250);

            await messageCountDown.DeleteAsync();


            var loadedMessages = await messages;

            var nuke = await Context.Channel.SendMessageAsync($"https://i.pinimg.com/originals/6c/48/5e/6c485efad8b910e5289fc7968ea1d22f.gif");
            var nukeMsg = await Context.Channel.SendMessageAsync($"kaboom", tts);
            await Task.Delay(1000);
            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(loadedMessages);

            await Task.Delay(5000);
            await nuke.DeleteAsync();
            await nukeMsg.DeleteAsync();
        }





        public async Task SubmitImage()
        {

        }

        [RequireOwner]
        [Command("serverowner")]
        public async Task Lukas()
        {
            await Context.Channel.SendMessageAsync("<@223932775474921472>");
            await Context.Channel.SendMessageAsync("https://media.discordapp.net/attachments/768600365602963496/958082710100901988/ezgif.com-gif-maker.gif");
        }


        [Command("ocr")]
        public async Task OcrImage(ulong messageId)
        {
            try
            {
                // find image
                FileDBManager fileDBManager = FileDBManager.Instance();
                var discordFiles = fileDBManager.GetDiscordFile(messageId);

                if (discordFiles == null || discordFiles.Count == 0)
                {
                    await Context.Channel.SendMessageAsync("No image found with that message id", false);
                    return;
                }

                // if any file hasnt been processed yet
                var notProcessed = discordFiles.All(i => i.OcrDone && i.IsImage && i.Extension != "gif");
                if (!notProcessed)
                {
                    await Context.Channel.SendMessageAsync("Some images have not been processed yet please wait up to a minute", false);
                    return;
                }


                List<(Stream, string)> streams = new List<(Stream, string)>();
                foreach (var file in discordFiles)
                {
                    if (file.IsImage && file.Extension != "gif") // TODO add gif support
                    {
                        // skia bitmap
                        var filePath = file.FullPath;
                        var bitmap = SKBitmap.Decode(filePath);
                        var canvas = new SKCanvas(bitmap);

                        var ocrBoxes = fileDBManager.GetOcrBoxesByFileId(file.DiscordFileId);

                        if (ocrBoxes == null || ocrBoxes.Count == 0)
                        {
                            await Context.Channel.SendMessageAsync($"No text found in the image {file.FileName}", false);
                            continue;
                        }

                        var text = file.OcrText;
                        foreach (var box in ocrBoxes)
                        {
                            // draw lines around the text
                            var paint = new SKPaint
                            {
                                Style = SKPaintStyle.Stroke,
                                Color = SKColors.Red,
                                StrokeWidth = 2
                            };

                            var topLeft = new SKPoint(box.TopLeftX, box.TopLeftY);
                            var topRight = new SKPoint(box.TopRightX, box.TopRightY);
                            var bottomRight = new SKPoint(box.BottomRightX, box.BottomRightY);
                            var bottomLeft = new SKPoint(box.BottomLeftX, box.BottomLeftY);

                            // draw the lines around the text
                            canvas.DrawLine(topLeft, topRight, paint);
                            canvas.DrawLine(topRight, bottomRight, paint);
                            canvas.DrawLine(bottomRight, bottomLeft, paint);
                            canvas.DrawLine(bottomLeft, topLeft, paint);

                            // above the rect draw the text
                            var textPaint = new SKPaint
                            {
                                Style = SKPaintStyle.Fill,
                                Color = SKColors.Red,
                                TextSize = 20
                            };

                            canvas.DrawText(box.Text, topLeft, textPaint);

                            // and higher draw probability
                            var probPaint = new SKPaint
                            {
                                Style = SKPaintStyle.Fill,
                                Color = SKColors.Red,
                                TextSize = 15
                            };

                            canvas.DrawText(box.Probability.ToString("N2"), new SKPoint(topLeft.X, topLeft.Y - 20), probPaint);
                        }

                        // save the image
                        streams.Add((CommonHelper.GetStream(bitmap), text));
                    }
                }

                if (streams.Count == 0)
                {
                    await Context.Channel.SendMessageAsync("No image found with that message id", false);
                    return;
                }

                // send the images
                foreach (var stream in streams)
                {
                    // Discord escape pings
                    string ocrText = stream.Item2.Replace("`", "");

                    // TODO for longer texts split up over multiple messages
                    if (ocrText.Length > 1950)
                    {
                        ocrText = ocrText.Substring(0, 1950);
                    }
                    
                    await Context.Channel.SendFileAsync(stream.Item1, "ocr.png", "```" + stream.Item2 + "```", false);
                }

            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString(), false);
            }
        }

        // ocr with direct upload
        [Command("ocr")]
        public async Task OcrImage()
        {
            try
            {
                var attachments = Context.Message.Attachments;
                if (attachments.Count == 0)
                {
                    await Context.Channel.SendMessageAsync("No image found with that message id", false);
                    return;
                }

                await Context.Channel.SendMessageAsync("Processing the image... This may take up to a minute", false);

                int count = 0;
                var fileDBManager = FileDBManager.Instance();
                while (count <= 100)
                {
                    count++;
                    await Task.Delay(1000);

                    var files = fileDBManager.GetDiscordFile(Context.Message.Id);

                    if (files != null && files.Count > 0)
                    {
                        var processed = files.All(i => i.OcrDone && i.IsImage && i.Extension != "gif");
                        if (processed)
                        {
                            await Context.Channel.SendMessageAsync("Processing done. Retreiving results", false);
                            await OcrImage(Context.Message.Id);
                            break;
                        }
                    }
                }

                if (count > 100)
                {
                    await Context.Channel.SendMessageAsync("Processing took too long. Please try again later. The file will still be processed. Check with .ocr " + Context.Message.Id, false);
                }
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString(), false);
            }
        }

        //[Command("countdown2021")]
        //public async Task countdown2021()
        //{
        //    return;
        //    var author = Context.Message.Author;
        //    if (author.Id != ETHDINFKBot.Program.Owner)
        //    {
        //        Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
        //        return;
        //    }

        //    Task t = new Task(() => CountdownLoop(Context));
        //    t.Start();

        //}

        private DateTime Now()
        {
            return DateTime.UtcNow.AddHours(1);
        }

        private long MilisecsToMidnight()
        {
            return (int)(new DateTime(2021, 1, 1, 0, 0, 0) - Now()).TotalMilliseconds;
        }

        private async void CountdownLoop(SocketCommandContext context)
        {
            while (Now().Year == 2020)
            {
                var ms = MilisecsToMidnight();
                if (ms < 0)
                    break;
                if (ms < 10000)
                {
                    long secs = (ms / 1000) + 1;
                    string addText = "";
                    if (secs == 10)
                        addText = "only 10 secs can you believe it";
                    else if (secs == 9)
                        addText = "";
                    else if (secs == 8)
                        addText = "uhm my time is running out";
                    else if (secs == 7)
                        addText = "";
                    else if (secs == 6)
                        addText = "uhm";
                    else if (secs == 5)
                        addText = "what do I say";
                    else if (secs == 4)
                        addText = "";
                    else if (secs == 3)
                        addText = "I could say something inspirational";
                    else if (secs == 2)
                        addText = "";
                    else if (secs == 1)
                        addText = "guess what happens next?";

                    await context.Channel.SendMessageAsync($"{secs}... {addText}");

                    Thread.Sleep((int)(ms % 1000));
                }
                else if (ms < 60000)
                {
                    await context.Channel.SendMessageAsync($"{(ms / 1000) + 1}...");
                    Thread.Sleep((int)(ms % 5000));
                }
                else
                {
                    await context.Channel.SendMessageAsync($"{(ms / 60000) + 1} min left...");
                    Thread.Sleep((int)(ms % 60000));
                }
            }

            await context.Channel.SendMessageAsync($"Is it 2021?");
            Thread.Sleep(2);
            await context.Channel.SendMessageAsync($"Checks the clock");
            Thread.Sleep(2);
            await context.Channel.SendMessageAsync($"I guess it is. **Happy 2021**");
            Thread.Sleep(5);
            await context.Channel.SendMessageAsync($"Also **WE ARE NOW 1 YEAR CLOSER TO THE BASISPRÜFUNG EXAMS**");
        }




        //[Command("test")]
        //public async Task Test()
        //{
        //    return;
        //    if (AllowedToRun(BotPermissionType.EnableType2Commands))
        //        return;
        //    EmbedBuilder builder = new EmbedBuilder();

        //    builder.WithTitle("BattleRush's Helper Stats");
        //    //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");

        //    builder.WithColor(0, 100, 175);

        //    // Profile image of top person -> to update
        //    //builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");

        //    builder.WithCurrentTimestamp();
        //    builder.AddField("Top Emoji Usage", $"<:checkmark:778202017372831764>");
        //    builder.AddField("<:checkmark:778202017372831764>", $"test");
        //    await Context.Channel.SendMessageAsync("", false, builder.Build());
        //}


        // Allow only once per hour to call
        private static DateTime lastCall = DateTime.MinValue;
        [Command("count")]
        public async Task CheckCount()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            if (lastCall.AddHours(1) > DateTime.Now && Context.Message.Author.Id != ETHDINFKBot.Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync("You can only call this command once per hour");
                return;
            }

            lastCall = DateTime.Now;

            string query = "CALL CheckLongestCountingChain";

            var commandResponse = await SQLHelper.SqlCommand(Context, query, true);
            await Context.Channel.SendMessageAsync(commandResponse, false);
        }

        [Command("r")]
        public async Task Reddit(string subreddit = "")
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            if (subreddit.Contains("'") || subreddit.Contains("\""))
                return;

            if (CommonHelper.ContainsForbiddenQuery(subreddit))
                return;

            LogManager.ProcessMessage(Context.Message.Author, BotMessageType.Reddit);

            if (subreddit.ToLower() == "all")
            {
                string allSubreddits = "**Available subreddits**" + Environment.NewLine;
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var subredditInfos = context.SubredditInfos.AsQueryable().OrderBy(i => i.SubredditName).ToList();

                    foreach (var item in subredditInfos)
                    {
                        allSubreddits += $"{item.SubredditName}, ";
                    }

                    // TODO better text
                    await Context.Channel.SendMessageAsync(allSubreddits, false);
                }
            }
            else
            {
                // TODO text posts

                string link = "";
                try
                {
                    // TODO Better escaping
                    subreddit = subreddit.Replace("'", "''");
                    using (ETHBotDBContext context = new ETHBotDBContext())
                    {
                        using (var command = context.Database.GetDbConnection().CreateCommand())
                        {
                            // TODO sql input escaping
                            command.CommandText = @$"select ri.Link from SubredditInfos si
left join RedditPosts pp on si.SubredditId = pp.SubredditInfoId
left join RedditImages ri on pp.RedditPostId = ri.RedditPostId
where si.SubredditName like '%{subreddit}%' and ri.Link is not null and pp.IsNSFW = 0
ORDER BY RAND() LIMIT 1";// todo nsfw test
                            context.Database.OpenConnection();
                            using (var result = command.ExecuteReader())
                            {
                                while (result.Read())
                                {
                                    link = result.GetString(0);
                                    break;
                                }
                            }
                        }
                    }

                }
                catch (Exception ex)
                {

                }

                await Context.Channel.SendMessageAsync(link, false);

            }
            /*
        * 
        * SELECT column FROM table 
        ORDER BY RANDOM() LIMIT 1

        */
        }

        [Command("space")]
        public async Task SpaceCommand(int amount = 1)
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            if (amount < 1)
                amount = 1;
            if (amount > 2)
                amount = 2;

            List<string> oneTimeEmotes = new List<string>() { "rocket", "ringed_planet", "boom", "comet", "sparkles", "full_moon", "earth_africa", "dizzy", "star", "star2" };
            try
            {

                for (int i = 0; i < amount; i++)
                {
                    string stars = "...ﾟﾟ✦";

                    Random r = new Random();

                    string message = "";

                    while (message.Length < 2000)
                    {

                        int randomSpace = r.Next(20);

                        int randomDot = r.Next(4);
                        int randomEmote = r.Next(50);

                        // add up to 4 normal spaces to introduce "randomness"
                        message += new String(' ', r.Next(5)) + new String('　', randomSpace);



                        if (randomEmote == 0 && message.Length < 1980 /*to ensure enough space*/)
                        {
                            // reset them if empty
                            if (oneTimeEmotes.Count == 0)
                                oneTimeEmotes = new List<string>() { "rocket", "ringed_planet", "boom", "comet", "sparkles", "full_moon", "earth_africa", "dizzy", "star", "star2" };

                            int randomEmoteIndex = r.Next(oneTimeEmotes.Count);
                            string randomEmoteString = oneTimeEmotes.ElementAt(randomEmoteIndex);
                            message += $":{randomEmoteString}:";

                            oneTimeEmotes.RemoveAt(randomEmoteIndex);
                            continue; // do not add a dot
                        }

                        if (randomDot == 0)
                        {
                            int randStar = r.Next(stars.Length);
                            message += stars.ElementAt(randStar);
                        }
                    }

                    if (message.Length > 2000)
                    {
                        message = message.Substring(0, 2000);
                    }

                    await Context.Channel.SendMessageAsync(message);
                }
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString());
            }

            await Context.Message.DeleteAsync();
        }

        //[Command("disk")]
        //public void DirSizeReddit()
        //{
        //    return; // disable
        //    try
        //    {
        //        DirectoryInfo info = new DirectoryInfo("Reddit");
        //        long size = DirSize(info);

        //        Context.Channel.SendMessageAsync($"Current Reddit disk usage :{size / (decimal)1024 / 1024 / 1024} GB", false);
        //    }
        //    catch (Exception ex)
        //    {
        //        Context.Channel.SendMessageAsync(ex.ToString(), false);
        //    }
        //}

        public static long DirSize(DirectoryInfo d)
        {
            long size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += DirSize(di);
            }
            return size;
        }

        [Command("rp")]
        public async Task RedditPost(string subreddit = "")
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            if (subreddit.Contains("'") || subreddit.Contains("\""))
                return;

            if (CommonHelper.ContainsForbiddenQuery(subreddit))
                return;

            LogManager.ProcessMessage(Context.Message.Author, BotMessageType.Reddit);

            if (subreddit.ToLower() == "all")
            {
                string allSubreddits = "**Available subreddits**" + Environment.NewLine;
                await Context.Channel.SendMessageAsync(allSubreddits, false);
                allSubreddits = "";

                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var subredditInfos = context.SubredditInfos.AsQueryable().OrderBy(i => i.SubredditName).ToList();

                    foreach (var item in subredditInfos)
                    {
                        allSubreddits += $"{item.SubredditName}, ";

                        if (allSubreddits.Length > 1900)
                        {
                            // TODO better text
                            await Context.Channel.SendMessageAsync(allSubreddits, false);
                            allSubreddits = "";
                        }
                    }


                }
            }
            else
            {

                int postId = 0;
                try
                {
                    // TODO Better escaping
                    subreddit = subreddit.Replace("'", "''");
                    using (ETHBotDBContext context = new ETHBotDBContext())
                    {
                        using (var command = context.Database.GetDbConnection().CreateCommand())
                        {
                            // TODO sql input escaping
                            command.CommandText = @$"select pp.RedditPostId from SubredditInfos si
left join RedditPosts pp on si.SubredditId = pp.SubredditInfoId
where si.SubredditName like '%{subreddit}%' and pp.IsNSFW = 0
ORDER BY RAND() LIMIT 1";// todo nsfw test
                            context.Database.OpenConnection();
                            using (var result = command.ExecuteReader())
                            {
                                while (result.Read())
                                {
                                    postId = result.GetInt32(0);
                                    break;
                                }
                            }
                        }

                        // TODO handle video as non embed until discord fixes it

                        var redditPost = DatabaseManager.GetRedditPostById(postId);

                        var subredditInfo = DatabaseManager.GetSubreddit(redditPost.SubredditInfoId);

                        EmbedBuilder builder = new();

                        builder.WithTitle(redditPost.PostTitle);
                        builder.WithUrl("https://www.reddit.com/" + redditPost.Permalink);

                        var content = redditPost.IsText ? redditPost.Content : "";

                        if (content.Length > 2000)
                        {
                            content = content.Substring(0, 2000);
                        }

                        // TODO if subreddit name null get the subreddit 
                        builder.WithDescription(content);
                        builder.WithColor(0, 0, 255);

                        //builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");
                        builder.WithCurrentTimestamp();
                        string url = redditPost.Url;
                        if (url.Contains("v.redd.it"))
                        {
                            url += "/DASH_720.mp4";
                        }

                        builder.WithImageUrl(url);
                        builder.AddField("Infos", $"Posted by: {redditPost.Author} in /r/{subredditInfo?.SubredditName} at {redditPost.PostedAt}");
                        builder.AddField("Upvotes", redditPost.UpvoteCount, true);
                        builder.AddField("Downvotes", redditPost.DownvoteCount, true);
                        builder.AddField("NSFW", redditPost.IsNSFW, true);

                        await Context.Channel.SendMessageAsync("", false, builder.Build());

                    }

                }
                catch (Exception ex)
                {

                }
            }
            /*
        * 
        * SELECT column FROM table 
        ORDER BY RANDOM() LIMIT 1

        */
        }

        /*
        [Command("radmin")]
        public async Task RedditAdmin(string command = "", string value = "")
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                Context.Channel.SendMessageAsync("You aren't allowed to run this command ask BattleRush to run it for you", false);
                return;
            }

            if (command == "")
                command = "help";

            switch (command)
            {
                case "help":
                    Context.Channel.SendMessageAsync("help, status, add NAME, start NAME", false);
                    break;

                case "status":
                    CheckReddit();
                    break;

                case "add":
                    AddSubreddit(value);
                    break;

                case "start":

                    break;
                default:
                    break;
            }

        }
        */

        // todo dupe code

        /*[Command("drawtest")]
        public async Task drawtest()
        {
            return;
            try
            {
                DateTime from = new DateTime(2021, 03, 08);
                DateTime to = new DateTime(2021, 03, 09);

                using (var stream = StatsHelper.GetMessageGraph(from, to))
                    await Context.Channel.SendFileAsync(stream, "graph.png");
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString().Substring(0, Math.Min(ex.ToString().Length, 1990)));
            }

        }*/


        [Command("testpiechart")]
        public void testpiechart()
        {
            return;

            // SYSTEM.DRAWING
            /*
            try
            {
                List<string> labels = new List<string>()
                {
                    "one", "two", "three", "four", "five", "six", "seven", "eight",
                };

                List<int> data = new List<int>()
                {
                    5,84,63,45,127,12,55,95
                };

                using (var stream = new PieChart2D(labels, data).DrawChart())
                    await Context.Channel.SendFileAsync(stream, "graph.png");
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString().Substring(0, Math.Min(ex.ToString().Length, 1990)));
            }*/
        }

        [Command("messagegraph")]
        public void MessageGraph(string param = null)
        {
            // STYTEM.DRAWING
            /*
            try
            {
                List<DateTimeOffset> messageTimes = new List<DateTimeOffset>();
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    if (param == null)
                        messageTimes = context.DiscordMessages.AsQueryable().Where(i => i.DiscordUserId == Context.Message.Author.Id).Select(i => SnowflakeUtils.FromSnowflake(i.DiscordMessageId)).ToList();
                    else if (param == "all")
                        messageTimes = context.DiscordMessages.AsQueryable().Select(i => SnowflakeUtils.FromSnowflake(i.DiscordMessageId)).ToList();
                    else if (param == "lernphase")
                        messageTimes = context.DiscordMessages.AsQueryable().Select(i => SnowflakeUtils.FromSnowflake(i.DiscordMessageId)).ToList().Where(i => new DateTime(2020, 12, 18) < i && new DateTime(2021, 1, 25) > i).ToList();
                    else if (param == "bp")
                        messageTimes = context.DiscordMessages.AsQueryable().Select(i => SnowflakeUtils.FromSnowflake(i.DiscordMessageId)).ToList().Where(i => new DateTime(2021, 1, 25) < i && new DateTime(2021, 2, 9) > i).ToList();
                }

                int bound = 10;

                var groups = messageTimes.GroupBy(x =>
                {
                    var stamp = x;
                    stamp = stamp.AddMinutes(-(stamp.Minute % bound));
                    stamp = stamp.AddMilliseconds(-stamp.Millisecond - 1000 * stamp.Second);
                    return stamp;
                }).Select(g => new { TimeStamp = g.Key, Value = g.Count() }).ToList();

                string reply = "";

                foreach (var group in groups)
                {
                    reply += $"{group.TimeStamp}: {group.Value}" + Environment.NewLine;

                    if (reply.Length > 1925)
                        break;
                }


                Brush b = new SolidBrush(Color.White);
                Pen p = new Pen(b);

                Brush b2 = new SolidBrush(Color.Red);

                if (param != null)
                {
                    b2 = new SolidBrush(Color.Blue);
                }

                Pen pen2 = new Pen(Color.FromArgb(172, 224, 128, 0), 15);

                pen2.Width = 4;

                Font drawFont2 = new Font("Arial", 16);
                SolidBrush drawBrush = new SolidBrush(Color.Black);



                // todo make dynamic 

                Bitmap Bitmap;
                Graphics Graphics;
                List<DBTableInfo> DBTableInfo;
                int width = 1920;
                int height = 1080;

                Bitmap = new Bitmap(width, height); // TODO insert into constructor
                Graphics = Graphics.FromImage(Bitmap);
                Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Graphics.Clear(Color.FromArgb(54, 57, 63));


                var dayNames = CultureInfo.CurrentCulture.DateTimeFormat.DayNames.ToList();
                dayNames.Add(DateTimeFormatInfo.CurrentInfo.GetDayName(DayOfWeek.Sunday));
                dayNames.RemoveAt(0);

                width -= 120;


                int xMin = 100;
                int xMax = 1900;
                int xSize = xMax - xMin;

                int yMin = 1020;
                int yMax = 40;
                int ySize = yMin - yMax;

                Graphics.DrawString($"Graph for param: {param ?? Context.Message.Author.Username}", drawFont2, b, new Point(10, 5));

                for (int i = 0; i < dayNames.Count; i++)
                {
                    Graphics.DrawString($"{dayNames[i]}", drawFont2, b, new Point(xMin + 100 + i * (width / dayNames.Count), height - 35));
                    Graphics.DrawLine(p, new Point(xMin + i * (width / dayNames.Count), 20), new Point(xMin + i * (width / dayNames.Count), ySize + 20));

                }

                decimal maxValue = 1;

                List<int> vals = new List<int>();
                foreach (var item in groups)
                {
                    vals.Add(item.Value);
                }

                vals.Sort();

                int outlierIgnore = 10;

                if (param != null)
                    outlierIgnore = 25;

                if (param == "bp")
                    outlierIgnore = 0;

                // cap top 10 outliers
                maxValue = vals.ElementAt(vals.Count - outlierIgnore - 1);

                int yNum = 10;

                for (int i = 0; i <= yNum; i++)
                {
                    Graphics.DrawString($"{(int)((maxValue / yNum) * i)}", drawFont2, b, new Point(40, 10 + ySize - (ySize / yNum) * i));
                    Graphics.DrawLine(p, new Point(xMin, 20 + ySize - (ySize / yNum) * i), new Point(xMax, 20 + ySize - (ySize / yNum) * i));
                }


                int daySize = xSize / 7;
                int maxMinsPerDay = 24 * 60;

                foreach (var item in groups)
                {
                    DateTimeOffset time = item.TimeStamp;

                    int xMult = 0;

                    switch (time.DayOfWeek)
                    {
                        case DayOfWeek.Sunday:
                            xMult = 6;
                            break;
                        case DayOfWeek.Monday:
                            xMult = 0;
                            break;
                        case DayOfWeek.Tuesday:
                            xMult = 1;
                            break;
                        case DayOfWeek.Wednesday:
                            xMult = 2;
                            break;
                        case DayOfWeek.Thursday:
                            xMult = 3;
                            break;
                        case DayOfWeek.Friday:
                            xMult = 4;
                            break;
                        case DayOfWeek.Saturday:
                            xMult = 5;
                            break;
                        default:
                            break;
                    }

                    int dayprefix = xMult * daySize;

                    decimal totalMinsFromLastDay = time.Hour * 60 + time.Minute;

                    if (totalMinsFromLastDay == 0)
                        totalMinsFromLastDay++;

                    decimal xPercentage = totalMinsFromLastDay / maxMinsPerDay;


                    decimal yPercentage = item.Value / maxValue;
                    if (yPercentage > 1)
                        yPercentage = 1;

                    int xpos = xMin + (int)(daySize * xPercentage) + dayprefix;
                    int ypos = ySize - (int)(ySize * yPercentage) + yMax;


                    Graphics.FillRectangle(b2, xpos - 3, ypos - 3, 6, 6);
                }


                Graphics.DrawRectangle(pen2, new Rectangle(xMin, yMax, xSize, ySize));


                var stream = CommonHelper.GetStream(Bitmap);
                await Context.Channel.SendFileAsync(stream, "graph.png");
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString().Substring(0, Math.Min(ex.ToString().Length, 1990)));
            }
            */
        }

        private static EmbedBuilder GenerateEmbedForFirstPoster(List<DiscordUser> users, bool daily)
        {
            // Requires atlest 3 entries
            if (users.Count < 3)
                return null;

            EmbedBuilder builder = new();
            builder.WithColor(25, 100, 255);

            builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

            builder.WithCurrentTimestamp();

            builder.AddField($"Top 1 with {(daily ? users[0].FirstDailyPostCount : users[0].FirstAfternoonPostCount)} posts", users[0].Nickname ?? users[0].Username);
            builder.AddField($"Top 2 with {(daily ? users[1].FirstDailyPostCount : users[1].FirstAfternoonPostCount)} posts", users[1].Nickname ?? users[1].Username);
            builder.AddField($"Top 3 with {(daily ? users[2].FirstDailyPostCount : users[2].FirstAfternoonPostCount)} posts", users[2].Nickname ?? users[2].Username);

            int top = 3;
            foreach (var item in users.Skip(3))
            {
                top++;
                builder.AddField($"Top {top} with {(daily ? item.FirstDailyPostCount : item.FirstAfternoonPostCount)} posts", item.Nickname ?? item.Username, true);
            }

            return builder;
        }

        private static EmbedBuilder GetEmbedForFirstDailyPosts(List<DiscordUser> users)
        {
            var embedBuilder = GenerateEmbedForFirstPoster(users, true);
            embedBuilder.WithTitle("First Daily posters leaderboard");

            return embedBuilder;
        }

        private EmbedBuilder GetEmbedForFirstAfternoonPosts(List<DiscordUser> users)
        {
            var embedBuilder = GenerateEmbedForFirstPoster(users, false);
            embedBuilder.WithTitle("First Afternoon posters leaderboard");

            return embedBuilder;
        }

        [Command("first")]
        public async Task FirstPosterLeaderboard()
        {

            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            try
            {
                // TODO dynamic place number when 2 have the same score

                var author = Context.Message.Author;
                LogManager.ProcessMessage(author, BotMessageType.Other);

                var topFirstDailyPosters = DatabaseManager.GetTopFirstDailyPosterDiscordUsers();
                var topFirstAfternoonPosters = DatabaseManager.GetTopFirstAfternoonPosterDiscordUsers();


                var firstDailyEmbed = GetEmbedForFirstDailyPosts(topFirstDailyPosters);
                var firstAfternoonEmbed = GetEmbedForFirstAfternoonPosts(topFirstAfternoonPosters);


                await Context.Channel.SendMessageAsync("", false, firstDailyEmbed.Build());
                await Context.Channel.SendMessageAsync("", false, firstAfternoonEmbed.Build());
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString(), false);
            }
        }


        [Command("last")]
        public async Task LastPoster()
        {
            var author = Context.Message.Author;
            var messageCount = DatabaseManager.GetDiscordUserMessageCount(author.Id);

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle($"{author.Username} IS THE LAST POSTER");
            builder.WithColor(0, 0, 255);
            builder.WithDescription($"This is the {CommonHelper.DisplayWithSuffix(messageCount)} time you are the last poster.");

            builder.WithAuthor(author);
            builder.WithCurrentTimestamp();

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("cloud_gen")]
        public async Task GenerateCloud()
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                return;
            }

            var txtFile = Path.Combine(Program.ApplicationSetting.BasePath, "Database", "MessagesText.txt");

            File.WriteAllText(txtFile, ""); // reset file

            int count = 0;

            while (true)
            {
                var messagesToProcess = DatabaseManager.GetDiscordMessagesPaged(count);

                if (messagesToProcess.Count == 0)
                {
                    await Context.Channel.SendMessageAsync("Done", false);

                    break;
                }

                string textToAdd = "";
                foreach (var item in messagesToProcess)
                {
                    textToAdd += item.Content + " ";
                }

                count += messagesToProcess.Count;

                File.AppendAllText(txtFile, textToAdd);

                await Context.Channel.SendMessageAsync($"Processed {messagesToProcess.Count}/{count} messages", false);
            }
        }

        private BannedLink GetReportInfoByImage(string imageUrl)
        {
            return DatabaseManager.GetBannedLink(imageUrl);
        }

        private string GetRankingString(IEnumerable<string> list)
        {
            string rankingString = "";
            int pos = 1;
            foreach (var item in list)
            {
                string boldText = pos == 1 ? " ** " : "";
                rankingString += $"{boldText}{pos}) {item}{boldText}{Environment.NewLine}";
                pos++;
            }
            return rankingString;
        }
    }
}
