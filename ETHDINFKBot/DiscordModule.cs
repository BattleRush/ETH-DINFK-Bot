using Discord;
using Discord.Addons.Interactive;
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
using System.Drawing;
using System.Drawing.Drawing2D;
using Color = System.Drawing.Color;
using System.Net;
using ETHDINFKBot.Helpers;
using SkiaSharp;
using CSharpMath.SkiaSharp;
using System.Globalization;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using ETHDINFKBot.Drawing;
using System.Reflection;
using TimeZoneConverter;

namespace ETHDINFKBot
{

    //FAA for spacex stuff https://www.faa.gov/data_research/commercial_space_data/launches/
    public class DiscordModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger _logger = new Logger<DiscordModule>(Program.Logger);

        public static NekoClient NekoClient = new NekoClient("BattleRush's Helper");
        public static NekosFun NekosFun = new NekosFun();

        static List<RestUserMessage> LastMessages = new List<RestUserMessage>();

        DatabaseManager DatabaseManager = DatabaseManager.Instance();

        LogManager LogManager = new LogManager(DatabaseManager.Instance()); // not needed to pass a singleton actually


        // TODO Remove alot of the redundant code for loggining and stats


        private bool AllowedToRun(BotPermissionType type)
        {
            // since this is always calles works for now as workaround
            NekoClient.LogType = LogType.None;

            var channelSettings = DatabaseManager.GetChannelSetting(Context.Message.Channel.Id);
            if (Context.Message.Author.Id != Program.Owner
                && !((BotPermissionType)channelSettings?.ChannelPermissionFlags).HasFlag(type))
            {
#if DEBUG
                Context.Channel.SendMessageAsync("blocked by perms", false);
#endif
                return true;
            }

            return false;
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

            var ownerUser = Program.Client.GetUser(Program.Owner);
            builder.WithThumbnailUrl(ownerUser.GetAvatarUrl());
            builder.WithAuthor(ownerUser);
            builder.WithCurrentTimestamp();

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        // https://stackoverflow.com/a/4423615/3144729
        // TODO Move into a helper
        private string ToReadableString(TimeSpan span)
        {
            string formatted = string.Format("{0}{1}{2}{3}",
                span.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", span.Days, span.Days == 1 ? string.Empty : "s") : string.Empty,
                span.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", span.Hours, span.Hours == 1 ? string.Empty : "s") : string.Empty,
                span.Duration().Minutes > 0 ? string.Format("{0:0} min{1}, ", span.Minutes, span.Minutes == 1 ? string.Empty : "s") : string.Empty,
                span.Duration().Seconds > 0 ? string.Format("{0:0} sec{1}", span.Seconds, span.Seconds == 1 ? string.Empty : "s") : string.Empty);

            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";

            return formatted;
        }

        // GET CPU USAGE
        // https://medium.com/@jackwild/getting-cpu-usage-in-net-core-7ef825831b8b
        private async Task<double> GetCpuUsageForProcess()
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
            var currentProcessCpuUsage = GetCpuUsageForProcess();
            var proc = Process.GetCurrentProcess();

            var currentAssembly = Assembly.GetExecutingAssembly();
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string productVersion = fileVersionInfo.ProductVersion;
            string fileVersion = fileVersionInfo.FileVersion;
            bool isDebug = fileVersionInfo.IsDebug;



            var netCoreVer = Environment.Version;
            var runtimeVer = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            var osVersion = Environment.OSVersion;
            var applicationOnlineTime = (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime());


            var processorCount = Environment.ProcessorCount;
            var ram = proc.WorkingSet64;
            var freeBytes = new DriveInfo(assembly.Location).AvailableFreeSpace;
            var totalBytes = new DriveInfo(assembly.Location).TotalSize;

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

            //builder.WithFooter($"If you can read this then ping Mert | TroNiiXx | [13]");
            builder.WithCurrentTimestamp();
            //builder.WithAuthor(author);
            builder.AddField("Version", $"{version.ToString()}", true);
            builder.AddField("Product Version", $"{productVersion}", true);
            builder.AddField("File Version", $"{fileVersion}", true);
            builder.AddField("Build Mode", $"{(isDebug ? "Debug" : "Release")}", true);
            builder.AddField(".NET Version", $"{netCoreVer.ToString()}", true);
            builder.AddField("Runtime Version", $"{runtimeVer.ToString()}", true);
            builder.AddField("OS Version", $"{osVersion.ToString()}", true);
            builder.AddField("Online for", $"{ToReadableString(applicationOnlineTime)}", true);
            builder.AddField("Processor Count", $"{processorCount.ToString("N0")}", true);

            double cpuUsage = await currentProcessCpuUsage;

            builder.AddField("CPU", $"{Math.Round(cpuUsage, 2)}%", true);
            builder.AddField("RAM", $"{Math.Round(ram / 1024d / 1024d / 1024d, 2)} GB", true);
            builder.AddField("DISK", $"{Math.Round((totalBytes - freeBytes) / 1024d / 1024d / 1024d, 2)} GB out of {Math.Round(totalBytes / 1024d / 1024d / 1024d, 2)} GB ({Math.Round(100 * ((totalBytes - freeBytes) / (decimal)totalBytes), 2)}%)", true);

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("help")]
        public async Task HelpOutput()
        {
            // _logger.LogError("GET HelpOutput called.");


            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;


            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Other);

            EmbedBuilder builder = new EmbedBuilder();

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

            //builder.WithFooter($"If you can read this then ping Mert | TroNiiXx | [13]");
            builder.WithCurrentTimestamp();
            //builder.WithAuthor(author);
            builder.AddField("Misc", $"```{prefix}help {prefix}version {prefix}source {prefix}stats {prefix}lb```", true);
            builder.AddField("Search", $"```{prefix}google|duck <search term>```", true);
            //builder.AddField("Images", $"```{prefix}neko[avatar] {prefix}fox {prefix}waifu {prefix}baka {prefix}smug {prefix}holo {prefix}avatar {prefix}wallpaper```");
            builder.AddField("Reddit", $"```{prefix}r[p] <subreddit>|all```", true);
            builder.AddField("Rant", $"```{prefix}rant [ types | (<type> <message>) ]```", true);
            builder.AddField("SQL", $"```{prefix}sql (table info) | (query[d] <query>)```", true);
            builder.AddField("Emote", $"```{prefix}emote <search_string> [<page>] | {prefix}<emote_name>```");
            builder.AddField("React (only this server emotes)", $"```{prefix}react <message_id> <emote_name>```", true);
            //builder.AddField("Space Min: 1 Max: 5", $"```{prefix}space [<amount>]```", true);
            builder.AddField("WIP Command", $"```{prefix}messagegraph [all|lernphase|bp]```", true);
            builder.AddField("ETH DINFK Place", $"```Type '{prefix}place help' for more information```");

            /*builder.AddField("Write .study to force yourself away from discord", "```May contain spoilers to old exams! Once you receive the study role you will be only to chat for max of 15 mins at a time." + Environment.NewLine +
               $"If you are in cooldown, the bot will delete all your messages. Every question is designed to be able to solve within 5-10 mins. To recall your message write '.study'" + Environment.NewLine +
               $"To be able to chat you will need to solve a question each time. (All subject channels are exempt from this rule.)```");*/
            builder.AddField($"Random Exam Question (for now only LinAlg) Total tracking: {new StudyHelper().GetQuestionCount()} question(s)", $"```{prefix}question [Exam] (Exams: {new StudyHelper().GetExams()})```");
            //builder.AddField(".next", "```Regenerate a new question.```");

            Context.Channel.SendMessageAsync("", false, builder.Build());
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

            EmbedBuilder builder = new EmbedBuilder();

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

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("google")]
        [Summary("Returns info about the current user, or the user parameter, if one passed.")]
        public async Task GoogleSearch([Remainder] string searchString)
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            var userInfo = Context.Message.Author;
            //await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");

            LogManager.ProcessMessage(userInfo, BotMessageType.Search);

            var reply = new GoogleEngine().Search(searchString);

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
                builder.AddField(item.title, item.description + Environment.NewLine + item.url);
            }

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }

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
                Context.Channel.SendMessageAsync($"The current image has been blocked by {user.Nickname}. Try the command again to get a new image", false);
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
        }

        /*[Command("wallpaper")] // TODO INTEGRATE 2 wallpaper endpoints
        public async Task Wallpaper()
        {
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Wallpaper);

            var req = await NekoClient.Image_v3.Wallpaper();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }*/

        // TODO alot of rework to do
        // TODO dynamic image sizes
        // TODO support 100+
        // TODO gifs -> video?
        private Stream DrawPreviewImage(List<DiscordEmote> emojis, List<GuildEmote> guildEmotes)
        {
            int page = 10;

            int padding = 50;
            int paddingY = 55;

            int imgSize = 48;
            int blockSize = imgSize + 35;

            int yOffsetFixForImage = 2;
            int xOffsetFixForText = -3;

            int width = Math.Min(emojis.Count, 10) * blockSize + padding;
            int height = (int)(Math.Ceiling(emojis.Count / 10d) * blockSize + paddingY);

            width = Math.Max(width + 25, 350); // because of the title

            Bitmap Bitmap = new Bitmap(width, height); // TODO insert into constructor
            Graphics Graphics = Graphics.FromImage(Bitmap);
            Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Graphics.Clear(Color.FromArgb(54, 57, 63));

            Font drawFont = new Font("Arial", 10, FontStyle.Bold);
            Font drawFontTitle = new Font("Arial", 12, FontStyle.Bold);
            Font drawFontIndex = new Font("Arial", 16, FontStyle.Bold);

            Brush brush = new SolidBrush(Color.White);
            Brush brushNormal = new SolidBrush(Color.LightSkyBlue);
            Brush brushGif = new SolidBrush(Color.Coral);
            Brush brushEmote = new SolidBrush(Color.Gold);

            Graphics.DrawString($"Normal emote", drawFont, brushNormal, new Point(10, 10));
            Graphics.DrawString($"Gif emote", drawFont, brushGif, new Point(125, 10));
            Graphics.DrawString($"Server emote", drawFont, brushEmote, new Point(210, 10));

            Pen p = new Pen(brush);

            // TODO make it more robust and cleaner
            for (int i = 0; i < page; i++)
            {
                Graphics.DrawString($"[{i}]", drawFontIndex, brush, new Point(10, i * blockSize + paddingY + 12));

                for (int j = 0; j < page; j++)
                {
                    if (emojis.Count <= i * j)
                        break;

                    try
                    {
                        var emote = emojis[i * page + j];

                        Bitmap bmp;
                        using (var ms = new MemoryStream(File.ReadAllBytes(emote.LocalPath)))
                        {
                            bmp = new Bitmap(ms);
                        }

                        Brush b = brushNormal;

                        if (emote.Animated)
                        {
                            b = brushGif;
                        }

                        if (guildEmotes.Any(i => i.Id == emote.DiscordEmoteId))
                        {
                            // this server contains this emote
                            b = brushEmote;
                        }

                        Graphics.DrawImage(bmp, j * blockSize + padding, i * blockSize + paddingY + yOffsetFixForImage, imgSize, imgSize);
                        Graphics.DrawString($"{emote.EmoteName}", drawFont, b, new Point(j * blockSize + padding + xOffsetFixForText, i * blockSize + j % 2 * (imgSize + 15) + paddingY - 15));
                    }
                    catch (Exception ex)
                    {

                    }
                }

                Graphics.DrawLine(p, new Point(0, i * blockSize + paddingY - 15), new Point(width, i * blockSize + paddingY - 15));
            }

            Stream mst = new MemoryStream();
            Bitmap.Save(mst, System.Drawing.Imaging.ImageFormat.Png);
            mst.Position = 0;

            return mst;
        }

        [Command("react")]
        public async Task ReactEmote(ulong messageid, string emoteName)
        {
            if (Context.Channel is SocketGuildChannel guildChannel)
            {
                var emote = guildChannel.Guild.Emotes.FirstOrDefault(i => i.Name.ToLower().Contains(emoteName.ToLower()));

                if (emote == null)
                    return;

                var message = await Context.Channel.GetMessageAsync(messageid);
                await message.AddReactionAsync(emote);
            }
            Context.Message.DeleteAsync();
        }

        [Command("ping")]
        public async Task GhostPing()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            try
            {
                var user = Context.Message.Author as SocketGuildUser;

                List<PingHistory> pingHistory = new List<PingHistory>();

                pingHistory.AddRange(DatabaseManager.GetLastPingHistory(20, user.Id, null));

                foreach (var userRole in user.Roles)
                {
                    ulong roleId = DiscordHelper.GetRoleIdFromMention(userRole);
                    pingHistory.AddRange(DatabaseManager.GetLastPingHistory(5, null, roleId));
                }

                pingHistory = pingHistory.OrderByDescending(i => i.PingHistoryId).ToList();

                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle($"Your last pings");

                pingHistory = pingHistory.Take(20).ToList();

                string messageText = "";
                foreach (var item in pingHistory)
                {
                    //if (item.DiscordMessageId == null)
                    //    continue;

                    var dbMessage = DatabaseManager.GetDiscordMessageById(item.DiscordMessageId);

                    var dateTime = SnowflakeUtils.FromSnowflake(item.DiscordMessageId ?? 0); // TODO maybe track time in the ping history

                    var dateTimeCET = dateTime.Add(Program.TimeZoneInfo.GetUtcOffset(DateTime.Now)); // CEST CONVERSION

                    if (item.DiscordRoleId.HasValue)
                        messageText += $"<@{item.FromDiscordUserId}> pinged <@&{item.DiscordRoleId}> at {dateTimeCET.ToString("dd.MM HH:mm")} in <#{dbMessage?.DiscordChannelId}> {Environment.NewLine}"; // todo check for everyone or here
                    else
                        messageText += $"<@{item.FromDiscordUserId}> at {dateTimeCET.ToString("dd.MM HH:mm")} in <#{dbMessage?.DiscordChannelId}> {Environment.NewLine}";
                }

                messageText += Environment.NewLine;

                builder.WithDescription(messageText);
                builder.WithColor(128, 64, 128);

                builder.WithAuthor(user);
                builder.WithCurrentTimestamp();

                await Context.Message.Channel.SendMessageAsync("", false, builder.Build());
            }
            catch (Exception ex)
            {

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
            builder.AddField("Is Accepting", $"{ws.IsAccepting}", true);
            builder.AddField("Is Started", $"{ws.IsStarted}", true);
            builder.AddField("IP Endpoint", $"{ws.Endpoint}", true);
            builder.AddField("Connected Sessions", $"{ws.ConnectedSessions}", true);
            builder.AddField("Bytes Pending", $"{ws.BytesPending.ToString("N0")}", true);
            builder.AddField("Bytes Received", $"{ws.BytesReceived.ToString("N0")}", true);
            builder.AddField("Bytes Sent", $"{ws.BytesSent.ToString("N0")}", true);

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }


        // TODO duplicate finder -> fingerprint
        // TODO better selection
        [Command("emote")]
        public async Task EmojiInfo(string search, int page = 0, bool debug = false)
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            //await Context.Channel.SendMessageAsync($"Disabled dev", false); // to prevent from db overload
            //return;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            var author = Context.Message.Author;

            if (search.Length < 2 && author.Id != Program.Owner)
            {
                await Context.Channel.SendMessageAsync($"Search term needs to be atleast 2 characters long", false); // to prevent from db overload
                return;
            }

            var emotes = DatabaseManager.GetEmotesByName(search); // TODO dont dowload the emote data before its further filtered

            int count = 0;

            if (count > 0)
            {
                await Context.Channel.SendMessageAsync($"Downloaded {count} emote(s)", false);
            }

            List<GuildEmote> guildEmotes = new List<GuildEmote>();

            if (Context.Channel is SocketGuildChannel guildChannel)
                guildEmotes = guildChannel.Guild.Emotes.ToList();

            int total = emotes.Count;
            // limit to 100

            int rowMax = debug ? 8 : 10; // since it will reach the char count
            int columnMax = 10;
            int pageSize = rowMax * columnMax;


            string nextPageInfo = "";

            if (total > (page + 1) * pageSize)
            {
                nextPageInfo = $" or .emote {search} {page + 1} for next page";
            }


            Dictionary<string, int> dupes = new Dictionary<string, int>();

            string sep = "-";

            foreach (var emote in emotes)
            {
                int offset = 0;

                if (dupes.ContainsKey(emote.EmoteName.ToLower()))
                {
                    // we found a dupe
                    offset = dupes[emote.EmoteName.ToLower()] += 1;
                }
                else
                {
                    dupes.Add(emote.EmoteName.ToLower(), 1);
                }

                string offsetString = "";
                if (debug)
                {
                    emote.EmoteName = emote.DiscordEmoteId.ToString();
                }
                else
                {
                    if (offset > 0)
                    {
                        emote.EmoteName += $"{sep}{offset}";
                    }
                }
            }

            emotes = emotes.Skip(page * pageSize).Take(pageSize).ToList();

            // TODO make it look nice
            string text = $"**Available({page * pageSize}-{Math.Min((page + 1) * pageSize, total)}/{total}) '{search}' emojis to use (Usage .<name>){nextPageInfo}**" + Environment.NewLine;

            int countEmotes = 0;
            int row = 0;

            text += "```css" + Environment.NewLine;

            text += "[0] ";



            foreach (var emoji in emotes)
            {
                string emoteString = $".{emoji.EmoteName} ";

                if (emoji.Animated)
                    emoteString = $"[{emoji.EmoteName}] ";

                if (guildEmotes.Any(i => i.Id == emoji.DiscordEmoteId))
                    emoteString = $"({emoji.EmoteName}) ";


                text += emoteString;

                countEmotes++;

                if (countEmotes >= columnMax)
                {
                    row++;
                    text += Environment.NewLine;

                    if (row < rowMax)
                    {
                        text += $"[{row}] ";
                    }
                    countEmotes = 0;
                }

                /*          if (text.Length > 1950)
                {
                    await Context.Channel.SendMessageAsync(text, false);
                    text = "";
                }*/
            }

            text += "```";

            //await Context.Channel.SendMessageAsync(, false);



            var stream = DrawPreviewImage(emotes, guildEmotes);

            if (text.Length > 1990)
            {
                text = text.Substring(0, 1990);
            }
            watch.Stop();
            await Context.Channel.SendFileAsync(stream, $"emote_{search}.png", text + $"Time: {watch.ElapsedMilliseconds} ms");
        }


        [Command("study", RunMode = RunMode.Async)]
        public async Task Study(ulong confirmId = 0)
        {
            return;
            if (confirmId == 0)
            {
                await Context.Channel.SendMessageAsync($"May contain spoilers to old exams! Once you receive the study role you will be only to chat for max of 15 mins at a time." + Environment.NewLine +
                    $"If you are in cooldown, the bot will delete all your messages. Every question is designed to be able to solve within 5-10 mins. To recall your message write '.study'" + Environment.NewLine +
                    $" To be able to chat you will need to solve a question each time. (All subject channels are exempt from this rule.)" + Environment.NewLine +
                    $"Enter code: .study {Context.Message.Author.Id}", false);
            }
            else if (confirmId == Context.Message.Author.Id)
            {
                try
                {
                    var user = Context.User;
                    var role = Context.Guild.Roles.FirstOrDefault(x => x.Id == 798639212818726952); // study role
                    await (user as IGuildUser).AddRoleAsync(role);
                    await Context.Channel.SendMessageAsync("Role assigned. Good luck!");
                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                await Context.Channel.SendMessageAsync("Wrong code");
            }
        }

        [Command("question", RunMode = RunMode.Async)]
        public async Task Question([Remainder] string filter = null)
        {
            return;
            try
            {
                // TODO disable subjects if the exam is behind

                StudyHelper helper = new StudyHelper();

                var question = helper.GetRandomLinalgQuestion(filter);
                //if (!Program.CurrentActiveQuestion.ContainsKey(Context.Message.Author.Id))
                //    Program.CurrentActiveQuestion.Add(Context.Message.Author.Id, question);

                PrintQuestion(question);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            // TODO place the question into global
        }


        [Command("repeat", RunMode = RunMode.Async)]
        public async Task RepeatQuestion()
        {
            return;
            // TODO disable subjects if the exam is behind

            StudyHelper helper = new StudyHelper();

            Question question = null;
            if (Program.CurrentActiveQuestion.ContainsKey(Context.Message.Author.Id))
                question = Program.CurrentActiveQuestion[Context.Message.Author.Id];

            if (question == null)
                return;


            PrintQuestion(question);

            // TODO place the question into global
        }


        [Command("tex", RunMode = RunMode.Async)]
        public async Task Latex([Remainder] string input)
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
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
                msg.DeleteAsync();
            }
            else
            {
                var msg = await Context.Channel.SendMessageAsync(text);
                await Task.Delay(secWait * 1000);
                msg.DeleteAsync();
            }
        }

        [Command("wallpaper", RunMode = RunMode.Async)]
        [Alias("wp")]
        public async Task Wallpaper()
        {
            return;
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Wallpaper);

            var req = NekosFun.GetLink("wallpaper");
            BannedLink report = null;

            string regenString = "";

            do
            {
                try
                {
                    report = GetReportInfoByImage(req);
                    if (report != null)
                    {

                        var user = DatabaseManager.GetDiscordUserById(report.AddedByDiscordUserId);
                        regenString += $"An image has been blocked by {user.Nickname}. Regenerating a new image just for you :)" + Environment.NewLine;
                        req = NekosFun.GetLink("wallpaper");

                        //return;
                    }
                }
                catch (Exception ex)
                {
                    return;
                }
            } while (report != null);

            if (regenString.Length > 0)
            {
                await Context.Channel.SendMessageAsync(regenString, false);
            }

            var message = await Context.Channel.SendMessageAsync(req, false);

            // disabled for now
            if (false)
                await AddSaveReact(message);

            AddMessageToList(message);

            if (new Random().Next(0, 20) == 0)
            {
                // Send only every x messages
                Context.Channel.SendMessageAsync("wallpaper may still contain some NSFW images. To remove them type '.block link' To get the link, right click the image -> Copy Link. Do not use < > around the link", false);
            }
        }

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


        public async void AddMessageToList(RestUserMessage message)
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
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                //Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                //return
            }
            var guildUser = author as SocketGuildUser;

            // Remove < > for no preview if used
            image = image.Replace("<", "").Replace("<", "");



            if (image.Contains("discordapp") || !image.StartsWith("https://"))
            {
                Context.Channel.SendMessageAsync($"You did not provide a valid link.", false);
                return;
            }

            var blockInfo = DatabaseManager.GetBannedLink(image);

            if (blockInfo != null)
            {
                Context.Message.DeleteAsync();
                var user = DatabaseManager.GetDiscordUserById(blockInfo.AddedByDiscordUserId);
                Context.Channel.SendMessageAsync($"Image is already in the blacklist (blocked by {user.Nickname}) You were too slow {guildUser.Nickname} <:exmatrikulator:769624058005553152>", false);
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
                    message.DeleteAsync();
                }
            }

            Context.Channel.SendMessageAsync($"Added the image to blacklist by {guildUser.Nickname}", false);
            Context.Message.DeleteAsync();
        }

        private async Task AddSaveReact(RestUserMessage message)
        {
            await message.AddReactionAsync(Emote.Parse("<:savethis:780179874656419880>"));
        }


        [Command("rant")]
        public async Task Rant(string type = null, [Remainder] string content = "")
        {
            // TODO perm check but for now open everwhere

            if (type == null)
            {
                // get a random rant
                RandomRant();

            }
            else if (type.ToLower() == "help")
            {
                HelpOutput();
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

                Context.Channel.SendMessageAsync("", false, builder.Build());
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
                Context.Channel.SendMessageAsync($"Added rant for {type} Success: {successRant}", false);
            }
        }

        private async void RandomRant(string type = null)
        {
            var rant = DatabaseManager.GetRandomRant(type);
            if (rant == null)
            {
                await Context.Channel.SendMessageAsync($"No rant could be loaded for type {type} (To see all types write: '.rant types')." +
                    $"If you are trying to add a rant type '.rant {type} <your actuall rant>'", false);
                return;
            }

            var byUser = Program.Client.GetUser(rant.DiscordUserId);
            var datePosted = SnowflakeUtils.FromSnowflake(rant.DiscordMessageId);
            var rantType = DatabaseManager.GetRantTypeNameById(rant.RantTypeId);

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle($"Rant about {rantType} on {datePosted:dd.MM.yyyy}");
            builder.Description = rant.Content;
            builder.WithColor(255, 0, 255);
            builder.WithAuthor(byUser);
            builder.WithCurrentTimestamp();
            builder.WithFooter($"RantId: {rant.RantMessageId} TypeId: {rant.RantTypeId}");

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }


        [Command("graph")]
        public async Task Graph()
        {



        }


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
                Context.Channel.SendMessageAsync("", false, builder.Build());
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
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                //Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            if (amount < 1)
                amount = 1;

            Context.Message.DeleteAsync();

            for (int i = 0; i < amount; i++)
            {
                Context.Channel.SendMessageAsync(message, false);
                await Task.Delay(1250);
            }
        }

        [Command("purge")]
        public async Task Purge(int count, bool fromBot = false)
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                //Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            ulong fromUserToDelete = fromBot ? 774276700557148170 : ETHDINFKBot.Program.Owner;

            if (fromBot)
            {
                Context.Message.DeleteAsync();
            }

            var messages = await Context.Channel.GetMessagesAsync(100).FlattenAsync(); //defualt is 100
            messages = messages.Where(i => i.Author.Id == fromUserToDelete).OrderByDescending(i => i.Id).Take(count);
            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messages);
        }

        [Command("nuke")]
        public async Task Nuke(int count, bool tts = false)
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
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


            var messages = Context.Channel.GetMessagesAsync(count).FlattenAsync(); //defualt is 100

            var messageCountDown = await Context.Channel.SendMessageAsync("https://media4.giphy.com/media/tBvPFCFQHSpEI/200.gif");
            Context.Channel.SendMessageAsync($"Placing a tactical nuke KMN-{count}. Scheduled to detonate in 10 seconds.");

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

        [Command("countdown2021")]
        public async Task countdown2021()
        {
            return;
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                //Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            Task t = new Task(() => CountdownLoop(Context));
            t.Start();

        }

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




        [Command("test")]
        public async Task Test()
        {
            return;
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("BattleRush's Helper Stats");
            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");

            builder.WithColor(0, 100, 175);

            // Profile image of top person -> to update
            //builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");

            builder.WithCurrentTimestamp();
            builder.AddField("Top Emoji Usage", $"<:checkmark:778202017372831764>");
            builder.AddField("<:checkmark:778202017372831764>", $"test");
            Context.Channel.SendMessageAsync("", false, builder.Build());
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
                    Context.Channel.SendMessageAsync(allSubreddits, false);
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

                Context.Channel.SendMessageAsync(link, false);

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
            return;

            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            if (amount < 1)
                amount = 1;
            if (amount > 5)
                amount = 5;

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

                    Context.Channel.SendMessageAsync(message);
                }
            }
            catch (Exception ex)
            {
                Context.Channel.SendMessageAsync(ex.ToString());
            }

            Context.Message.DeleteAsync();
        }

        [Command("disk")]
        public void DirSizeReddit()
        {
            return; // disable
            try
            {
                DirectoryInfo info = new DirectoryInfo("Reddit");
                long size = DirSize(info);

                Context.Channel.SendMessageAsync($"Current Reddit disk usage :{size / (decimal)1024 / 1024 / 1024} GB", false);
            }
            catch (Exception ex)
            {
                Context.Channel.SendMessageAsync(ex.ToString(), false);
            }
        }

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
                Context.Channel.SendMessageAsync(allSubreddits, false);
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
                            Context.Channel.SendMessageAsync(allSubreddits, false);
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

                        EmbedBuilder builder = new EmbedBuilder();

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

                        Context.Channel.SendMessageAsync("", false, builder.Build());

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

        [Command("drawtest")]
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

        }


        [Command("testpiechart")]
        public async Task testpiechart()
        {
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
            }
        }

        [Command("messagegraph")]
        public async Task MessageGraph(string param = null)
        {
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

        }

        [Command("lb")]
        public async Task Leaderboard()
        {

            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            try
            {

                var author = Context.Message.Author;
                LogManager.ProcessMessage(author, BotMessageType.Other);
                /*
                var statText = DatabaseManager.GetTopEmojiStatisticByText(10);
                var statTextBot = DatabaseManager.GetTopEmojiStatisticByBot(10);
                var statTextOnce = DatabaseManager.GetTopEmojiStatisticByTextOnce(10);
                var statTextReaction = DatabaseManager.GetTopEmojiStatisticByReaction(10);
                //var statEmoji = DatabaseManager.ping(10);


                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("BattleRush's Helper Stats");
                //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");

                builder.WithColor(0, 100, 175);

                // Profile image of top person -> to update
                //builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");

                builder.WithCurrentTimestamp();
                builder.AddField("Top Emoji", GetRankingString(statTextOnce.Select(i => $"<{(i.Animated ? "a:" : ":") + i.EmojiName}:{i.EmojiId}> " + i.UsedInTextOnce)), true);
                builder.AddField("Top Emoji (all)", GetRankingString(statText.Select(i => $"<{(i.Animated ? "a:" : ":") + i.EmojiName}:{i.EmojiId}> " + i.UsedInText)), true);
                //builder.AddField("Top Emoji (from Bots)", GetRankingString(statTextBot.Select(i => $"<{(i.Animated ? "a:" : ":") + i.EmojiName}:{i.EmojiId}> " + i.UsedByBots)), true);
                builder.AddField("Top Reactions", GetRankingString(statTextReaction.Select(i => $"<{(i.Animated ? "a:" : ":") + i.EmojiName}:{i.EmojiId}> " + i.UsedAsReaction)), true);
                builder.AddField("Top Pinged Users", "TODO");

                Context.Channel.SendMessageAsync("", false, builder.Build());*/
            }
            catch (Exception ex)
            {
                Context.Channel.SendMessageAsync(ex.ToString(), false);
            }
        }

        [Command("cloud_gen")]
        public async Task GenerateCloud()
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                return;
            }

            var txtFile = Path.Combine(Program.BasePath, "Database", "MessagesText.txt");

            File.WriteAllText(txtFile, ""); // reset file

            int count = 0;

            while (true)
            {
                var messagesToProcess = DatabaseManager.GetDiscordMessagesPaged(count);

                if (messagesToProcess.Count == 0)
                {
                    Context.Channel.SendMessageAsync("Done", false);

                    break;
                }

                string textToAdd = "";
                foreach (var item in messagesToProcess)
                {
                    textToAdd += item.Content + " ";
                }

                count += messagesToProcess.Count;

                File.AppendAllText(txtFile, textToAdd);

                Context.Channel.SendMessageAsync($"Processed {messagesToProcess.Count}/{count} messages", false);
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


    public class ModIntroduction : InteractiveBase
    {
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        [Command("ACP")]
        public async Task ACPanel()
        {
            var m = Context.Message;
            var author = Context.Message.Author;
            if (author.Id != 321022340412735509)
            {
                Context.Channel.SendMessageAsync($"Unauthorized access atempt. Banning <@{author.Id}>", false);
                return;
            }

            await m.Channel.SendMessageAsync("https://i.gifer.com/embedded/download/8XAj.gif");
            await m.Channel.SendMessageAsync("**Entering Admin Control Panel**");
            Thread.Sleep(4000);
            await m.Channel.SendMessageAsync("**ACP open** Hello <@321022340412735509>");
            Thread.Sleep(3000);

            var selectOption = await m.Channel.SendMessageAsync(@"Loading options please wait");
            Thread.Sleep(2000);

            await selectOption.ModifyAsync(msg => msg.Content = @"Loading options please wait.
1) Give Marc Admin");
            Thread.Sleep(2000);

            await selectOption.ModifyAsync(msg => msg.Content = @"Loading options please wait..
1) Give Marc Admin
2) Delete Server");
            Thread.Sleep(2000);

            await selectOption.ModifyAsync(msg => msg.Content = @"Loading options please wait...
1) Give Marc Admin
2) Delete Server
3) Ban all current Admins");
            Thread.Sleep(2000);

            await selectOption.ModifyAsync(msg => msg.Content = @"Loading options please wait..
1) Give Marc Admin
2) Delete Server
3) Ban all current Admins
4) Show BP2 Exam Solutions");
            Thread.Sleep(2000);

            await selectOption.ModifyAsync(msg => msg.Content = @"Loading options please wait.
1) Give Marc Admin
2) Delete Server
3) Ban all current Admins
4) Show BP2 Exam Solutions
5) Assign Random User Moderator");
            Thread.Sleep(2000);

            await selectOption.ModifyAsync(msg => msg.Content = @"Loading options please wait
1) Give Marc Admin
2) Delete Server
3) Ban all current Admins
4) Show BP2 Exam Solutions
5) Assign Random User Moderator
6) Remove Mod from Marc");

            Thread.Sleep(2000);

            await selectOption.ModifyAsync(msg => msg.Content = @"Type an option you want to select:
1) Give Marc Admin
2) Delete Server
3) Ban all current Admins
4) Show BP2 Exam Solutions
5) Assign Random User Moderator
6) Remove Mod from Marc
7) Exit");

            var response = await NextMessageAsync(true, true, TimeSpan.FromMinutes(3));
            await m.Channel.SendMessageAsync($"You have chosen option: {response.Content}");

            if (response.Content != "5")
            {
                await m.Channel.SendMessageAsync("NotImplemented Exception has been throws. Exiting ACP");
                return;
            }

            var breachMessage = await m.Channel.SendMessageAsync("Selecting random user to become mod. 1 out of 997 Users selected");

            Thread.Sleep(1000);

            // unlock next stage
            var randomString = await m.Channel.SendMessageAsync("Seed: GENERATING_SEED");
            Thread.Sleep(2000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);
            await randomString.ModifyAsync(msg => msg.Content = "Seed: " + RandomString(32));
            Thread.Sleep(1000);


            var initMsg = await m.Channel.SendMessageAsync("User selected.");
            Thread.Sleep(3000);
            await initMsg.ModifyAsync(msg => msg.Content = @"Initializing process to assign mod..");
            Thread.Sleep(2000);

            await initMsg.ModifyAsync(msg => msg.Content = @"Initializing process to assign mod....");
            Thread.Sleep(3000);

            await initMsg.ModifyAsync(msg => msg.Content = @"Initializing process to assign mod......");
            Thread.Sleep(4000);

            await PrintProgressBar(m);

            string hex = "48656c6c6f2c20696620796f75206172652072656164696e672074686973206d6573736167653a20436f6e67726174756c6174696f6e212120596f7520686176652070726f6772657373656420746f20746865206e6578742073746167652e2054686973206d65737361676520697320686f7765766572206f6e6c79206d65616e7420666f72206f6e6520706572736f6e206f6e6c792e20546f2076616c696461746520796f752061726520746865207265616c206465616c20616e6420776f756c64206c696b6520746f206a6f696e20746865204d6f6465726174696f6e2074696d652c20737461727420746865202e3c796f7572446973636f726449643e20636f6d6d616e6420746f2070726f6365656420746f20746865206e65787420737465702e2054686973207761792077652063616e20656e737572652074686174206e6f20696d706f737465722063616e207472696767657220746865206e6578742073746167652e20496620796f75206861766520636f6e74696e75656420746f20746f207265616420757020746f20686572652c206865726520697320612068696e742e20546865204964206973204175737472616c69616e203b2920474c2120202020202020202020202020202020202020202020202020202020202020202020";

            var bytes = StringToByteArray(hex);

            int size = 22;

            int pixelSize = 10;

            var board = DrawingHelper.GetEmptyGraphics(size * pixelSize, size * pixelSize);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int xBase = x * pixelSize;
                    int yBase = y * pixelSize;

                    for (int xx = 0; xx < pixelSize; xx++)
                    {
                        for (int yy = 0; yy < pixelSize; yy++)
                        {
                            byte r = x * size + y - 1 >= 0 ? bytes[x * size + y - 1] : (byte)0;
                            byte g = x * size + y - 0 >= 0 ? bytes[x * size + y - 0] : (byte)0;
                            byte b = x * size + y + 1 < bytes.Length ? bytes[x * size + y+ 1] : (byte)0;
                            board.Bitmap.SetPixel(xBase + xx, yBase + yy, Color.FromArgb(r, g, b));
                        }
                    }
                }
            }

            var stream = CommonHelper.GetStream(board.Bitmap);
            await Context.Channel.SendFileAsync(stream, "secret_message.png", $"Dump file output");

            //return true;
        }

        [Command("449499266612148321")]
        public async Task UserEnter()
        {
            var m = Context.Message;
            var author = Context.Message.Author;
            if (author.Id != 123841216662994944)
            {
                Context.Channel.SendMessageAsync($"Unauthorized access atempt. Banning <@{author.Id}>", false);
                return;
            }

            //await initMsg.DeleteAsync();

            EmbedBuilder nextStage = new EmbedBuilder();

            nextStage.WithTitle($"Confirm to assign <@123841216662994944> to the next stage.");
            nextStage.WithColor(0, 0, 255);
            nextStage.WithAuthor(author);
            nextStage.WithCurrentTimestamp();

            var reactMessage = await m.Channel.SendMessageAsync("Process Initialization Check", false, nextStage.Build());
            reactMessage.AddReactionAsync(Emote.Parse($"<:this:{DiscordHelper.DiscordEmotes["this"]}>"));
        }

        private async Task<bool> PrintProgressBar(SocketMessage m)
        {
            List<string> left = new List<string>() {
                "<:left0:829444101308547136>",
                "<:left1:829444101551423508>",
                "<:left2:829444101614600252>",
                "<:left3:829444101619318814>",
                "<:left4:829444101627707452>",
                "<:left5:829444101639372910>",
                "<:left6:829444304799399946>",
                "<:left7:829444328626847745>",
                "<:left8:829444338840633387>",
                "<:left9:829444353637875772>",
                "<:left10:829444368329998387>"
            };

            List<string> middle = new List<string>() {

                "<:middle0:832534031177613352>",
                "<:middle1:832534056138571796>",
                "<:middle2:832534067156746270>",
                "<:middle3:832534079844778014>",
                "<:middle4:832534090593992705>",
                "<:middle5:832534101969207306>",
                "<:middle6:832534113285963776>",
                "<:middle7:832534125260701726>",
                "<:middle8:832534134927654922>",
                "<:middle9:832534146475229276>",
                "<:middle10:832534158186250260>"
            };
            // Progressbar right

            List<string> right = new List<string>()
            {

                "<:right0:829444702105239613>",
                "<:right1:829444715803443261>",
                "<:right2:829444741062066246>",
                "<:right3:829444752251551744>",
                "<:right4:829444776746549260>",
                "<:right5:829444791137206332>",
                "<:right6:829444802928050206>",
                "<:right7:829444814180319242>",
                "<:right8:829444826843578378>",
                "<:right9:829444840520810586>",
                "<:right10:829444852583759913>"
            };

            var progressText = await m.Channel.SendMessageAsync("Startup");
            var progressBar = await m.Channel.SendMessageAsync(left[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0]);

            //10
            for (int i = 0; i < 11; i++)
            {
                string line = left[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }
            await progressText.ModifyAsync(msg => msg.Content = "Wonder who won the mod lotery <:thonku:747783377846927401>");

            // 20
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "Could it be Marc again?");

            // 30
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "Or you?");

            // 40
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "Weird that such messages come again....");
            // 50
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);

            }

            await progressText.ModifyAsync(msg => msg.Content = "Maybe its just a big troll, who knows");
            // 60
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "Soon..");

            // 70
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "Soon....");
            // 80
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }


            await progressText.ModifyAsync(msg => msg.Content = "Soon......");
            // 90
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            //----------------------------------------------------------------------------------------------------------------------------------------------
            await progressText.ModifyAsync(msg => msg.Content = "SYSTEM FAILURE. SHUTTING DOWN");
            Thread.Sleep(6000);
            //----------------------------------------------------------------------------------------------------------------------------------------------

            // 90
            for (int i = 10; i > 0; i -= 2)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            // 80
            for (int i = 10; i > 0; i -= 2)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            // 70
            for (int i = 10; i > 0; i -= 2)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "GENERATING DUMP FILE");

            // 60
            for (int i = 10; i > 0; i -= 3)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            // 50
            for (int i = 10; i > 0; i -= 3)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);

            }

            // 40
            for (int i = 10; i > 0; i -= 4)
            {
                string line = left[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            // 30
            for (int i = 10; i > 0; i -= 4)
            {
                string line = left[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            // 20
            for (int i = 10; i > 0; i -= 5)
            {
                string line = left[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            //10
            for (int i = 10; i > 0; i -= 5)
            {
                string line = left[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            string lineEnd = left[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
            await progressBar.ModifyAsync(msg => msg.Content = lineEnd);
            return true;
        }
    }
}
