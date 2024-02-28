using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data.Enums;
using ETHDINFKBot.Classes;
using ETHDINFKBot.CronJobs;
using ETHDINFKBot.CronJobs.Jobs;
using ETHDINFKBot.Data;
using ETHDINFKBot.Drawing;
//using ETHDINFKBot.Drawing;
using ETHDINFKBot.Helpers;
using ETHDINFKBot.Log;
using ETHDINFKBot.Struct;
using HtmlAgilityPack;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Reddit;
using Reddit.Controllers;
using RedditScrapper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Web;
using ETHBot.DataLayer.Data.Discord;
using MySqlConnector;
using ETHBot.DataLayer.Data.ETH.Food;
using ETHDINFKBot.Helpers.Food;
using System.Runtime.InteropServices;
using Discord.Net;
using System.ComponentModel.DataAnnotations;

namespace ETHDINFKBot.Modules
{
    public class Class1
    {
        public ulong id { get; set; }
        public string nick { get; set; }
        public string top_role_name { get; set; }
        public ulong top_role_id { get; set; }
    }


    public class EmoteInfo
    {
        public string Name { get; set; }
        public ulong Id { get; set; }
        public bool Animated { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string Url { get; set; }
        public string Folder { get; set; }

    }

    [Group("admin")]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {

        //restartpy - restarts the python bot on port 13225
        [Command("restartpy")]
        public async Task RestartPythonBot()
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            try
            {
                // call localhost:13225/restart
                HttpClient client = new HttpClient();
                var response = client.GetAsync("http://localhost:13225/restart").Result;
                await Context.Channel.SendMessageAsync(response.StatusCode.ToString(), false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message.ToString(), false);
            }

            await Context.Channel.SendMessageAsync("Done", false);
        }

        [Command("download")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task DownloadImages(int count = 100)
        {
            Console.WriteLine("Downloading images");
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            try
            {
                await Context.Channel.SendMessageAsync($"Calling {count} messages", false);
                ulong channelId = 747758757395562557;
                var channelObj = Program.Client.GetChannel(channelId) as SocketTextChannel;

                var messages = await channelObj.GetMessagesAsync(count).FlattenAsync();

                await Context.Channel.SendMessageAsync($"Found {messages.Count()} messages", false);

                HttpClient client = new HttpClient();

                // add chrome user agent
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:122.0) Gecko/20100101 Firefox/122.0");
                // todo other headers?


                int downloaded = 0;
                int skipped = 0;
                int errors = 0;
                int noAttachments = 0;
                int threadCreated = 0;

                int tenorGifs = 0;
                int imgur = 0;
                int youtubeVideos = 0;
                int currentCount = 0;

                foreach (var message in messages)
                {
                    currentCount++;

                    if (currentCount % 250 == 0)
                        await Context.Channel.SendMessageAsync($"Done {currentCount} messages", false);

                    string link = $"https://discord.com/channels/747752542741725244/{channelId}/{message.Id}";

                    if (message.Content.Contains("tenor.com"))
                    {
                        tenorGifs++;
                        continue;
                    }

                    if (message.Content.Contains("imgur.com"))
                    {
                        imgur++;
                        continue;
                    }

                    // if domain twitter skip because images dont have ending in url
                    if (message.Content.Contains("twitter.com") || message.Content.Contains("//x.com") || message.Content.Contains("twimg.com") || message.Content.Contains("fixupx.com"))
                    {
                        // handle them when i have time or never
                        skipped++;
                        continue;
                    }

                    if (message.Content.Contains("youtube.com") || message.Content.Contains("youtu.be"))
                    {
                        youtubeVideos++;
                        continue;
                    }

                    if (message.Type == MessageType.ThreadCreated)
                    {
                        threadCreated++;
                        continue;
                    }

                    if (message.Attachments.Count == 0 && message.Embeds.Count == 0)
                    {
                        var messageType = message.Type;
                        await Context.Channel.SendMessageAsync($"Message {message.Id} has no attachments or embeds link: {link} and type: {messageType}", false);
                        noAttachments++;
                        continue;
                    }

                    int index = 0;
                    if (message.Attachments.Count > 0)
                    {
                        foreach (var attachment in message.Attachments)
                        {
                            string url = attachment.Url;

                            // dont download webp images if possible
                            url = url.Replace("&format=webp", "");

                            // remove width and height query params
                            url = Regex.Replace(url, @"&width=\d+", "");
                            url = Regex.Replace(url, @"&height=\d+", "");

                            // if the parameter is at the start with ? then remove it
                            url = Regex.Replace(url, @"\?width=\d+", "?");
                            url = Regex.Replace(url, @"\?height=\d+", "?");

                            // if url ends with ? then remove it
                            url = Regex.Replace(url, @"\?$", "");

                            if (url.Contains("webp") || url.Contains("webm"))
                            {
                                int t = 1;
                            }

                            try
                            {
                                string fileName = attachment.Filename;

                                fileName = fileName.ToLower(); // so no png and PNG


                                if (!fileName.Contains("."))
                                {
                                    await Context.Channel.SendMessageAsync($"Filename '{fileName}' is invalid from content: ```{message.Content}```", false);
                                    skipped++;
                                    throw new Exception("Invalid filename");
                                }


                                // remove any . except the last one
                                string fileExtension = fileName.Split('.').Last();
                                string name = fileName.Substring(0, fileName.Length - fileExtension.Length - 1);

                                // limit filename to 150 chars max
                                if (name.Length > 100)
                                    name = name.Substring(0, 100);


                                name = name.Replace(".", "");

                                // remove any non alphanumeric chars from name
                                name = Regex.Replace(name, @"[^a-zA-Z0-9_]", "");

                                fileName = $"{message.Id}_{index}_{name}.{fileExtension}";

                                // put image into folder Python/memes
                                string filePath = Path.Combine(Environment.CurrentDirectory, "Python", "memes", fileName);

                                // if os linux dont do ../../..
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                                    filePath = Path.Combine(Environment.CurrentDirectory, "Python", "memes", fileName);

                                // check if folder exists
                                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                                {
                                    await Context.Channel.SendMessageAsync($"Folder {Path.GetDirectoryName(filePath)} does not exist", false);
                                    await Context.Channel.SendMessageAsync($"Content: ```{message.Content}```");
                                    skipped++;
                                    continue;
                                }

                                // check if file exists
                                if (File.Exists(filePath))
                                {
                                    skipped++;
                                    //await Context.Channel.SendMessageAsync($"File {filePath} already exists", false);
                                    continue;
                                }


                                byte[] bytes = client.GetByteArrayAsync(url).Result;
                                File.WriteAllBytes(filePath, bytes);

                                downloaded++;
                            }
                            catch (HttpException ex)
                            {
                                errors++;
                                // if status code 404 then skip
                                if (ex.HttpCode == HttpStatusCode.NotFound) continue;

                                await Context.Channel.SendMessageAsync($"HTTP Download error in attachment ({link}) and url <{url}>: " + ex.Message.ToString(), false);
                            }
                            catch (Exception ex)
                            {
                                errors++;
                                await Context.Channel.SendMessageAsync($"Download error in embed ({link}) and url <{url}>: " + ex.Message.ToString(), false);
                            }
                        }
                    }

                    if (message.Embeds.Count > 0)
                    {
                        foreach (var embed in message.Embeds)
                        {
                            // TODO check other embed types
                            if (embed.Type == EmbedType.Image || embed.Type == EmbedType.Gifv || embed.Type == EmbedType.Video)
                            {
                                string url = embed.Url.Replace("&format=webp", "");
                                // remove width and height query params
                                url = Regex.Replace(url, @"&width=\d+", "");
                                url = Regex.Replace(url, @"&height=\d+", "");

                                // if the parameter is at the start with ? then remove it
                                url = Regex.Replace(url, @"\?width=\d+", "?");
                                url = Regex.Replace(url, @"\?height=\d+", "?");

                                // if url ends with ? then remove it
                                url = Regex.Replace(url, @"\?$", "");

                                if (url.Contains("webp") || url.Contains("webm"))
                                {
                                    int t = 1;
                                }

                                try
                                {
                                    string fileName = embed.Url.Split('/').Last();
                                    fileName = fileName.Split('?').First();

                                    fileName = fileName.ToLower(); // so no png and PNG


                                    if (!fileName.Contains("."))
                                    {
                                        await Context.Channel.SendMessageAsync($"Filename '{fileName}' is invalid from content: ```{message.Content}```", false);
                                        skipped++;
                                        throw new Exception("Invalid filename");
                                    }


                                    // remove any . except the last one
                                    string fileExtension = fileName.Split('.').Last();
                                    string name = fileName.Substring(0, fileName.Length - fileExtension.Length - 1);

                                    // limit filename to 150 chars max
                                    if (name.Length > 100)
                                        name = name.Substring(0, 100);


                                    name = name.Replace(".", "");

                                    // remove any non alphanumeric chars from name
                                    name = Regex.Replace(name, @"[^a-zA-Z0-9_]", "");

                                    fileName = $"{message.Id}_{index}_{name}.{fileExtension}";

                                    // put image into folder Python/memes
                                    string filePath = Path.Combine(Environment.CurrentDirectory, "Python", "memes", fileName);

                                    // if os linux dont do ../../..
                                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                                        filePath = Path.Combine(Environment.CurrentDirectory, "Python", "memes", fileName);

                                    // check if folder exists
                                    if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                                    {
                                        await Context.Channel.SendMessageAsync($"Folder {Path.GetDirectoryName(filePath)} does not exist", false);
                                        return;
                                    }

                                    // check if file exists
                                    if (File.Exists(filePath))
                                    {
                                        skipped++;
                                        //await Context.Channel.SendMessageAsync($"File {filePath} already exists", false);
                                        continue;
                                    }

                                    byte[] bytes = client.GetByteArrayAsync(url).Result;
                                    File.WriteAllBytes(filePath, bytes);

                                    downloaded++;
                                }
                                catch (HttpException ex)
                                {
                                    errors++;
                                    // if status code 404 then skip
                                    if (ex.HttpCode == HttpStatusCode.NotFound) continue;

                                    await Context.Channel.SendMessageAsync($"HTTP Download error in embed ({link}) and url <{url}>: " + ex.Message.ToString(), false);
                                }
                                catch (Exception ex)
                                {
                                    errors++;
                                    await Context.Channel.SendMessageAsync($"Download error in embed ({link}) and url <{url}>: " + ex.Message.ToString(), false);
                                }
                            }
                            else
                            {
                                await Context.Channel.SendMessageAsync($"Embed type {embed.Type} not supported link: {link}", false);
                                skipped++;
                            }
                        }
                    }
                }

                int totalCalls = downloaded + skipped + errors + noAttachments + threadCreated + tenorGifs + youtubeVideos + imgur;

                await Context.Channel.SendMessageAsync($"Total: {totalCalls} | Downloaded: {downloaded} | Skipped: {skipped} | Errors: {errors} | NoAttachments: {noAttachments} | ThreadCreated: {threadCreated} " +
                    $"| TenorGifs: {tenorGifs} | YoutubeVideos: {youtubeVideos} | Imgur: {imgur}", false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message.ToString(), false);
                await Context.Channel.SendMessageAsync(ex.StackTrace.ToString(), false);
            }

            await Context.Channel.SendMessageAsync("Done", false);
        }

        [Command("image")]
        public async Task ImageTest()
        {
            try
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                GoogleEngine google = new GoogleEngine();
                var results = await google.GetSearchResultBySelenium("ETH Zürich", 0, "de");
                if (results != null && results.Count > 0)
                    await Context.Channel.SendMessageAsync(string.Join(Environment.NewLine, results));
                else
                    await Context.Channel.SendMessageAsync("No results found", false);

            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString(), false);
            }
        }

        [Command("ampel")]
        public async Task CheckVisAmpel()
        {
            try
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                DiscordHelper.CheckVISAmpel();

                await Context.Channel.SendMessageAsync("Done", false);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString(), false);
            }
        }

        [Command("assign")]
        public async Task Test()
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }
            try
            {
                var allUsers = await Context.Guild.GetUsersAsync().FlattenAsync();
                await Context.Channel.SendMessageAsync("users " + allUsers.Count().ToString(), false);


                foreach (SocketGuildUser user in allUsers)
                {
                    if (user.Status == UserStatus.Online || user.Status == UserStatus.Idle || user.Status == UserStatus.AFK || user.Status == UserStatus.DoNotDisturb)
                    {
                        await Context.Channel.SendMessageAsync("Setting " + user.Username, false);

                        ulong roleId = 0;
                        switch (user.Id % 7)
                        {
                            case 0:
                                roleId = 1089996311522201715;
                                break;
                            case 1:
                                roleId = 1089996425091371128;
                                break;
                            case 2:
                                roleId = 1089996512701984789;
                                break;
                            case 3:
                                roleId = 1089996620625612921;
                                break;
                            case 4:
                                roleId = 1089996706009063424;
                                break;
                            case 5:
                                roleId = 1089996740654006412;
                                break;
                            case 6:
                                roleId = 1089996797780447282;
                                break;
                            default:
                                continue;
                        }

                        await user.AddRoleAsync(roleId);
                    }
                }
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message, false);
            }
        }

        //[RequireOwner]
        [Command("help")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task AdminHelp()
        {
            //var author = Context.Message.Author;
            //if (author.Id != Program.ApplicationSetting.Owner)
            //{
            //    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
            //    return;
            //}

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Admin Help (Admin only)");

            builder.WithColor(0, 0, 255);

            builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

            builder.WithCurrentTimestamp();
            builder.AddField("admin help", "This message :)");
            builder.AddField("admin channel help", "Help for channel command");
            builder.AddField("admin reddit help", "Help for reddit command");
            builder.AddField("admin rant help", "Help for rant command");
            builder.AddField("admin place help", "Help for place command");
            builder.AddField("admin keyval help", "Help for KeyValue DB Management");
            builder.AddField("admin kill", "Do I really need to explain this one");
            builder.AddField("admin cronjob <name>", "Manually start a CronJob");
            builder.AddField("admin blockemote <id> <block>", "Block an emote from being selectable");
            builder.AddField("admin events", "Sync VIS Events");

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("kill")]
        public async Task AdminKill()
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            await Context.Channel.SendMessageAsync("I'll be back!", false);
            Process.GetCurrentProcess().Kill();
        }


        [Command("reboot")]
        public async Task AdminReboot()
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            await Context.Channel.SendMessageAsync("Rebooting...", false);

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = "sudo shutdown -r now", };
                Process proc = new Process() { StartInfo = startInfo, };
                proc.Start();
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message, false);
            }

        }

        private Type[] GetTypesInNamespace(Assembly assembly, string nameSpace)
        {
            return
              assembly.GetTypes()
                      .Where(t => string.Equals(t.Namespace, nameSpace, StringComparison.Ordinal))
                      .ToArray();
        }

        [Command("events")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SyncVisEvents()
        {
            // TODO Move constants to config
            await DiscordHelper.SyncVisEvents(
                (Context.Channel as SocketGuildChannel).Guild.Id,
                747768907992924192,
                819864331192631346);
        }

        [Command("cronjob")]
        public async Task ManualCronJob(string cronJobName)
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            // TODO find a way to call them dynamically

            /*
            string baseNamespaceCronJobs = "ETHDINFKBot.CronJobs.Jobs";
            Type[] typelist = GetTypesInNamespace(Assembly.GetExecutingAssembly(), baseNamespaceCronJobs);

            Type type = Type.GetType(baseNamespaceCronJobs + "." + "DailyCleanup"); //target type

            MethodInfo info = type.GetMethod("StartAsync");

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            try
            {
                if (info.IsStatic)
                    info.Invoke(null, new object[] { token });
                else
                    info.Invoke(type, new object[] { token });
            }
            catch (Exception ex)
            {

            }*/
            try
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken token = cts.Token;
                switch (cronJobName)
                {
                    case "DailyCleanup":
                        var config = new ScheduleConfig<DailyCleanup>();
                        config.TimeZoneInfo = TimeZoneInfo.Local;
                        config.CronExpression = "* * * * *";

                        var logger = Program.Logger.CreateLogger<DailyCleanup>();

                        var job = new DailyCleanup(config, logger);
                        await job.StartAsync(token);
                        break;

                    /* TODO Add more jobs if needed*/
                    default:
                        await Context.Channel.SendMessageAsync("Only available: DailyCleanup", false);
                        break;
                }

                cts.CancelAfter(TimeSpan.FromSeconds(30));
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("Error: " + ex.ToString(), false);
            }
        }

        [Command("blockemote")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task BlockEmote(ulong emoteId, bool blockStatus)
        {
            var author = Context.Message.Author;
            //if (author.Id != Program.ApplicationSetting.Owner)
            //{
            //await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
            //return;
            //}

            var emoteInfo = DatabaseManager.EmoteDatabaseManager.GetDiscordEmoteById(emoteId);
            bool success = DatabaseManager.EmoteDatabaseManager.SetEmoteBlockStatus(emoteId, blockStatus);

            if (success)
            {
                // Also locally delete the file
                if (File.Exists(emoteInfo.LocalPath))
                    File.Delete(emoteInfo.LocalPath); // TODO Redownload if the emote is unblocked

                await Context.Channel.SendMessageAsync($"Successfully set block status of emote {emoteId} to: {blockStatus}", false);
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Failed to set block status of emote {emoteId}", false);
            }
        }


        [Command("notrack")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task NoTrackUser(ulong discordUserId)
        {
            DatabaseManager.Instance().AddNoTrackUser(discordUserId);
            await Context.Channel.SendMessageAsync($"Added user {discordUserId} to no track list", false);
        }


        [Command("ban")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task BanUser(ulong discordUserId)
        {
            var user = Program.Client.GetUser(discordUserId);

            if (user == null)
            {
                await Context.Channel.SendMessageAsync($"user is null", false);
                return;
            }

            // get direct messages with that user
            var dmChannel = await user.CreateDMChannelAsync();

            if (dmChannel == null)
            {
                await Context.Channel.SendMessageAsync($"dmChannel is null", false);
                return;
            }

            // delete all messages send by bot
            var messages = await dmChannel.GetMessagesAsync(1000).FlattenAsync();
            await Context.Channel.SendMessageAsync($"Found {messages.Count()} messages to delete", false);

            foreach (var message in messages)
            {
                if (message.Author.Id == Program.Client.CurrentUser.Id)
                {
                    await message.DeleteAsync();
                }
            }

            DatabaseManager.Instance().AddBannedUser(discordUserId);
            await Context.Channel.SendMessageAsync($"Banned user {discordUserId}", false);
        }

        class DiscordUserDump
        {
            public ulong DiscordUserId { get; set; }
            public string DiscordUserName { get; set; }
            public string AvatarUrl { get; set; }
            public bool IsBot { get; set; }
        }

        [Command("userupdate")]
        public async Task UserUpdate()
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            var allUsers = DatabaseManager.Instance().GetDiscordUsers();

            foreach (var user in allUsers)
            {
                try
                {
                    // Only users without pfp will be force updates
                    if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
                        continue;

                    var discordUser = Program.Client.GetUser(user.DiscordUserId);

                    DatabaseManager.Instance().UpdateDiscordUser(new DiscordUser()
                    {
                        DiscordUserId = user.DiscordUserId,
                        DiscriminatorValue = user.DiscriminatorValue,
                        AvatarUrl = discordUser.GetAvatarUrl() ?? discordUser.GetDefaultAvatarUrl(), // If user has no custom avatar load the url for the default avatar
                        IsBot = user.IsBot,
                        IsWebhook = user.IsWebhook,
                        Nickname = user.Nickname,
                        Username = user.Username,
                        JoinedAt = user.JoinedAt,
                        FirstAfternoonPostCount = user.FirstAfternoonPostCount
                    });

                    await Context.Message.Channel.SendMessageAsync($"Updated {user.Nickname ?? user.Username}");
                }
                catch (Exception ex)
                {
                    // Ignore
                }
            }

            await Context.Message.Channel.SendMessageAsync($"Done");

        }


        [Command("userdump")]
        public async Task UserDump()
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }


            var allUsers = DatabaseManager.Instance().GetDiscordUsers();

            List<DiscordUserDump> discordUsersList = new List<DiscordUserDump>();

            foreach (var user in allUsers)
            {
                if (string.IsNullOrWhiteSpace(user.AvatarUrl))
                    continue;

                discordUsersList.Add(new DiscordUserDump()
                {
                    DiscordUserId = user.DiscordUserId,
                    DiscordUserName = user.Nickname ?? user.Username,
                    AvatarUrl = user.AvatarUrl,
                    IsBot = user.IsBot
                });
            }

            var json = JsonConvert.SerializeObject(discordUsersList, Formatting.Indented);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await Context.Channel.SendFileAsync(stream, "DiscordUsersList.json", "DiscordUsers");
        }


        [Command("emoterestore")]
        public async Task EmoteRestore()
        {
            int count = 0;
            int downloaded = 0;

            try
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                var emotes = DatabaseManager.EmoteDatabaseManager.GetEmotes(null, false);

                await Context.Channel.SendMessageAsync($"Found {emotes.Count} emotes", false);


                foreach (var emote in emotes)
                {
                    count++;

                    // check if emote LocalPath exists
                    if (File.Exists(emote.LocalPath))
                        continue;

                    downloaded++;

                    // if not download it
                    var emoteUrl = emote.Url;
                    var emoteName = emote.EmoteName;

                    using (var webClient = new WebClient())
                    {
                        byte[] bytes = webClient.DownloadData(emote.Url);
                        string filePath = EmoteDBManager.MoveEmoteToDisk(emote, bytes);

                        // if paths differ then log to chat
                        if (filePath != emote.LocalPath)
                        {
                            await Context.Channel.SendMessageAsync($"Emote {emoteName} was moved from {emote.LocalPath} to {filePath}", false);

                            using (var context = new ETHBotDBContext())
                            {
                                var emoteDb = context.DiscordEmotes.Where(i => i.DiscordEmoteId == emote.DiscordEmoteId).Single();
                                emoteDb.LocalPath = filePath;
                                context.SaveChanges();
                            }
                        }
                    }


                    if (count % 1000 == 0)
                        await Context.Channel.SendMessageAsync($"Done {count} emotes and downloaded {downloaded} emotes", false);
                }
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString(), false);
                await Context.Channel.SendMessageAsync($"Done {count} emotes and downloaded {downloaded} emotes before error", false);
            }

            await Context.Channel.SendMessageAsync($"Done {count} emotes and downloaded {downloaded} emotes", false);
        }

        [Command("journal")]
        public async Task GenerateJournalLog(int days = 1)
        {
            try
            {
                // if os isnt linux then return
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    await Context.Channel.SendMessageAsync("This command only works on linux", false);
                    return;
                }

                var author = Context.Message.Author;

                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                string since = $"--since \\\"{days} days ago\\\""; // todo does 24+ work?

                string command = $"journalctl {since} --no-pager --output=short-precise --unit=ETHBot.service";

                string tempFilePath = Path.Combine(Program.ApplicationSetting.BasePath, "Data", "temp", "journal.log");

                // if temp file exists clear it
                if (File.Exists(tempFilePath))
                    File.WriteAllText(tempFilePath, "");
                // check if folder exists
                if (!Directory.Exists(Path.GetDirectoryName(tempFilePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath));

                // run command where we pipe into a file
                string finalCommand = $"{command} > {tempFilePath}";

                ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = $"-c \"{finalCommand}\"", };
                Console.WriteLine($"Running command: {finalCommand}");
                Process proc = new Process() { StartInfo = startInfo, };
                proc.Start();

                // wait for process to finish
                proc.WaitForExit();

                // send file to discord
                await Context.Channel.SendFileAsync(tempFilePath, $"Journal log for {days} days");
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString(), false);
            }
        }

        [Command("emotedump")]
        public async Task EmoteDump()
        {
            var author = Context.Message.Author;
            if (author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            var allEmotes = DatabaseManager.EmoteDatabaseManager.GetEmotes().OrderBy(i => i.DiscordEmoteId).ToList(); // sort it to ensure they are chronologically in there

            await Context.Channel.SendMessageAsync($"Successfully retrieved {allEmotes.Count} emotes", false);

            var emotesPath = Path.Combine(Program.ApplicationSetting.BasePath, "Emotes");
            var archivePath = Path.Combine(emotesPath, "Archive");

            try
            {

                // If the directory exists clean it up
                if (Directory.Exists(archivePath))
                    Directory.Delete(archivePath, true);

                // Create dir
                Directory.CreateDirectory(archivePath);

                List<EmoteInfo> emoteInfos = new List<EmoteInfo>();

                foreach (var emote in allEmotes)
                {
                    var folder = GetEmoteFolder(emote.LocalPath);
                    emoteInfos.Add(new EmoteInfo()
                    {
                        Id = emote.DiscordEmoteId,
                        Name = emote.EmoteName,
                        Animated = emote.Animated,
                        CreatedAt = emote.CreatedAt,
                        Url = emote.Url,
                        Folder = folder
                    });
                }

                //var emoteFolders = Directory.GetDirectories(emotesPath);

                //foreach (var emoteFolder in emoteFolders.ToList().OrderBy(i => i))
                //{
                //// Needs to contain - else its not an active folder
                //if (emoteFolder.Contains("-"))
                //{
                //string tarGZFile = $"{new DirectoryInfo(emoteFolder).Name}.tar.gz";
                //CreateTarGZ(Path.Combine(archivePath, tarGZFile), emoteFolder);
                //}
                //}

                //var archiveFiles = Directory.GetFiles(archivePath);
                //await Context.Channel.SendMessageAsync($"Created {archiveFiles.Length} archives", false);

                // Send file infos

                var json = JsonConvert.SerializeObject(emoteInfos, Formatting.Indented);
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                await Context.Channel.SendFileAsync(stream, "EmoteInfo.json", "Emote Infos");

                //foreach (var archiveFile in archiveFiles.ToList().OrderBy(i => i))
                //    await Context.Channel.SendFileAsync(archiveFile, new DirectoryInfo(archiveFile).Name);

                // In the end clean up the archive folder again
                //if (Directory.Exists(archivePath))
                //    Directory.Delete(archivePath, true);

                await Context.Channel.SendMessageAsync($"Done", false);

            }
            catch (Exception ex)
            {
                string error = $"Error: {ex.ToString()}";
                await Context.Channel.SendMessageAsync(error.Substring(0, Math.Min(2000, error.Length)), false);
            }
        }

        private string GetEmoteFolder(string path)
        {
            return new DirectoryInfo(path).Parent.Name;
        }

        //  https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples#user-content--create-a-tgz-targz
        private void CreateTarGZ(string tgzFilename, string sourceDirectory)
        {
            Stream outStream = File.Create(tgzFilename);
            Stream gzoStream = new GZipOutputStream(outStream);
            TarArchive tarArchive = TarArchive.CreateOutputTarArchive(gzoStream);

            tarArchive.RootPath = sourceDirectory.Replace('\\', '/');
            if (tarArchive.RootPath.EndsWith("/"))
                tarArchive.RootPath = tarArchive.RootPath.Remove(tarArchive.RootPath.Length - 1);

            AddDirectoryFilesToTar(tarArchive, sourceDirectory, true);

            tarArchive.Close();
        }

        private void AddDirectoryFilesToTar(TarArchive tarArchive, string sourceDirectory, bool recurse)
        {
            // Optionally, write an entry for the directory itself.
            // Specify false for recursion here if we will add the directory's files individually.
            TarEntry tarEntry = TarEntry.CreateEntryFromFile(sourceDirectory);
            tarArchive.WriteEntry(tarEntry, false);

            // Write each file to the tar.
            string[] filenames = Directory.GetFiles(sourceDirectory);
            foreach (string filename in filenames)
            {
                tarEntry = TarEntry.CreateEntryFromFile(filename);
                tarArchive.WriteEntry(tarEntry, true);
            }

            if (recurse)
            {
                string[] directories = Directory.GetDirectories(sourceDirectory);
                foreach (string directory in directories)
                    AddDirectoryFilesToTar(tarArchive, directory, recurse);
            }
        }

        [Group("food")]
        public class FoodAdminModule : ModuleBase<SocketCommandContext>
        {
            private static FoodDBManager FoodDBManager = FoodDBManager.Instance();

            [Command("help")]
            public async Task AdminHelp()
            {
                var author = Context.Message.Author;
                var guildUser = Context.Message.Author as SocketGuildUser;
                if (!(author.Id == ETHDINFKBot.Program.ApplicationSetting.Owner || guildUser.GuildPermissions.ManageChannels))
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Admin Help (Admin only)");

                builder.WithColor(0, 0, 255);
                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                builder.WithCurrentTimestamp();
                builder.AddField($"{Program.CurrentPrefix}admin food help", "This message :)");
                builder.AddField($"{Program.CurrentPrefix}admin food setup", "Sets Default values for Tables Restaurant and Allergies");
                builder.AddField($"{Program.CurrentPrefix}admin food clear <id>", "Clears today menus");
                builder.AddField($"{Program.CurrentPrefix}admin food load <id>", "Loads todays menus");
                builder.AddField($"{Program.CurrentPrefix}admin food image <restaurant|menu> <id> <full>", "Runs a websearch to replace images");
                //builder.AddField($"{Program.CurrentPrefix}admin food menuimage <menu_id>", "Returns all images found for this menu");
                builder.AddField($"{Program.CurrentPrefix}admin food status <debug>", "Returns current menus status");
                builder.AddField($"{Program.CurrentPrefix}admin food fix", "Fixes today menus");
                builder.AddField($"{Program.CurrentPrefix}admin food 2050mensas <dryRun>", "Loads all mensas from food2050 and add missing ones");
                builder.AddField($"{Program.CurrentPrefix}admin food ethmensas <dryRun>", "Loads all mensas from eth page and add missing ones");
                builder.AddField($"{Program.CurrentPrefix}admin food setlocation <locationid> <restaurantids>", "Sets the location for a restaurants (comma seperated)");

                builder.AddField($"{Program.CurrentPrefix}admin food broken <days>", "Lists all restaurants with no menus for the last X days or more");
                builder.AddField($"{Program.CurrentPrefix}admin food similar", "Lists all similar restaurants");

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            // TODO move this somewhere else or create insert script to check if all inserted
            [Command("setup")]
            public async Task SetupFood()
            {
                try
                {
                    var author = Context.Message.Author;
                    if (author.Id != Program.ApplicationSetting.Owner)
                    {
                        await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                        return;
                    }

                    var foodDBManager = FoodDBManager.Instance();

                    var sqlFilePath = Path.Combine(Program.ApplicationSetting.BasePath, "Data", "SQL", "RestaurantBaseSetup.sql");
                    string sqlFileContent = File.ReadAllText(sqlFilePath);

                    using (var connection = new MySqlConnection(Program.ApplicationSetting.ConnectionStringsSetting.ConnectionString_Full))
                    {
                        using (var command = new MySqlCommand(sqlFileContent, connection))
                        {
                            try
                            {
                                command.CommandTimeout = 5;

                                connection.Open();

                                int rowsAffected = command.ExecuteNonQuery();
                                await Context.Channel.SendMessageAsync($"Affected {rowsAffected} row(s)", false);
                            }
                            catch (Exception ex)
                            {
                                await Context.Message.Channel.SendMessageAsync(ex.ToString());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Context.Message.Channel.SendMessageAsync(ex.ToString());
                }
            }

            [Command("broken")]
            [RequireOwner]
            public async Task FindBrokenMensas(int days = 14)
            {
                try
                {
                    // list restaurants with no menus for the last 7 days or more
                    var allRestaurants = FoodDBManager.GetAllRestaurants();

                    List<Restaurant> workingRestaurants = new List<Restaurant>();

                    for (int i = 0; i < days; i++)
                    {
                        foreach (var restaurant in allRestaurants)
                        {
                            // if the current day -i is not a weekday skip
                            var day = DateTime.Now.AddDays(-i);
                            if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
                                continue;

                            var allMenus = FoodDBManager.GetMenusFromRestaurant(restaurant.RestaurantId, day);

                            if (allMenus.Count > 0)
                            {
                                if (!workingRestaurants.Contains(restaurant))
                                    workingRestaurants.Add(restaurant);
                            }
                        }
                    }

                    var brokenRestaurants = allRestaurants.Where(i => !workingRestaurants.Contains(i)).ToList();

                    await Context.Channel.SendMessageAsync($"Broken restaurants: {brokenRestaurants.Count}", false);

                    // list broken restaurants
                    List<string> brokenRestaurantInfo = new List<string>();

                    foreach (var restaurant in brokenRestaurants)
                    {
                        brokenRestaurantInfo.Add($"{restaurant.RestaurantId} - {restaurant.InternalName} - {restaurant.AdditionalInternalName} - {restaurant.TimeParameter}");
                    }

                    // print text but break lines when it would exceed 1990 chars
                    string outputString = "";
                    foreach (var info in brokenRestaurantInfo)
                    {
                        if (outputString.Length + info.Length > 1990)
                        {
                            await Context.Channel.SendMessageAsync(outputString, false);
                            outputString = "";
                        }

                        outputString += info + Environment.NewLine;
                    }

                    if (!string.IsNullOrWhiteSpace(outputString))
                        await Context.Channel.SendMessageAsync(outputString, false);
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.ToString(), false);
                }
            }

            // https://gist.github.com/Davidblkx/e12ab0bb2aff7fd8072632b396538560
            public static class LevenshteinDistance
            {
                /// <summary>
                ///     Calculate the difference between 2 strings using the Levenshtein distance algorithm
                /// </summary>
                /// <param name="source1">First string</param>
                /// <param name="source2">Second string</param>
                /// <returns></returns>
                public static int Calculate(string source1, string source2) //O(n*m)
                {
                    var source1Length = source1.Length;
                    var source2Length = source2.Length;

                    var matrix = new int[source1Length + 1, source2Length + 1];

                    // First calculation, if one entry is empty return full length
                    if (source1Length == 0)
                        return source2Length;

                    if (source2Length == 0)
                        return source1Length;

                    // Initialization of matrix with row size source1Length and columns size source2Length
                    for (var i = 0; i <= source1Length; matrix[i, 0] = i++) { }
                    for (var j = 0; j <= source2Length; matrix[0, j] = j++) { }

                    // Calculate rows and collumns distances
                    for (var i = 1; i <= source1Length; i++)
                    {
                        for (var j = 1; j <= source2Length; j++)
                        {
                            var cost = (source2[j - 1] == source1[i - 1]) ? 0 : 1;

                            matrix[i, j] = Math.Min(
                                Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                                matrix[i - 1, j - 1] + cost);
                        }
                    }
                    // return result
                    return matrix[source1Length, source2Length];
                }
            }

            [Command("similar")]
            [RequireOwner]
            public async Task FindSimilarMensas()
            {
                var allRestaurants = FoodDBManager.GetAllRestaurants();

                // do minimal edit distance to similar names
                foreach (var restaurant in allRestaurants)
                {
                    var currentName = $"{restaurant.InternalName}-{restaurant.AdditionalInternalName}-{restaurant.TimeParameter}";

                    foreach (var otherRestaurant in allRestaurants)
                    {
                        if (restaurant.RestaurantId == otherRestaurant.RestaurantId)
                            continue;

                        // only food2050
                        if (!restaurant.IsFood2050Supported || !otherRestaurant.IsFood2050Supported)
                            continue;

                        var otherName = $"{otherRestaurant.InternalName}-{otherRestaurant.AdditionalInternalName}-{otherRestaurant.TimeParameter}";

                        int distance = LevenshteinDistance.Calculate(currentName, otherName);
                        if (distance < 5)
                        {
                            await Context.Channel.SendMessageAsync($"Similar with distance {distance}: {restaurant.RestaurantId} ({currentName}) and {otherRestaurant.RestaurantId} ({otherName})", false);
                        }
                    }
                }
            }

            [Command("clear")]
            public async Task ClearFood(int restaurantId = -1)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                //var allRestaurants = FoodDBManager.GetAllRestaurants();

                var allMenus = FoodDBManager.GetMenusByDay(DateTime.Now, restaurantId);
                if (allMenus.Count > 0)
                    await Context.Channel.SendMessageAsync($"Deleting {allMenus.Count} menu(s)", false);

                foreach (var menu in allMenus)
                    FoodDBManager.DeleteMenu(menu);

                await Context.Channel.SendMessageAsync($"Done clear for: {restaurantId}", false);
            }

            // TODO allow load mode for all restaurants with no menus today
            [Command("fix", RunMode = RunMode.Async)]
            public async Task FixFood()
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                var allRestaurants = FoodDBManager.GetAllRestaurants();
                var foodHelper = new FoodHelper();

                foreach (var restaurant in allRestaurants)
                {
                    if (!restaurant.IsOpen)
                        continue;

                    var allMenus = FoodDBManager.GetMenusByDay(DateTime.Now, restaurant.RestaurantId);
                    if (allMenus.Count == 0)
                    {
                        await ClearFood(restaurant.RestaurantId); // Ensure but likely empty anyway
                        foodHelper.LoadMenus(restaurant.RestaurantId);

                        await Context.Channel.SendMessageAsync($"Done load for: {restaurant.RestaurantId}", false);
                    }
                }
            }

            // TODO allow load mode for all restaurants with no menus today
            [Command("load", RunMode = RunMode.Async)]
            public async Task LoadFood(int restaurantId = -1)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                var foodHelper = new FoodHelper();
                //await ClearFood(restaurantId); // Ensure deleted -> atm there is no need to delete as we just update records
                foodHelper.LoadMenus(restaurantId);

                await Context.Channel.SendMessageAsync($"Done load for: {restaurantId}", false);
            }

            [Command("image", RunMode = RunMode.Async)]
            public async Task LoadImage(string key, int id, bool full = true)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                List<Menu> menus = new List<Menu>();

                if (key == "restaurant")
                {
                    await Context.Channel.SendMessageAsync($"Loading images for RestaurantId: {id}", false);
                    menus = FoodDBManager.GetMenusByDay(DateTime.Now, id);
                }
                else if (key == "menu")
                {
                    await Context.Channel.SendMessageAsync($"Loading images for MenuId: {id}", false);
                    var menu = FoodDBManager.GetMenusById(id);
                    menus.Add(menu);
                }
                else
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                }

                var foodHelper = new FoodHelper();
                try
                {
                    foreach (var menu in menus)
                    {
                        if (menu.MenuImageId.HasValue)
                            continue; // We dont need to research this image

                        var menuImage = foodHelper.GetImageForFood(menu, true);
                        await Context.Channel.SendMessageAsync($"Got ImageId: {menuImage?.MenuImageId ?? -1} for Menu: {menu.MenuId}", false);

                        if (menuImage != null)
                            FoodDBManager.SetImageIdForMenu(menu.MenuId, menuImage.MenuImageId);
                    }
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.ToString(), false);
                }

                //await ClearFood(restaurantId); // Ensure deleted
                //FoodHelper.LoadMenus(restaurantId);

                await Context.Channel.SendMessageAsync("Done load", false);
            }

            [Command("status", RunMode = RunMode.Async)]
            public async Task StatusFood(bool debug = true)
            {
                var author = Context.Message.Author;
                var guildUser = Context.Message.Author as SocketGuildUser;
                if (!(author.Id == ETHDINFKBot.Program.ApplicationSetting.Owner || guildUser.GuildPermissions.ManageChannels))
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Food status");

                builder.WithColor(0, 0, 255);
                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                builder.WithCurrentTimestamp();
                builder.WithAuthor(author);

                var allRestaurants = FoodDBManager.GetAllRestaurants();
                var allTodaysMenus = FoodDBManager.GetMenusByDay(DateTime.Now);

                builder.WithDescription(@$"Total restaurants: {allRestaurants.Count}
Total todays menus: {allTodaysMenus.Count}");

                foreach (var restaurant in allRestaurants)
                {
                    var todaysMenus = FoodDBManager.GetMenusFromRestaurant(restaurant.RestaurantId, DateTime.Now);

                    if (!debug)
                        builder.AddField(restaurant.Name, $"{todaysMenus.Count()} menu(s)" + Environment.NewLine + String.Join(", ", todaysMenus.Select(i => $"{i.Name} **{i.Amount} CHF**")));
                    else
                        builder.AddField($"{restaurant.Name} ({restaurant.RestaurantId})", $"{todaysMenus.Count()} menu(s)" + Environment.NewLine + String.Join(", ", todaysMenus.Select(i => $"{i.Name} **{i.Amount} CHF ({i.MenuId}/{i.MenuImageId ?? -1})**")));
                }

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("insert2050mensa")]
            public async Task Insert2050Mensa(string link)
            {
                if (link.EndsWith("/"))
                    link = link.Substring(0, link.Length - 1);

                var locationName = link.Split('/').Last();

                var author = Context.Message.Author;
                var guildUser = Context.Message.Author as SocketGuildUser;
                if (!(author.Id == ETHDINFKBot.Program.ApplicationSetting.Owner || guildUser.GuildPermissions.ManageChannels))
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }


                var allRestaurants = FoodDBManager.GetAllRestaurants();



                try
                {
                    int totalAdded = 0;
                    string mainUrl = "https://app.food2050.ch/";

                    var client = new HttpClient();

                    var htmlMainPage = client.GetStringAsync(mainUrl);

                    var doc = new HtmlDocument();

                    doc.LoadHtml(htmlMainPage.Result);

                    // get script tag with json with the id __NEXT_DATA__
                    var scriptTag = doc.DocumentNode.Descendants("script").Where(i => i.Id == "__NEXT_DATA__").FirstOrDefault();

                    if (scriptTag == null)
                    {
                        await Context.Channel.SendMessageAsync("Could not find script tag in main", false);
                        return;
                    }

                    var json = scriptTag.InnerText;

                    var appResponse = JsonConvert.DeserializeObject<Food2050MainAppResponse>(json);

                    // loop trough all restaurants
                    var locations = appResponse.props.pageProps.locations;
                    string output = "Found: " + locations.Count() + " restaurants" + Environment.NewLine;
                    foreach (var location in locations)
                    {
                        if (location.slug != locationName)
                            continue; // skip this one

                        await Context.Channel.SendMessageAsync($"Found location: {location.title}", false);

                        // location title remove number at the start of the string divided by a space
                        var parts = location.title.Split(' ');

                        // if the first part is just numbers remove it
                        if (parts.First().All(Char.IsDigit))
                        {
                            parts = parts.Skip(1).ToArray();
                            location.title = string.Join(" ", parts);
                        }


                        output += $"Slug: {location.slug} Title: {location.title}" + Environment.NewLine;


                        // for each location make http call url/{location.slug}
                        string locationUrl = mainUrl + location.slug + "/";

                        var htmlLocationPage = "";

                        try
                        {
                            htmlLocationPage = await client.GetStringAsync(locationUrl);
                        }
                        catch (Exception ex)
                        {
                            await Context.Channel.SendMessageAsync($"Could not get {locationUrl}", false);
                            continue;
                        }

                        var locationDoc = new HtmlDocument();

                        locationDoc.LoadHtml(htmlLocationPage);

                        // get script tag with json with the id __NEXT_DATA__

                        var locationScriptTag = locationDoc.DocumentNode.Descendants("script").Where(i => i.Id == "__NEXT_DATA__").FirstOrDefault();

                        if (locationScriptTag == null)
                        {
                            await Context.Channel.SendMessageAsync($"Could not find script tag for {locationUrl}", false);
                            return;
                        }

                        var locationJson = locationScriptTag.InnerText;

                        var locationAppResponse = JsonConvert.DeserializeObject<Food2050MainAppLocationResponse>(locationJson);

                        var kitchens = locationAppResponse.props.pageProps.location.outlets;

                        foreach (var kitchen in kitchens)
                        {
                            var kitchenSlug = kitchen.slug;

                            string kitchenUrl = mainUrl + location.slug + "/" + kitchenSlug + "/";

                            var htmlKitchenPage = "";

                            try
                            {
                                htmlKitchenPage = await client.GetStringAsync(kitchenUrl);
                            }
                            catch (Exception ex)
                            {
                                await Context.Channel.SendMessageAsync($"Could not get {kitchenUrl}", false);
                                continue;
                            }

                            var kitchenDoc = new HtmlDocument();

                            kitchenDoc.LoadHtml(htmlKitchenPage);

                            // get script tag with json with the id __NEXT_DATA__

                            var kitchenScriptTag = kitchenDoc.DocumentNode.Descendants("script").Where(i => i.Id == "__NEXT_DATA__").FirstOrDefault();

                            if (kitchenScriptTag == null)
                            {
                                await Context.Channel.SendMessageAsync($"Could not find script tag for {kitchenUrl}", false);
                                return;
                            }

                            var kitchenJson = kitchenScriptTag.InnerText;

                            var kitchenAppResponse = JsonConvert.DeserializeObject<Food2050MainAppKitchenResponse>(kitchenJson);

                            var digitalMenus = kitchenAppResponse.props.pageProps.location.kitchen.digitalMenus;

                            foreach (var digitalMenu in digitalMenus)
                            {
                                var menuSlug = digitalMenu.slug;


                                // get all db restaurants (we call all here always to ensure no duplicates)


                                var dbRestaurant = allRestaurants.Where(i =>
                                    i.InternalName == location.slug
                                    && i.AdditionalInternalName == kitchenSlug
                                    && i.TimeParameter == menuSlug).FirstOrDefault();

                                if (dbRestaurant == null)
                                {
                                    //output += $"  LocationSlug: {location.slug} KitchenSlug: {kitchenSlug} MenuSlug: {menuSlug}" + Environment.NewLine;

                                    string menuName = "";

                                    string menuLabel = digitalMenu.label ?? "";

                                    // translate to english
                                    if (menuLabel.ToLower().Contains("mittag"))
                                        menuLabel = "Lunch";
                                    else if (menuLabel.ToLower().Contains("abend"))
                                        menuLabel = "Dinner";

                                    if (!string.IsNullOrWhiteSpace(menuSlug) && !string.IsNullOrWhiteSpace(menuLabel))
                                        menuName = $"({menuLabel.Trim()})";

                                    RestaurantLocation restaurantLocation = RestaurantLocation.Other;

                                    string name = $"{location.title.Trim()} {menuName}";
                                    string fullName = $"{location.title} {kitchen.publicLabel}";

                                    // could lead to some false positives
                                    if (fullName.ToLower().Contains("eth"))
                                        restaurantLocation = RestaurantLocation.ETH_UZH_Zentrum;


                                    if (fullName.ToLower().Contains("irchel"))
                                        restaurantLocation = RestaurantLocation.UZH_Irchel_Oerlikon;

                                    // not all will be captured
                                    if (fullName.ToLower().Contains("hslu"))
                                        restaurantLocation = RestaurantLocation.HSLU;

                                    // some may be classified in zentrum but should be irchel/oerlikon
                                    if (fullName.ToLower().Contains("uzh"))
                                        restaurantLocation = RestaurantLocation.ETH_UZH_Zentrum;

                                    if (fullName.ToLower().Contains("zhaw"))
                                        restaurantLocation = RestaurantLocation.ZHAW;

                                    if (fullName.ToLower().Contains("ubs"))
                                        restaurantLocation = RestaurantLocation.UBS;

                                    if (fullName.ToLower().Contains("sbb") || fullName.ToLower().Contains("cff") || fullName.ToLower().Contains("ffs"))
                                        restaurantLocation = RestaurantLocation.SBB_CFF_FFS;

                                    // TODO maybe other to auto detect

                                    name = name.Trim();

                                    // create new restaurant
                                    dbRestaurant = new Restaurant()
                                    {
                                        InternalName = location.slug,
                                        AdditionalInternalName = kitchenSlug,
                                        TimeParameter = menuSlug,
                                        Name = name,
                                        IsOpen = true,
                                        OffersLunch = true,
                                        OffersDinner = false, // TODO this has to be done manually
                                        Location = restaurantLocation,
                                        IsFood2050Supported = true,
                                        ScraperTypeId = FoodScraperType.Food2050
                                    };


                                    output += $"  LocationSlug: {location.slug} KitchenSlug: {kitchenSlug} MenuSlug: {menuSlug} Name: {name} Location: {restaurantLocation}" + Environment.NewLine;

                                    string sqlInsert = $".sql query ```sql\nINSERT INTO `Restaurant` (`InternalName`, `AdditionalInternalName`, `TimeParameter`, `Name`, `IsOpen`, `OffersLunch`, `OffersDinner`, `Location`, `IsFood2050Supported`, `ScraperTypeId`) {Environment.NewLine}" +
                                    $"VALUES ('{dbRestaurant.InternalName}', '{dbRestaurant.AdditionalInternalName}', '{dbRestaurant.TimeParameter}', '{dbRestaurant.Name}', {Convert.ToInt32(dbRestaurant.IsOpen)}, {Convert.ToInt32(dbRestaurant.OffersLunch)}, {Convert.ToInt32(dbRestaurant.OffersDinner)}, {Convert.ToInt32(dbRestaurant.Location)}, {Convert.ToInt32(dbRestaurant.IsFood2050Supported)}, {Convert.ToInt32(dbRestaurant.ScraperTypeId)});\n```";

                                    await Context.Channel.SendMessageAsync($"{location.title} - {kitchen.name} - {digitalMenu.label} ```sql\n{sqlInsert}```", false);


                                    var dbRestaurantName = $"{dbRestaurant.InternalName}-{dbRestaurant.AdditionalInternalName}-{dbRestaurant.TimeParameter}";
                                    // do minimal edit distance to similar names
                                    foreach (var restaurant in allRestaurants)
                                    {
                                        var currentName = $"{restaurant.InternalName}-{restaurant.AdditionalInternalName}-{restaurant.TimeParameter}";

                                        int distance = LevenshteinDistance.Calculate(currentName, dbRestaurantName);
                                        if (distance < 5)
                                        {
                                            await Context.Channel.SendMessageAsync($"Similar with distance {distance}: {restaurant.RestaurantId} ({currentName}) and {dbRestaurant.Name})", false);
                                        }

                                    }
                                }
                                else
                                {
                                    output += $"  [SKIP] Found: {dbRestaurant.InternalName} {dbRestaurant.AdditionalInternalName} {dbRestaurant.TimeParameter}" + Environment.NewLine;
                                }
                            }
                        }
                    }

                    output += $"# Total added: {totalAdded}";

                    // send up to 2000 chars max per message, split by lines
                    var lines = output.Split(Environment.NewLine).ToList();
                    var messages = new List<string>();
                    string currentMessage = "";
                    foreach (var line in lines)
                    {
                        if (currentMessage.Length + line.Length > 2000)
                        {
                            messages.Add(currentMessage);
                            currentMessage = "";
                        }

                        currentMessage += line + Environment.NewLine;
                    }

                    messages.Add(currentMessage);

                    foreach (var message in messages)
                        await Context.Channel.SendMessageAsync(message, false);
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.ToString(), false);
                }
            }

            /*[Command("2050mensas", RunMode = RunMode.Async)]
            public async Task Mensas2050(bool dryRun = true)
            {
                var author = Context.Message.Author;
                var guildUser = Context.Message.Author as SocketGuildUser;
                if (!(author.Id == ETHDINFKBot.Program.ApplicationSetting.Owner || guildUser.GuildPermissions.ManageChannels))
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                try
                {
                    int totalAdded = 0;
                    string mainUrl = "https://app.food2050.ch/";

                    var client = new HttpClient();

                    var htmlMainPage = client.GetStringAsync(mainUrl);

                    var doc = new HtmlDocument();

                    doc.LoadHtml(htmlMainPage.Result);

                    // get script tag with json with the id __NEXT_DATA__
                    var scriptTag = doc.DocumentNode.Descendants("script").Where(i => i.Id == "__NEXT_DATA__").FirstOrDefault();

                    if (scriptTag == null)
                    {
                        await Context.Channel.SendMessageAsync("Could not find script tag in main", false);
                        return;
                    }

                    var json = scriptTag.InnerText;

                    var appResponse = JsonConvert.DeserializeObject<Food2050MainAppResponse>(json);

                    // loop trough all restaurants
                    var locations = appResponse.props.pageProps.locations;
                    string output = "Found: " + locations.Count() + " restaurants" + Environment.NewLine;
                    foreach (var location in locations)
                    {
                        // location title remove number at the start of the string divided by a space
                        var parts = location.title.Split(' ');

                        // if the first part is just numbers remove it
                        if (parts.First().All(Char.IsDigit))
                        {
                            parts = parts.Skip(1).ToArray();
                            location.title = string.Join(" ", parts);
                        }


                        output += $"Slug: {location.slug} Title: {location.title}" + Environment.NewLine;


                        // for each location make http call url/{location.slug}
                        string locationUrl = mainUrl + location.slug + "/";

                        var htmlLocationPage = "";

                        try
                        {
                            htmlLocationPage = await client.GetStringAsync(locationUrl);
                        }
                        catch (Exception ex)
                        {
                            await Context.Channel.SendMessageAsync($"Could not get {locationUrl}", false);
                            continue;
                        }

                        var locationDoc = new HtmlDocument();

                        locationDoc.LoadHtml(htmlLocationPage);

                        // get script tag with json with the id __NEXT_DATA__

                        var locationScriptTag = locationDoc.DocumentNode.Descendants("script").Where(i => i.Id == "__NEXT_DATA__").FirstOrDefault();

                        if (locationScriptTag == null)
                        {
                            await Context.Channel.SendMessageAsync($"Could not find script tag for {locationUrl}", false);
                            return;
                        }

                        var locationJson = locationScriptTag.InnerText;

                        var locationAppResponse = JsonConvert.DeserializeObject<Food2050MainAppLocationResponse>(locationJson);

                        var kitchens = locationAppResponse.props.pageProps.location.outlets;

                        foreach (var kitchen in kitchens)
                        {
                            var kitchenSlug = kitchen.slug;

                            string kitchenUrl = mainUrl + location.slug + "/" + kitchenSlug + "/";

                            var htmlKitchenPage = "";

                            try
                            {
                                htmlKitchenPage = await client.GetStringAsync(kitchenUrl);
                            }
                            catch (Exception ex)
                            {
                                await Context.Channel.SendMessageAsync($"Could not get {kitchenUrl}", false);
                                continue;
                            }

                            var kitchenDoc = new HtmlDocument();

                            kitchenDoc.LoadHtml(htmlKitchenPage);

                            // get script tag with json with the id __NEXT_DATA__

                            var kitchenScriptTag = kitchenDoc.DocumentNode.Descendants("script").Where(i => i.Id == "__NEXT_DATA__").FirstOrDefault();

                            if (kitchenScriptTag == null)
                            {
                                await Context.Channel.SendMessageAsync($"Could not find script tag for {kitchenUrl}", false);
                                return;
                            }

                            var kitchenJson = kitchenScriptTag.InnerText;

                            var kitchenAppResponse = JsonConvert.DeserializeObject<Food2050MainAppKitchenResponse>(kitchenJson);

                            var digitalMenus = kitchenAppResponse.props.pageProps.location.kitchen.digitalMenus;

                            foreach (var digitalMenu in digitalMenus)
                            {
                                var menuSlug = digitalMenu.slug;


                                // get all db restaurants (we call all here always to ensure no duplicates)

                                var allRestaurants = FoodDBManager.GetAllRestaurants();

                                var dbRestaurant = allRestaurants.Where(i =>
                                    i.InternalName == location.slug
                                    && i.AdditionalInternalName == kitchenSlug
                                    && i.TimeParameter == menuSlug).FirstOrDefault();

                                if (dbRestaurant == null)
                                {
                                    output += $"  LocationSlug: {location.slug} KitchenSlug: {kitchenSlug} MenuSlug: {menuSlug}" + Environment.NewLine;

                                    string menuName = "";

                                    string menuLabel = digitalMenu.label ?? "";

                                    // translate to english
                                    if (menuLabel.ToLower().Contains("mittag"))
                                        menuLabel = "Lunch";
                                    else if (menuLabel.ToLower().Contains("abend"))
                                        menuLabel = "Dinner";

                                    if (!string.IsNullOrWhiteSpace(menuSlug) && !string.IsNullOrWhiteSpace(menuLabel))
                                        menuName = $"({menuLabel.Trim()})";

                                    RestaurantLocation restaurantLocation = RestaurantLocation.Other;

                                    string name = $"{location.title.Trim()} {menuName}";
                                    string fullName = $"{location.title} {kitchen.publicLabel}";

                                    // could lead to some false positives
                                    if (fullName.ToLower().Contains("eth"))
                                        restaurantLocation = RestaurantLocation.ETH_UZH_Zentrum;


                                    if (fullName.ToLower().Contains("irchel"))
                                        restaurantLocation = RestaurantLocation.UZH_Irchel_Oerlikon;

                                    // not all will be captured
                                    if (fullName.ToLower().Contains("hslu"))
                                        restaurantLocation = RestaurantLocation.HSLU;

                                    // some may be classified in zentrum but should be irchel/oerlikon
                                    if (fullName.ToLower().Contains("uzh"))
                                        restaurantLocation = RestaurantLocation.ETH_UZH_Zentrum;

                                    if (fullName.ToLower().Contains("zhaw"))
                                        restaurantLocation = RestaurantLocation.ZHAW;

                                    if (fullName.ToLower().Contains("ubs"))
                                        restaurantLocation = RestaurantLocation.UBS;

                                    if (fullName.ToLower().Contains("sbb") || fullName.ToLower().Contains("cff") || fullName.ToLower().Contains("ffs"))
                                        restaurantLocation = RestaurantLocation.SBB_CFF_FFS;

                                    // TODO maybe other to auto detect

                                    // create new restaurant
                                    dbRestaurant = new Restaurant()
                                    {
                                        InternalName = location.slug,
                                        AdditionalInternalName = kitchenSlug,
                                        TimeParameter = menuSlug,
                                        Name = name,
                                        IsOpen = true,
                                        OffersLunch = true,
                                        OffersDinner = false, // TODO this has to be done manually
                                        Location = restaurantLocation,
                                        IsFood2050Supported = true,
                                        ScraperTypeId = FoodScraperType.Food2050
                                    };

                                    if (!dryRun)
                                    {
                                        bool success = FoodDBManager.AddRestaurant(dbRestaurant);

                                        if (success)
                                            totalAdded++;

                                        output += $"    Added new restaurant with name {name} to DB: {success} at the location: {restaurantLocation}" + Environment.NewLine;
                                    }
                                    else
                                    {
                                        output += $"    [DRY] Would have added new restaurant to DB at the location: {restaurantLocation}" + Environment.NewLine;
                                    }
                                }
                            }
                        }
                    }

                    output += $"# Total added: {totalAdded}";

                    // send up to 2000 chars max per message, split by lines
                    var lines = output.Split(Environment.NewLine).ToList();
                    var messages = new List<string>();
                    string currentMessage = "";
                    foreach (var line in lines)
                    {
                        if (currentMessage.Length + line.Length > 2000)
                        {
                            messages.Add(currentMessage);
                            currentMessage = "";
                        }

                        currentMessage += line + Environment.NewLine;
                    }

                    messages.Add(currentMessage);

                    foreach (var message in messages)
                        await Context.Channel.SendMessageAsync(message, false);
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.ToString(), false);
                }
            }*/

            // command to load ETHMensas
            [Command("ethmensas", RunMode = RunMode.Async)]
            public async Task LoadETHMensas(bool dryRun = true)
            {
                var author = Context.Message.Author;
                var guildUser = Context.Message.Author as SocketGuildUser;
                if (!(author.Id == ETHDINFKBot.Program.ApplicationSetting.Owner || guildUser.GuildPermissions.ManageChannels))
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                try
                {
                    string url = "https://idapps.ethz.ch/cookpit-pub-services/v1/facilities?client-id=ethz-wcms&lang=en&rs-first=0&rs-size=50";

                    var client = new HttpClient();

                    var response = await client.GetAsync(url);

                    var json = await response.Content.ReadAsStringAsync();

                    var ethMensas = JsonConvert.DeserializeObject<ETHMensaResponse>(json);

                    var restaurants = FoodDBManager.GetAllRestaurants();

                    string messageString = "";

                    foreach (var facility in ethMensas.facilityarray)
                    {
                        // check if restaurant exists with InternalParameter == facilify.facilityid
                        var dbRestaurant = restaurants.Where(i => i.InternalName == facility.facilityid.ToString()).FirstOrDefault();

                        if (dbRestaurant == null)
                        {
                            // create new restaurant
                            dbRestaurant = new Restaurant()
                            {
                                InternalName = facility.facilityid.ToString(),
                                Name = facility.facilityname,
                                IsOpen = true,
                                OffersLunch = true,
                                OffersDinner = false, // TODO this has to be done manually
                                Location = RestaurantLocation.ETH_UZH_Zentrum,
                                IsFood2050Supported = false,
                                ScraperTypeId = FoodScraperType.ETH_Website_v1
                            };

                            // if publication code is not 1 then there is likely no menu card
                            if (facility.publicationtypecode != 1)
                            {
                                messageString += $"[NO MENU] Restaurant {facility.facilityname} with InternalName: {facility.facilityid.ToString()} has no menu card" + Environment.NewLine;
                                continue;
                            }

                            if (!dryRun)
                            {
                                bool success = FoodDBManager.AddRestaurant(dbRestaurant);

                                messageString += $"Added new restaurant with InternalName: {facility.facilityid.ToString()} to DB: {success} at the location: {RestaurantLocation.ETH_UZH_Zentrum}" + Environment.NewLine;
                            }
                            else
                            {
                                messageString += $"[DRY] Would have added new restaurant {facility.facilityname} with InternalName: {facility.facilityid.ToString()} to DB at the location: {RestaurantLocation.ETH_UZH_Zentrum}" + Environment.NewLine;
                            }
                        }
                        else
                        {
                            messageString += $"Restaurant {facility.facilityname} already exists in DB with Internal name: {dbRestaurant.InternalName}" + Environment.NewLine;
                        }
                    }

                    // TODO duplicate code from above
                    // send up to 2000 chars max per message, split by lines
                    var lines = messageString.Split(Environment.NewLine).ToList();
                    var messages = new List<string>();
                    string currentMessage = "";
                    foreach (var line in lines)
                    {
                        if (currentMessage.Length + line.Length > 2000)
                        {
                            messages.Add(currentMessage);
                            currentMessage = "";
                        }

                        currentMessage += line + Environment.NewLine;
                    }

                    messages.Add(currentMessage);

                    foreach (var message in messages)
                        await Context.Channel.SendMessageAsync(message, false);
                    await Context.Channel.SendMessageAsync($"Done load", false);
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.ToString(), false);
                }
            }


            // set location pass location id and then comma separated the restaurant ids
            [Command("setlocation", RunMode = RunMode.Async)]
            public async Task SetLocation(int locationId, [Remainder] string restaurantIds)
            {
                var author = Context.Message.Author;
                var guildUser = Context.Message.Author as SocketGuildUser;
                if (!(author.Id == ETHDINFKBot.Program.ApplicationSetting.Owner || guildUser.GuildPermissions.ManageChannels))
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                var allRestaurants = FoodDBManager.GetAllRestaurants();


                var restaurantIdsList = restaurantIds.Split(',').Select(i => int.Parse(i)).ToList();

                foreach (var restaurantId in restaurantIdsList)
                {
                    var dbRestaurant = allRestaurants.Where(i => i.RestaurantId == restaurantId).FirstOrDefault();

                    if (dbRestaurant == null)
                    {
                        await Context.Channel.SendMessageAsync($"Could not find restaurant with id: {restaurantId}", false);
                        continue;
                    }

                    dbRestaurant.Location = (RestaurantLocation)locationId;

                    bool success = FoodDBManager.UpdateRestaurant(dbRestaurant);

                    await Context.Channel.SendMessageAsync($"Updated restaurant: {dbRestaurant.Name} with location: {locationId} with success: {success}", false);
                }
            }
        }


        [Group("rant")]
        public class RantAdminModule : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            public async Task AdminHelp()
            {
                var author = Context.Message.Author;
                var guildUser = Context.Message.Author as SocketGuildUser;
                if (!(author.Id == ETHDINFKBot.Program.ApplicationSetting.Owner || guildUser.GuildPermissions.ManageChannels))
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Admin Help (Admin only)");

                builder.WithColor(0, 0, 255);
                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                builder.WithCurrentTimestamp();
                builder.AddField("admin rant help", "This message :)");
                builder.AddField("admin rant all", "List all types");
                builder.AddField("admin rant add <type>", "Add new type (open for all)");
                builder.AddField("admin rant dt <type id>", "Delete type");
                builder.AddField("admin rant dr <rant id>", "Delete rant");

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("all")]
            public async Task AdminAllRantTypes()
            {
                // todo a bit of a duplicate from DiscordModule
                var typeList = DatabaseManager.Instance().GetAllRantTypes();
                string allTypes = "```" + string.Join(", ", typeList) + "```";

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("All Rant types");

                builder.WithColor(0, 0, 255);
                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                builder.WithCurrentTimestamp();
                builder.AddField("Types [Id, Name]", allTypes);

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("add")]
            public async Task AddRantType(string type)
            {
                /*var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }*/

                bool success = DatabaseManager.Instance().AddRantType(type);
                await Context.Channel.SendMessageAsync($"Added {type} Success: {success}", false);
            }

            [Command("dt")]
            public async Task DeleteRantType(int typeId)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                bool success = DatabaseManager.Instance().DeleteRantType(typeId);
                await Context.Channel.SendMessageAsync("Delete success: " + success, false);
            }


            [Command("dr")]
            public async Task DeleteRantMessage(int typeId)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                bool success = DatabaseManager.Instance().DeleteRantMessage(typeId);
                await Context.Channel.SendMessageAsync("Delete success: " + success, false);
            }
        }


        [Group("channel")]
        public class ChannelAdminModule : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            public async Task ChannelAdminHelp()
            {
                var author = Context.Message.Author;
                var guildUser = Context.Message.Author as SocketGuildUser;
                if (!(author.Id == Program.ApplicationSetting.Owner || guildUser.GuildPermissions.ManageChannels))
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Reddit Admin Help (Admin only)");

                builder.WithColor(0, 0, 255);
                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                builder.WithCurrentTimestamp();
                builder.AddField("admin channel help", "This message :)");
                builder.AddField("admin channel info", "Returns info about the current channel settings and global channel order info");
                builder.AddField("admin channel lock <true|false>", "Locks the ordering of all channels and reverts any order changes when active");
                builder.AddField("admin channel lockinfo", "Returns positions for all channels (if the Position lock is active)");
                builder.AddField("admin channel preload <channelId> <amount>", "Loads old messages into the DB");
                builder.AddField("admin channel set <permission> <channelId>", "Set permissions for the current channel or specific channel");
                builder.AddField("admin channel all <permission>", "Set the MINIMUM permissions for ALL channels");
                builder.AddField("admin channel flags", "Returns help with the flag infos");
                builder.AddField("admin channel create <VVZ Link>", "Creates a new ForumPost for the subject");
                builder.AddField("admin channel editpost <messageId> <content>", "Edits the current post content, if the bot is the owner of the post");
                builder.AddField("admin channel duplicate", "Checks if any forum post is duplicate");

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            /* public static IEnumerable<Enum> GetAllFlags(Enum e)
             {
                 return Enum.GetValues(e.GetType()).Cast<Enum>();
             }

             // TODO move to somewhere common
             static IEnumerable<Enum> GetFlags(Enum input)
             {
                 foreach (Enum value in Enum.GetValues(input.GetType()))
                     if (input.HasFlag(value))
                         yield return value;
             }*/

            [Command("editpost")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task EditPost(ulong messageId, [Remainder] string content)
            {
                if (Context.Channel is SocketThreadChannel socketThreadChannel)
                {
                    var parent = socketThreadChannel.ParentChannel;
                    if (parent is SocketForumChannel socketForumChannel)
                    {
                        // TODO Get first message automatically
                        var mainPostEdited = socketThreadChannel.ModifyMessageAsync(messageId, msg => msg.Content = content);
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync("This command can only be used in a forum post", false);
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync("This command can only be used in a thread channel", false);
                }
            }


            [Command("lockinfo")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task GetLockInfo()
            {
                // TODO Category infos
                ulong guildId = Program.ApplicationSetting.BaseGuild;

#if DEBUG
                guildId = 774286694794919986;
#endif

                var guild = Program.Client.GetGuild(guildId);


                var channels = guild.Channels;
                var categories = guild.CategoryChannels;

                var sortedDict = from entry in Program.ChannelPositions orderby entry.Position ascending select entry;

                List<string> header = new List<string>()
                        {
                            "Discord / Cache Position",
                            "Category Name",
                            "Channel Name"
                        };


                List<List<string>> data = new List<List<string>>();

                List<TableRowInfo> tableRowInfos = new List<TableRowInfo>();

                foreach (var category in categories.OrderBy(i => i.Position))
                {
                    var categoryInfos = Program.ChannelPositions.Where(i => i.CategoryId == category.Id);

                    var categoryInfo = new List<string>() { category.Position + " / --", category.Name, "" };
                    data.Add(categoryInfo);

                    var cells = new List<TableCellInfo>() {
                        new TableCellInfo() { ColumnId = 0, FontColor = new SkiaSharp.SKColor(255, 125, 0) },
                        new TableCellInfo() { ColumnId = 1, FontColor = new SkiaSharp.SKColor(255, 125, 0) }
                    };

                    tableRowInfos.Add(new TableRowInfo()
                    {
                        RowId = data.Count - 1,
                        Cells = cells
                    });

                    // One category
                    foreach (var channelInfo in categoryInfos.OrderBy(i => i.Position))
                    {
                        var channel = channels.SingleOrDefault(i => i.Id == channelInfo.ChannelId);
                        if (channel == null)
                            continue;

                        var currentRecord = new List<string>() { channel.Position.ToString() + " / " + channelInfo.Position.ToString(), "", channelInfo.ChannelName.ToString() };
                        //currentRecord.Add(Regex.Replace(channel.Name, @"[^\u0000-\u007F]+", string.Empty)); // replace non asci cars
                        data.Add(currentRecord);
                    }
                }

                var drawTable = new DrawTable(header, data, "", tableRowInfos, 1000);

                var stream = await drawTable.GetImage();
                if (stream == null)
                    return;// todo some message

                await Context.Channel.SendFileAsync(stream, "graph.png", "", false, null, null, false, null, new Discord.MessageReference(Context.Message.Id));
                stream.Dispose();
            }

            [Command("lock")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task LockChannelOrdering(bool lockChannels)
            {
                // allow for people that can manage channels to lock the ordering

                //var botSettings = DatabaseManager.Instance().GetBotSettings();
                //botSettings.ChannelOrderLocked = lockChannels;
                //botSettings = DatabaseManager.Instance().SetBotSettings(botSettings);

                var keyValueDBManager = DatabaseManager.KeyValueManager;

                var isLockEnabled = keyValueDBManager.Update<bool>("LockChannelPositions", lockChannels);

                await Context.Message.Channel.SendMessageAsync($"Set Global Position Lock to: {isLockEnabled}");

                if (isLockEnabled)
                {
                    // TODO Setting
                    ulong guildId = Program.ApplicationSetting.BaseGuild;

#if DEBUG
                    guildId = 774286694794919986;
#endif

                    var guild = Program.Client.GetGuild(guildId);

                    // list should always be empty
                    Program.ChannelPositions = new List<ChannelOrderInfo>();


                    // Any channels outside of categories considered?
                    foreach (var category in guild.CategoryChannels)
                        foreach (var channel in category.Channels)
                            Program.ChannelPositions.Add(new ChannelOrderInfo() { ChannelId = channel.Id, ChannelName = channel.Name, CategoryId = category.Id, CategoryName = category.Name, Position = channel.Position });


                    await Context.Message.Channel.SendMessageAsync($"Saved ordering for: {Program.ChannelPositions.Count}");
                }
                else
                {
                    // do nothing
                }
            }



            [Command("preload")]
            public async Task PreloadOldMessages(ulong channelId, int count = 1000)
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                // new column preloaded
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }
                var dbManager = DatabaseManager.Instance();
                var channel = Program.Client.GetChannel(channelId) as ISocketMessageChannel;
                var oldestMessage = dbManager.GetOldestMessageAvailablePerChannel(channelId);

                if (oldestMessage == null)
                    return;

                //var messages = channel.GetMessagesAsync(100000).FlattenAsync(); //default is 100

                var messagesFromMsg = await channel.GetMessagesAsync(oldestMessage.Value, Direction.Before, count).FlattenAsync();

                LogManager logManager = new LogManager(dbManager);
                int success = 0;
                int tags = 0;
                int newUsers = 0;
                try
                {
                    foreach (var message in messagesFromMsg)
                    {
                        var dbUser = dbManager.GetDiscordUserById(message.Author.Id);

                        if (dbUser == null)
                        {
                            var user = message.Author;
                            var socketGuildUser = user as SocketGuildUser;


                            var dbUserNew = dbManager.CreateDiscordUser(new ETHBot.DataLayer.Data.Discord.DiscordUser()
                            {
                                DiscordUserId = user.Id,
                                DiscriminatorValue = user.DiscriminatorValue,
                                AvatarUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(), // If user has no custom avatar load the url for the default avatar
                                IsBot = user.IsBot,
                                IsWebhook = user.IsWebhook,
                                Nickname = socketGuildUser?.Nickname,
                                Username = user.Username,
                                JoinedAt = socketGuildUser?.JoinedAt
                            });

                            if (dbUserNew != null)
                                newUsers++;

                        }

                        var newMessage = dbManager.CreateDiscordMessage(new ETHBot.DataLayer.Data.Discord.DiscordMessage()
                        {
                            //Channel = discordChannel,
                            DiscordChannelId = channelId,
                            //DiscordUser = dbAuthor,
                            DiscordUserId = message.Author.Id,
                            DiscordMessageId = message.Id,
                            Content = message.Content,
                            //ReplyMessageId = message.Reference.MessageId,
                            Preloaded = true
                        });


                        if (newMessage)
                        {
                            success++;
                        }
                        if (message.Reactions.Count > 0)
                        {
                            if (newMessage && message.Tags.Count > 0)
                            {
                                tags += message.Tags.Count;
                                await logManager.ProcessEmojisAndPings(message.Tags, message.Author.Id, message as SocketMessage, message.Author as SocketGuildUser);
                            }
                        }
                    }
                    watch.Stop();

                    await Context.Channel.SendMessageAsync($"Processed {messagesFromMsg.Count()} Added: {success} TagsCount: {tags} From: {SnowflakeUtils.FromSnowflake(messagesFromMsg.First()?.Id ?? 1)} To: {SnowflakeUtils.FromSnowflake(messagesFromMsg.Last()?.Id ?? 1)}" +
                        $" New Users: {newUsers} In: {watch.ElapsedMilliseconds}ms", false);
                }
                catch (Exception ex)
                {

                }
            }

            // TODO move to common
            private static string GetPermissionString(BotPermissionType flags)
            {
                List<string> permissionFlagNames = new List<string>();
                foreach (BotPermissionType flag in Enum.GetValues(typeof(BotPermissionType)))
                {
                    var hasFlag = flags.HasFlag(flag);

                    if (hasFlag)
                        permissionFlagNames.Add($"{flag} ({(int)flag})");
                }

                return string.Join(", ", permissionFlagNames);
            }


            [Command("info")]

            public async Task GetChannelInfoAsync(bool all = false)
            {
                var guildUser = Context.Message.Author as SocketGuildUser;
                var author = Context.Message.Author;
                if (!(author.Id == Program.ApplicationSetting.Owner || guildUser.GuildPermissions.ManageChannels))
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                if (Context.Message.Channel is SocketGuildChannel guildChannel)
                {
                    if (!all)
                    {
                        var channelInfo = DatabaseManager.Instance().GetChannelSetting(guildChannel.Id);
                        //var botSettings = DatabaseManager.Instance().GetBotSettings();
                        var keyValueDBManager = DatabaseManager.KeyValueManager;

                        var isLockEnabled = keyValueDBManager.Get<bool>("LockChannelPositions");

                        if (channelInfo == null)
                        {
                            Context.Channel.SendMessageAsync("channelInfo is null bad admin", false);
                            return;
                        }

                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithTitle($"Channel Info for {guildChannel.Name}");
                        builder.WithDescription($"Global Channel position lock active: {isLockEnabled} for {(isLockEnabled ? Program.ChannelPositions.Count : -1)} channels");
                        builder.WithColor(255, 0, 0);
                        builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                        builder.WithCurrentTimestamp();

                        builder.AddField("Permission flag", channelInfo.ChannelPermissionFlags);

                        foreach (BotPermissionType flag in Enum.GetValues(typeof(BotPermissionType)))
                        {
                            var hasFlag = ((BotPermissionType)channelInfo.ChannelPermissionFlags).HasFlag(flag);
                            builder.AddField(flag.ToString() + $" ({(int)flag})", $"```diff\r\n{(hasFlag ? "+ YES" : "- NO")}```", true);
                        }


                        await Context.Channel.SendMessageAsync("", false, builder.Build());

                    }
                    else
                    {
                        List<string> header = new List<string>()
                        {
                            "Category Name",
                            "Channel Name",
                            "Thread Name",
                            "Permission value",
                            "Permission string",
                            "Preload old",
                            "Preload new",
                            "Reached oldest"
                        };


                        List<List<string>> data = new List<List<string>>();

                        List<TableRowInfo> tableRowInfos = new List<TableRowInfo>();

                        var categories = Program.Client.GetGuild(guildChannel.Guild.Id).CategoryChannels.OrderBy(i => i.Position);


                        foreach (var category in categories)
                        {
                            var channelCategorySettingInfo = CommonHelper.GetChannelSettingByChannelId(category.Id, false);
                            var channelCategorySetting = channelCategorySettingInfo.Setting;

                            // New category
                            data.Add(new List<string>() {
                                category.Name,
                                "",
                                "",
                                channelCategorySetting?.ChannelPermissionFlags.ToString() ?? "N/A",
                                GetPermissionString((BotPermissionType)(channelCategorySetting?.ChannelPermissionFlags ?? 0)),
                                channelCategorySetting?.OldestPostTimePreloaded.ToString() ?? "N/A",
                                channelCategorySetting?.NewestPostTimePreloaded.ToString() ?? "N/A",
                                channelCategorySetting?.ReachedOldestPreload.ToString() ?? "N/A"
                            });

                            var cells = new List<TableCellInfo>() { new TableCellInfo() { ColumnId = 0, FontColor = new SkiaSharp.SKColor(255, 100, 0) } };
                            if (channelCategorySettingInfo.Inherit || true /* For now categories cant Inherit -> but show visually*/)
                            {
                                cells.Add(new TableCellInfo() { ColumnId = 3, FontColor = new SkiaSharp.SKColor(255, 100, 0) });
                                cells.Add(new TableCellInfo() { ColumnId = 4, FontColor = new SkiaSharp.SKColor(255, 100, 0) });
                                cells.Add(new TableCellInfo() { ColumnId = 5, FontColor = new SkiaSharp.SKColor(255, 100, 0) });
                                cells.Add(new TableCellInfo() { ColumnId = 6, FontColor = new SkiaSharp.SKColor(255, 100, 0) });
                                cells.Add(new TableCellInfo() { ColumnId = 7, FontColor = new SkiaSharp.SKColor(255, 100, 0) });
                            }

                            tableRowInfos.Add(new TableRowInfo()
                            {
                                RowId = data.Count - 1,
                                Cells = cells
                            });


                            // TODO Order
                            foreach (var channel in category.Channels)
                            {
                                var channelSettingInfo = CommonHelper.GetChannelSettingByChannelId(channel.Id, true);
                                var channelSetting = channelSettingInfo.Setting;

                                // New channel
                                data.Add(new List<string>() {
                                    "",
                                    channel.Name,
                                    "",
                                    channelSetting?.ChannelPermissionFlags.ToString() ?? "N/A",
                                    GetPermissionString((BotPermissionType)(channelSetting?.ChannelPermissionFlags ?? 0)),
                                    channelSetting?.OldestPostTimePreloaded.ToString() ?? "N/A",
                                    channelSetting?.NewestPostTimePreloaded.ToString() ?? "N/A",
                                    channelSetting?.ReachedOldestPreload.ToString() ?? "N/A"
                                });

                                var channelCells = new List<TableCellInfo>() { new TableCellInfo() { ColumnId = 1, FontColor = new SkiaSharp.SKColor(255, 255, 0) } };
                                if (channelSettingInfo.Inherit)
                                {
                                    channelCells.Add(new TableCellInfo() { ColumnId = 3, FontColor = new SkiaSharp.SKColor(0, 255, 255) });
                                    channelCells.Add(new TableCellInfo() { ColumnId = 4, FontColor = new SkiaSharp.SKColor(0, 255, 255) });
                                    channelCells.Add(new TableCellInfo() { ColumnId = 5, FontColor = new SkiaSharp.SKColor(0, 255, 255) });
                                    channelCells.Add(new TableCellInfo() { ColumnId = 6, FontColor = new SkiaSharp.SKColor(0, 255, 255) });
                                    channelCells.Add(new TableCellInfo() { ColumnId = 7, FontColor = new SkiaSharp.SKColor(0, 255, 255) });
                                }

                                tableRowInfos.Add(new TableRowInfo()
                                {
                                    RowId = data.Count - 1,
                                    Cells = channelCells
                                });

                                if (channel is SocketTextChannel socketTextChannel)
                                {
                                    // Current channel is a thread
                                    var threads = socketTextChannel.Threads;
                                    foreach (var thread in socketTextChannel.Threads)
                                    {
                                        var threadSettingInfo = CommonHelper.GetChannelSettingByThreadId(thread.Id);
                                        var threadSetting = threadSettingInfo.Setting;

                                        // New thread
                                        data.Add(new List<string>() {
                                            "",
                                            "",
                                            "#" + thread.Name,
                                            threadSetting?.ChannelPermissionFlags.ToString() ?? "N/A",
                                            GetPermissionString((BotPermissionType)(threadSetting?.ChannelPermissionFlags ?? 0)),
                                            threadSetting?.OldestPostTimePreloaded.ToString() ?? "N/A",
                                            threadSetting?.NewestPostTimePreloaded.ToString() ?? "N/A",
                                            threadSetting?.ReachedOldestPreload.ToString() ?? "N/A"
                                        });


                                        var threadCells = new List<TableCellInfo>() { new TableCellInfo() { ColumnId = 2, FontColor = new SkiaSharp.SKColor(255, 255, 255) } };
                                        if (threadSettingInfo.Inherit || true /* Thread for now always inherit */)
                                        {
                                            threadCells.Add(new TableCellInfo() { ColumnId = 3, FontColor = new SkiaSharp.SKColor(144, 238, 144) });
                                            threadCells.Add(new TableCellInfo() { ColumnId = 4, FontColor = new SkiaSharp.SKColor(144, 238, 144) });
                                            threadCells.Add(new TableCellInfo() { ColumnId = 5, FontColor = new SkiaSharp.SKColor(144, 238, 144) });
                                            threadCells.Add(new TableCellInfo() { ColumnId = 6, FontColor = new SkiaSharp.SKColor(144, 238, 144) });
                                            threadCells.Add(new TableCellInfo() { ColumnId = 7, FontColor = new SkiaSharp.SKColor(144, 238, 144) });
                                        }

                                        tableRowInfos.Add(new TableRowInfo()
                                        {
                                            RowId = data.Count - 1,
                                            Cells = threadCells
                                        });
                                    }
                                }

                                // TODO Copy of above code simplify
                                if (channel is SocketForumChannel socketForumChannel)
                                {
                                    // Current channel is a thread
                                    var threads = socketForumChannel.GetActiveThreadsAsync().Result;
                                    foreach (var thread in threads)
                                    {
                                        var threadSettingInfo = CommonHelper.GetChannelSettingByThreadId(thread.Id);
                                        var threadSetting = threadSettingInfo.Setting;

                                        // New thread
                                        data.Add(new List<string>() {
                                            "",
                                            "",
                                            "Post: " + thread.Name,
                                            threadSetting?.ChannelPermissionFlags.ToString() ?? "N/A",
                                            GetPermissionString((BotPermissionType)(threadSetting?.ChannelPermissionFlags ?? 0)),
                                            threadSetting?.OldestPostTimePreloaded.ToString() ?? "N/A",
                                            threadSetting?.NewestPostTimePreloaded.ToString() ?? "N/A",
                                            threadSetting?.ReachedOldestPreload.ToString() ?? "N/A"
                                        });


                                        var threadCells = new List<TableCellInfo>() { new TableCellInfo() { ColumnId = 2, FontColor = new SkiaSharp.SKColor(255, 255, 255) } };
                                        if (threadSettingInfo.Inherit || true /* Thread for now always inherit */)
                                        {
                                            threadCells.Add(new TableCellInfo() { ColumnId = 3, FontColor = new SkiaSharp.SKColor(144, 238, 144) });
                                            threadCells.Add(new TableCellInfo() { ColumnId = 4, FontColor = new SkiaSharp.SKColor(144, 238, 144) });
                                            threadCells.Add(new TableCellInfo() { ColumnId = 5, FontColor = new SkiaSharp.SKColor(144, 238, 144) });
                                            threadCells.Add(new TableCellInfo() { ColumnId = 6, FontColor = new SkiaSharp.SKColor(144, 238, 144) });
                                            threadCells.Add(new TableCellInfo() { ColumnId = 7, FontColor = new SkiaSharp.SKColor(144, 238, 144) });
                                        }

                                        tableRowInfos.Add(new TableRowInfo()
                                        {
                                            RowId = data.Count - 1,
                                            Cells = threadCells
                                        });
                                    }
                                }
                            }
                        }


                        var drawTable = new DrawTable(header, data, "", tableRowInfos);

                        var stream = await drawTable.GetImage();
                        if (stream == null)
                            return;// todo some message

                        await Context.Channel.SendFileAsync(stream, "graph.png", "", false, null, null, false, null, new Discord.MessageReference(Context.Message.Id));
                        stream.Dispose();
                    }
                }

            }

            [Command("set")]
            public async Task SetChannelInfoAsync(int flag, ulong? channelId = null)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }
                if (Context.Message.Channel is SocketGuildChannel guildChannel)
                {
                    if (!channelId.HasValue)
                    {
                        DatabaseManager.Instance().UpdateChannelSetting(guildChannel.Id, flag);
                        await Context.Channel.SendMessageAsync($"Set flag {flag} for channel {guildChannel.Name}", false);
                    }
                    else
                    {
                        var channel = guildChannel.Guild.GetChannel(channelId.Value);

                        DatabaseManager.Instance().UpdateChannelSetting(channel.Id, flag);
                        await Context.Channel.SendMessageAsync($"Set flag {flag} for channel {channel.Name}", false);
                    }
                }
            }

            [Command("all")]
            public async Task SetAllChannelInfoAsync(int flag)
            {
                return; // this one is a bit too risky xD
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                if (Context.Message.Channel is SocketGuildChannel guildChannel)
                {
                    var channels = DatabaseManager.Instance().GetDiscordAllChannels(guildChannel.Guild.Id);

                    foreach (var item in channels)
                    {
                        DatabaseManager.Instance().UpdateChannelSetting(item.DiscordChannelId, flag, 0, 0, true);
                        await Context.Channel.SendMessageAsync($"Set flag {flag} for channel {item.ChannelName}", false);
                    }
                }
            }


            [Command("flags")]
            public async Task GetChannelInfoFlagsAsync()
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"Available flags");

                builder.WithColor(255, 0, 0);

                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();
                string inlineString = "```";
                foreach (BotPermissionType flag in Enum.GetValues(typeof(BotPermissionType)))
                {
                    inlineString += $"{flag} ({(int)(flag)})\r\n";
                }

                inlineString = inlineString.Trim() + "```";
                builder.AddField("BotPermissionType", inlineString);

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            // check for duplicate forum posts
            [Command("duplicate")]
            public async Task CheckForDuplicateForumPosts()
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }


                List<ulong> forumIds = new List<ulong>
                {
                    1067785361062887424, // BSc
                    1067780019423817779 // MSc
                };

                Dictionary<ulong, string> titles = new Dictionary<ulong, string>();

                foreach (var forumId in forumIds)
                {
                    var forum = Context.Guild.GetChannel(forumId) as SocketForumChannel;

                    var threads = forum.GetActiveThreadsAsync().Result;
                    var archivedThreads = forum.GetPublicArchivedThreadsAsync().Result;

                    var currentThreads = threads.Concat(archivedThreads).ToList();

                    foreach (var thread in currentThreads)
                    {
                        var threadTitle = thread.Name;
                        if (titles.ContainsValue(threadTitle))
                            await Context.Channel.SendMessageAsync($"Duplicate found: {threadTitle} in {forum.Name} Link: <#{thread.Id}> and <#{titles.FirstOrDefault(x => x.Value == threadTitle).Key}>", false);
                        else
                            titles.Add(thread.Id, threadTitle);
                    }
                }

                await Context.Channel.SendMessageAsync($"Finished checking {forumIds.Count} forums for duplicates", false);
            }
        }

        [Group("place")]
        public class PlaceAdminModule : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            public async Task PlaceAdminHelp()
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("PLace Admin Help");

                builder.WithColor(0, 0, 255);


                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();
                builder.AddField("admin place help", "This message :)");
                builder.AddField("admin place verify <user> <true|false>", "Used to verify user for multipixel feature");

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("verify")]
            public async Task VerifyPlaceUser(SocketUser user, bool verified)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                var success = DatabaseManager.Instance().VerifyDiscordUserForPlace(user.Id, verified);

                await Context.Channel.SendMessageAsync($"Set <@{user.Id}> to {verified} Success: {success}", false);
            }
        }

        [Group("reddit")]
        public class RedditAdminModule : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            public async Task RedditAdminHelp()
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Reddit Admin Help (Admin only)");

                builder.WithColor(0, 0, 255);

                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();
                builder.AddField("admin reddit help", "This message :)");
                builder.AddField("admin reddit status", "Returns if there are currently active scrapers");
                builder.AddField("admin reddit add <name>", "Add Subreddit to SubredditInfos");
                builder.AddField("admin reddit ban <name>", "Manually ban");
                builder.AddField("admin reddit start <name>", "Starts the scraper for a specific subreddit if no scraper is currently running");

                Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("status")]
            public async Task CheckStatusAsync()
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }
                CheckReddit();
            }

            [Command("add")]
            public async Task AddSubredditAsync(string subredditName)
            {
                subredditName = subredditName.ToLower();

                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    Context.Channel.SendMessageAsync("Ping the owner and he will add it for you", false);
                    return;
                }

                AddSubreddit(subredditName);
            }

            [Command("ban")]
            public async Task BanSubredditAsync(string subredditName)
            {
                subredditName = subredditName.ToLower();

                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                DatabaseManager.Instance().BanSubreddit(subredditName);
                await Context.Channel.SendMessageAsync("Banned " + subredditName, false);

            }

            [Command("start")]
            public async Task StartScraperAsync(string subredditName)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                var subreddits = DatabaseManager.Instance().GetSubredditsByStatus();
                if (subreddits.Count > 0)
                {
                    await Context.Channel.SendMessageAsync($"{subreddits.First().SubredditName} is currently running. Try again later", false);
                    return;
                }
                // TODO check if no scraper is active
                await Context.Channel.SendMessageAsync($"Started {subredditName} please wait :)", false);

                if (subredditName.ToLower() == "all")
                {
                    var allSubreddits = DatabaseManager.Instance().GetSubredditsByStatus(false);

                    var allNames = allSubreddits.Select(i => i.SubredditName).ToList();

                    await Context.Channel.SendMessageAsync($"Starting", false);

                    for (int i = 0; i < allNames.Count; i += 100)
                    {
                        var items = allNames.Skip(i).Take(100);
                        await Context.Channel.SendMessageAsync($"{string.Join(", ", items)}", false);
                        // Do something with 100 or remaining items
                    }

                    await Context.Channel.SendMessageAsync($"Please wait :)", false);


                    await Task.Factory.StartNew(() => CommonHelper.ScrapReddit(allNames, Context.Channel));
                }
                else
                {
                    await Task.Factory.StartNew(() => CommonHelper.ScrapReddit(subredditName, Context.Channel));
                }
            }


            private async void CheckReddit()
            {
                var subreddits = DatabaseManager.Instance().GetSubredditsByStatus();

                foreach (var subreddit in subreddits)
                {
                    await Context.Channel.SendMessageAsync($"{subreddit.SubredditName} is active", false);
                }

                if (subreddits.Count == 0)
                {
                    await Context.Channel.SendMessageAsync($"No subreddits are currently active", false);
                }
            }

            private async void AddSubreddit(string subredditName)
            {
                var reddit = new RedditClient(Program.ApplicationSetting.RedditSetting.AppId, Program.ApplicationSetting.RedditSetting.RefreshToken, Program.ApplicationSetting.RedditSetting.AppSecret);
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    SubredditManager subManager = new SubredditManager(subredditName, reddit, context);
                    await Context.Channel.SendMessageAsync($"{subManager.SubredditName} was added to the list", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
                }
            }


            // TODO cleanup this mess


        }


        [Group("keyval")]
        public class KeyValuePairAdminModule : ModuleBase<SocketCommandContext>
        {
            List<string> SupportedTypes = new List<string>() { "Boolean", "Byte", "Char", "DateTime", "DBNull", "Decimal,", "Double", "Enum", "Int16", "Int32", "Int64", "SByte", "Single", "String", "UInt16", "UInt32", "UInt64" };

            private static KeyValueDBManager DBManager = DatabaseManager.KeyValueManager;

            [Command("help")]
            public async Task KeyValuePairAdminHelp()
            {
                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("KeyValuePair Admin Help (Admin only)");

                builder.WithColor(0, 0, 255);

                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();
                builder.AddField("admin keyval help", "This message :)");
                builder.AddField("admin keyval get <key>", "Get a specific KeyValuePair by Key");
                builder.AddField("admin keyval add <key> <value> <type>", "Add new KeyValuePair");
                builder.AddField("admin keyval update <key> <value> <type>", "Update existing KeyValuePair (Creates one if the key doesn't exist)");
                builder.AddField("admin keyval delete <key>", "Deletes the KeyValuePair");
                builder.AddField("admin keyval list", "Lists all current KeyValuePairs stored in the DB");
                builder.AddField("admin keyval supported", "Lists supported types (IConvertible)");

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("get")]
            public async Task GetKeyValuePair(string key)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                var result = DBManager.Get(key);
                await Context.Channel.SendMessageAsync($"Key: **{key}** has the value: **{result.Value}** with type: **{result.Type}**");
            }

            private string CheckSupportedType(string type)
            {
                return SupportedTypes.FirstOrDefault(item => item.Equals(type, StringComparison.OrdinalIgnoreCase));
            }

            [Command("add")]
            public async Task AddKeyValuePair(string key, string value, string type = "string")
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                // if the case is different take the one from the list
                type = CheckSupportedType(type);

                if (type == null)
                {
                    await Context.Channel.SendMessageAsync($"**{type}** is not supported");
                    return;
                }

                try
                {
                    var result = DBManager.Add(key, value, type);
                    await Context.Channel.SendMessageAsync($"Added new key: **{key}** with value: **{value}** of type **{type}**");
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.Message);
                }
            }

            [Command("update")]
            public async Task UpdateKeyValuePair(string key, string value, string type = "string")
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                // if the case is different take the one from the list
                type = CheckSupportedType(type);

                if (type == null)
                {
                    await Context.Channel.SendMessageAsync($"**{type}** is not supported");
                    return;
                }

                try
                {
                    var result = DBManager.Update(key, value, type);
                    await Context.Channel.SendMessageAsync($"Updated key: **{key}** with value: **{value}** of type **{type}**");
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.Message);
                }
            }

            [Command("delete")]
            public async Task DeleteKeyValuePair(string key)
            {
                var author = Context.Message.Author;
                if (author.Id != Program.ApplicationSetting.Owner)
                {
                    await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                try
                {
                    DBManager.Delete(key);
                    await Context.Channel.SendMessageAsync($"Deleted key: **{key}**");
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.Message);
                }
            }

            [Command("list")]
            public async Task ListKeyValuePairs()
            {
                var allStoredKeyValuePairs = DBManager.GetAll();

                // TODO better way for future when many keys are stored

                string text = "";
                foreach (var item in allStoredKeyValuePairs)
                {
                    var line = $"{item.Key}:{item.Value}";
                    if (text.Length + line.Length > 1975)
                    {
                        await Context.Channel.SendMessageAsync(text);
                        text = "";
                    }

                    text += line + Environment.NewLine;
                }

                await Context.Channel.SendMessageAsync(text);
            }

            [Command("supported")]
            public async Task ListSupportedTypes()
            {
                await Context.Channel.SendMessageAsync(string.Join(", ", SupportedTypes));
            }
        }
    }
}