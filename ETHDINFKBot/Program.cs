using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DuckSharp;
using ETHDINFKBot.Stats;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using ETHDINFKBot.Log;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data;
using ETHBot.DataLayer.Data.Enums;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Net;
using ETHDINFKBot.Helpers;

namespace ETHDINFKBot
{
    class Program
    {
        public static DiscordSocketClient Client;
        private CommandService commands;

        private IServiceProvider services;
        private static IConfiguration Configuration;
        private static string DiscordToken { get; set; }
        public static ulong Owner { get; set; }
        public static int TotalEmotes { get; set; }

        // TODO one object and somewhere else but im lazy
        public static string RedditAppId { get; set; }
        public static string RedditRefreshToken { get; set; }
        public static string RedditAppSecret { get; set; }
        public static string BasePath { get; set; }
        public static string ConnectionString { get; set; }
        public static bool TempDisableIncomming { get; set; }

        public static Dictionary<ulong, Question> CurrentActiveQuestion = new Dictionary<ulong, Question>();
        public static Dictionary<ulong, DateTime> CurrentDiscordOutOfJailTime = new Dictionary<ulong, DateTime>();

        public static ILoggerFactory Logger { get; set; }


        private static List<BotChannelSetting> BotChannelSettings;


        private static BotStats BotStats = new BotStats()
        {
            DiscordUsers = new List<Stats.DiscordUser>()
        };


        private static GlobalStats GlobalStats = new GlobalStats()
        {
            EmojiInfoUsage = new List<EmojiInfo>(),
            PingInformation = new List<PingInformation>()
        };

        private static List<ReportInfo> BlackList = new List<ReportInfo>();

        private DatabaseManager DatabaseManager = DatabaseManager.Instance();
        private LogManager LogManager = new LogManager(DatabaseManager.Instance());



        private static void CheckDirs()
        {
            //if (!Directory.Exists("Database"))
            //    Directory.CreateDirectory("Database");

            //if (!Directory.Exists("Plugins"))
            //    Directory.CreateDirectory("Plugins");

            //if (!Directory.Exists("Logs"))
            //     Directory.CreateDirectory("Logs");

            //if (!Directory.Exists("Stats"))
            //    Directory.CreateDirectory("Stats");

            //if (!Directory.Exists("Blacklist"))
            //    Directory.CreateDirectory("Blacklist");

            //if (!Directory.Exists("Blacklist\\Backup"))
            //    Directory.CreateDirectory("Blacklist\\Backup");

            //if (!Directory.Exists("Stats\\Backup"))
            //    Directory.CreateDirectory("Stats\\Backup");
        }

        static void Main(string[] args)
        {
            CheckDirs();
            Logger = LoggerFactory.Create(builder => { builder.AddConsole(); });

            /* using (ETHBotDBContext context = new ETHBotDBContext())
             {

                 context.DiscordUsers.Add(new ETHBot.DataLayer.Data.Discord.DiscordUser()
                 {
                     AvatarUrl = "",
                     DiscordUserId = (ulong)new Random().Next(1, 100000),
                     DiscriminatorValue = 1,
                     IsBot = false,
                     IsWebhook = false,
                     JoinedAt = DateTime.Now,
                     Nickname = "test1",
                     Username = "username"
                 }); ;

                 context.SaveChanges();
             }
            */

            //CheckDirs();

            Configuration = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
              .Build();

            DiscordToken = Configuration["DiscordToken"];
            Owner = Convert.ToUInt64(Configuration["Owner"]);
            BasePath = Configuration["BasePath"];
            ConnectionString = Configuration["ConnectionString"];

            RedditAppId = Configuration["Reddit:AppId"];
            RedditRefreshToken = Configuration["Reddit:RefreshToken"];
            RedditAppSecret = Configuration["Reddit:AppSecret"];


            BackupDBOnStartup();

            new Program().MainAsync(DiscordToken).GetAwaiter().GetResult();

        }


        private static Assembly LoadPlugin(string relativePath)
        {
            // Navigate up to the solution root
            string root = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(
                    Path.GetDirectoryName(
                        Path.GetDirectoryName(
                            Path.GetDirectoryName(
                                Path.GetDirectoryName(typeof(Program).Assembly.Location)))))));

            string pluginLocation = Path.GetFullPath(Path.Combine(root, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
            Console.WriteLine($"Loading commands from: {pluginLocation}");
            PluginLoadContext loadContext = new PluginLoadContext(pluginLocation);
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
        }

        public static void BackupDBOnStartup()
        {
            var path = Path.Combine(BasePath, "Database", "ETHBot.db");
            if (File.Exists(path))
            {
                var backupPath = Path.Combine(BasePath, "Database", "Backup");
                if (Directory.Exists(backupPath))
                {
                    // check if the oldest backup is newer than x h then dont backup
                    var files = Directory.GetFiles(backupPath);

                    if (files.Length > 50)
                    {

                    }

                    string oldestFile = files.ToList().OrderByDescending(i => i).First();
                    var fileInfo = new FileInfo(oldestFile);

                    if (fileInfo.CreationTimeUtc > DateTime.UtcNow.AddHours(-24))
                        return;
                }

                var path2 = Path.Combine(BasePath, "Database", "Backup", $"ETHBot_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.db");
                File.Copy(path, path2);
            }
        }






        public static void LoadChannelSettings()
        {
            BotChannelSettings = DatabaseManager.Instance().GetAllChannelSettings();
        }

        public async Task MainAsync(string token)
        {
            DatabaseManager.Instance().SetAllSubredditsStatus();
            LoadChannelSettings();


            var config = new DiscordSocketConfig { MessageCacheSize = 250 };
            Client = new DiscordSocketClient(config);

            Client.MessageReceived += HandleCommandAsync;
            Client.ReactionAdded += Client_ReactionAdded;
            Client.ReactionRemoved += Client_ReactionRemoved;
            Client.MessageDeleted += Client_MessageDeleted;
            Client.MessageUpdated += Client_MessageUpdated;

            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();

#if DEBUG
            await Client.SetGameAsync($"DEV MODE");
#else
            //await Client.SetGameAsync($"with a neko");
            TotalEmotes = DatabaseManager.Instance().TotalEmoteCount();
            await Client.SetGameAsync($"{TotalEmotes} emotes", null, ActivityType.Watching);
#endif


            services = new ServiceCollection()
                .AddSingleton(Client)
                .AddSingleton<InteractiveService>()
                .BuildServiceProvider();

            commands = new CommandService();
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);



            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Client_MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel arg3)
        {
            if (TempDisableIncomming) 
                return Task.CompletedTask;

            if (before.HasValue)
            {
                if (!AllowedToRun(before.Value.Channel.Id, BotPermissionType.RemovedPingMessage))
                    return Task.CompletedTask;

                if (before.Value.Tags?.Where(i => i.Type == TagType.UserMention || i.Type == TagType.RoleMention).Count() > 0)
                {
                    if (before.Value.Content.StartsWith("$q"))
                        return Task.CompletedTask; // exclude quotes

                    if (before.Value.CreatedAt.UtcDateTime < DateTime.UtcNow.AddMinutes(-15))
                        return Task.CompletedTask; // only track for first 15 mins

                    if (!before.Value.Tags.Any(i => after.Tags.Contains(i)))
                    {
                        // TODO similar code to deleted
                        EmbedBuilder builder = new EmbedBuilder();
                        var guildUser = before.Value.Author as SocketGuildUser;
                        builder.WithTitle($"{guildUser.Nickname} is a really bad person because he edited a message with a ping");

                        string messageText = "";
                        foreach (var item in before.Value.Tags.Where(i => i.Type == TagType.UserMention || i.Type == TagType.RoleMention))
                        {
                            string pefixForRole = item.Type == TagType.RoleMention ? "&" : "";
                            messageText += $"Poor <@{pefixForRole}{item.Key}>" + Environment.NewLine;
                        }

                        builder.WithDescription(messageText);
                        builder.WithColor(255, 64, 128);

                        builder.WithAuthor(before.Value.Author);

                        builder.WithCurrentTimestamp();

                        before.Value.Channel.SendMessageAsync("", false, builder.Build());
                    }
                }
            }

            return Task.CompletedTask;
        }



        private Task Client_MessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel arg2)
        {
            if (TempDisableIncomming)
                return Task.CompletedTask;

            if (message.HasValue)
            {
                if (!AllowedToRun(message.Value.Channel.Id, BotPermissionType.RemovedPingMessage))
                    return Task.CompletedTask;

                if (message.Value.Tags?.Where(i => i.Type == TagType.UserMention || i.Type == TagType.RoleMention).Count() > 0)
                {
                    if (message.Value.Content.StartsWith("$q"))
                        return Task.CompletedTask; // exclude quotes

                    if (message.Value.CreatedAt.UtcDateTime < DateTime.UtcNow.AddMinutes(-15))
                        return Task.CompletedTask; // only track for first 15 mins

                    EmbedBuilder builder = new EmbedBuilder();
                    var guildUser = message.Value.Author as SocketGuildUser;
                    builder.WithTitle($"{guildUser.Nickname} is a really bad person because he deleted a message with a ping");

                    string messageText = "";
                    foreach (var item in message.Value.Tags.Where(i => i.Type == TagType.UserMention || i.Type == TagType.RoleMention))
                    {
                        string pefixForRole = item.Type == TagType.RoleMention ? "&" : "";
                        messageText += $"Poor <@{pefixForRole}{item.Key}>" + Environment.NewLine;
                    }

                    builder.WithDescription(messageText);
                    builder.WithColor(255, 64, 128);

                    builder.WithAuthor(message.Value.Author);

                    builder.WithCurrentTimestamp();

                    message.Value.Channel.SendMessageAsync("", false, builder.Build());
                }
            }

            return Task.CompletedTask;
        }

        private Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            if (TempDisableIncomming)
                return Task.CompletedTask;

            if (((SocketGuildUser)arg3.User).IsBot == true)
                return Task.CompletedTask;

            Console.ForegroundColor = ConsoleColor.Red;

            if (arg3.Emote is Emote)
            {
                //Console.WriteLine($"Removed emote {arg3.Emote.Name} by {arg3.User.Value.Username}");
                LogManager.RemoveReaction((Emote)arg3.Emote, arg1.Id, ((SocketGuildUser)arg3.User));

                if (((Emote)arg3.Emote).Id == 780179874656419880)
                {
                    // TODO Remove the post from saved
                }
            }

            return Task.CompletedTask;
        }

        private bool AllowedToRun(ulong channelId, BotPermissionType type)
        {
            var channelSettings = DatabaseManager.GetChannelSetting(channelId);
            if (channelSettings == null)
                return false;

            if (((BotPermissionType)channelSettings?.ChannelPermissionFlags).HasFlag(type))
            {
                return true;
            }

            return false;
        }


        private Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            if (TempDisableIncomming)
                return Task.CompletedTask;

            try
            {
                // only if an emote has been used we dont count emojis yet
                if (arg3.Emote is Emote)
                {

                    /*if ( == true)
                        return Task.CompletedTask;*/

                    Console.ForegroundColor = ConsoleColor.Green;
                    //Console.WriteLine($"Added emote {arg3.Emote.Name} by {arg3.User.Value.Username}");
                    LogManager.AddReaction((Emote)arg3.Emote, arg1.Id, ((SocketGuildUser)arg3.User));

                    if (((Emote)arg3.Emote).Id == 780179874656419880 && !arg3.User.Value.IsBot)
                    {
                        // Save the post link

                        /*          var user = DatabaseManager.GetDiscordUserById(arg1.Value.Author.Id); // Verify the user is created but should actually be available by this poitn
                        var saveBy = DatabaseManager.GetDiscordUserById(arg3.User.Value.Id); // Verify the user is created but should actually be available by this poitn
                        */

                        if (DatabaseManager.IsSaveMessage(arg1.Value.Id, arg3.User.Value.Id))
                        {
                            // dont allow double saves
                            return Task.CompletedTask;
                        }

                        var guildChannel = (SocketGuildChannel)arg1.Value.Channel;
                        var link = $"https://discord.com/channels/{guildChannel.Guild.Id}/{guildChannel.Id}/{arg1.Value.Id}";
                        if (!string.IsNullOrWhiteSpace(arg1.Value.Content))
                        {
                            DatabaseManager.SaveMessage(arg1.Value.Id, arg1.Value.Author.Id, arg3.User.Value.Id, link, arg1.Value.Content);
                            // TODO parse to guild user

                            arg3.User.Value.SendMessageAsync($"Saved post from {arg1.Value.Author.Username}: " +
                                $"{Environment.NewLine} {arg1.Value.Content} {Environment.NewLine}Direct link: [{guildChannel.Guild.Name}/{guildChannel.Name}/by {arg1.Value.Author.Username}] <{link}>");
                        }

                        foreach (var item in arg1.Value.Embeds)
                        {
                            DatabaseManager.SaveMessage(arg1.Value.Id, arg1.Value.Author.Id, arg3.User.Value.Id, link, "Embed: " + ((Embed)item).ToString());
                            arg3.User.Value.SendMessageAsync("", false, ((Embed)item));
                        }


                        if (arg1.Value.Attachments.Count > 0)
                        {
                            //return Task.CompletedTask;

                            foreach (var item in arg1.Value.Attachments)
                            {
                                DatabaseManager.SaveMessage(arg1.Value.Id, arg1.Value.Author.Id, arg3.User.Value.Id, link, item.Url);

                                /*DatabaseManager.SaveMessage(new SavedMessage()
                                {
                                    DirectLink = link,
                                    SendInDM = false,
                                    Content = item.Url, // todo attachment save to disk
                                    MessageId = arg1.Value.Id,
                                    ByDiscordUserId = user.DiscordUserId,
                                    ByDiscordUser = user,
                                    SavedByDiscordUserId = saveBy.DiscordUserId,
                                    SavedByDiscordUser = saveBy
                                });*/
                                // TODO markdown

                                arg3.User.Value.SendMessageAsync($"Saved post (Attachment) from {arg1.Value.Author.Username}: " +
                                    $"{Environment.NewLine} {item.Url}{Environment.NewLine}Direct link: [{guildChannel.Guild.Name}/{guildChannel.Name}/by {arg1.Value.Author.Username}] <{link}>");
                            }

                        }

                        if (AllowedToRun(arg1.Value.Channel.Id, BotPermissionType.SaveMessage))
                        {
                            EmbedBuilder builder = new EmbedBuilder();

                            builder.WithTitle($"{arg3.User.Value.Username} saved a message");
                            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");
                            //builder.WithDescription($@"");
                            builder.WithColor(0, 0, 255);

                            //builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");

                            builder.WithCurrentTimestamp();
                            builder.AddField("Message Link", $"[Message]({link})", true);

                            builder.AddField("Message Author", $"{arg1.Value.Author.Username}", true);

                            // TODO More stats

                            arg1.Value.Channel.SendMessageAsync("", false, builder.Build());
                        }
                        // TODO markdown -> guild user also
                        //arg1.Value.Channel.SendMessageAsync($"{arg3.User.Value.Username} saves <{link}> by {arg1.Value.Author.Username}");
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return Task.CompletedTask;
        }

        private async Task<bool> TryToParseEmoji(SocketUserMessage msg)
        {
            if (msg.Channel is SocketGuildChannel guildChannel)
            {
                if (msg.Content.StartsWith("."))
                {
                    // check if the emoji exists and if the emojis is animated
                    string name = msg.Content.Substring(1, msg.Content.Length - 1);

                    var emote = DatabaseManager.GetEmoteByName(name);

                    if (emote != null)
                    {
                        var guildUser = msg.Author as SocketGuildUser;

                        msg.DeleteAsync();

                        var emoteString = $"<{(emote.Animated ? "a" : "")}:{emote.EmoteName}:{emote.DiscordEmoteId}>";

                        if (guildChannel.Guild.Emotes.Any(i => i.Id == emote.DiscordEmoteId))
                        {
                            // we can post the emote as it will be rendered out
                            await msg.Channel.SendMessageAsync(emoteString);
                        }
                        else
                        {
                            // TODO store resized images in db for faster reuse
                            if (emote.Animated)
                            {
                                // TODO gif resize
                                await msg.Channel.SendMessageAsync(emote.Url);
                            }
                            else
                            {
                                using (WebClient client = new WebClient())
                                {
                                    var imageBytes = client.DownloadData(emote.Url);

                                    using (var ms = new MemoryStream(imageBytes))
                                    {
                                        var image = System.Drawing.Image.FromStream(ms);
                                        var resImage = ResizeImage(image, Math.Min(image.Height, 64));

                                        var stream = new MemoryStream();

                                        resImage.Save(stream, resImage.RawFormat);
                                        stream.Position = 0;


                                        await msg.Channel.SendFileAsync(stream, $"{emote.EmoteName}.png");
                                    }
                                }
                            }
                            // we need to send the image as the current server doesnt have access

                        }

                        await msg.Channel.SendMessageAsync($"(.{name}) by <@{guildUser.Id}>");

                        return true;
                    }
                }
            }

            return false;
        }

        private static Dictionary<ulong, DateTime> SpamCache = new Dictionary<ulong, DateTime>();
        public async Task HandleCommandAsync(SocketMessage m)
        {
            if (TempDisableIncomming)
                return;

            if (!(m is SocketUserMessage msg)) return;

            // check if the emote is a command -> block
            List<CommandInfo> commandList = commands.Commands.ToList();
            if (!m.Author.IsBot && !commandList.Any(i => i.Name.ToLower() == msg.Content.ToLower().Replace(".", "")) && await TryToParseEmoji(msg))
                return; // emoji was found and we can exit here

            // TODO private channels

            var dbManager = DatabaseManager.Instance();

            DiscordServer discordServer = null;
            DiscordChannel discordChannel = null;
            BotChannelSetting channelSettings = null;
            if (msg.Channel is SocketGuildChannel guildChannel)
            {
                channelSettings = BotChannelSettings?.SingleOrDefault(i => i.DiscordChannelId == guildChannel.Id);

                discordServer = dbManager.GetDiscordServerById(guildChannel.Guild.Id);
                if (discordServer == null)
                {
                    discordServer = dbManager.CreateDiscordServer(new ETHBot.DataLayer.Data.Discord.DiscordServer()
                    {
                        DiscordServerId = guildChannel.Guild.Id,
                        ServerName = guildChannel.Guild.Name
                    });
                }

                discordChannel = dbManager.GetDiscordChannel(guildChannel.Id);
                if (discordChannel == null)
                {
                    discordChannel = dbManager.CreateDiscordChannel(new ETHBot.DataLayer.Data.Discord.DiscordChannel()
                    {
                        DiscordChannelId = guildChannel.Id,
                        ChannelName = guildChannel.Name,
                        DiscordServerId = discordServer.DiscordServerId
                    });
                }
            }
            else
            {
                // NO DM Tracking
                return;
            }

            if (channelSettings == null && m.Author.Id != Owner)
            {
                // No perms for this channel
                return;
            }

            var dbAuthor = dbManager.GetDiscordUserById(msg.Author.Id);
            // todo check for update
            var user = (SocketGuildUser)msg.Author;
            if (dbAuthor == null)
            {
                // todo check non socket user

                dbAuthor = dbManager.CreateDiscordUser(new ETHBot.DataLayer.Data.Discord.DiscordUser()
                {
                    DiscordUserId = user.Id,
                    DiscriminatorValue = user.DiscriminatorValue,
                    AvatarUrl = user.GetAvatarUrl(),
                    IsBot = user.IsBot,
                    IsWebhook = user.IsWebhook,
                    Nickname = user.Nickname,
                    Username = user.Username,
                    JoinedAt = user.JoinedAt
                });

                dbAuthor = dbManager.GetDiscordUserById(msg.Author.Id);
            }
            else
            {
                dbManager.UpdateDiscordUser(new ETHBot.DataLayer.Data.Discord.DiscordUser()
                {
                    DiscordUserId = user.Id,
                    DiscriminatorValue = user.DiscriminatorValue,
                    AvatarUrl = user.GetAvatarUrl(),
                    IsBot = user.IsBot,
                    IsWebhook = user.IsWebhook,
                    Nickname = user.Nickname,
                    Username = user.Username,
                    JoinedAt = user.JoinedAt
                });
            }


            if (m.Author.Id != Owner && !((BotPermissionType)channelSettings?.ChannelPermissionFlags).HasFlag(BotPermissionType.Read))
            {
                // Cant read
                return;
            }
            /*
                        if (channelSettings.DiscordChannelId == 747754931905364000 || channelSettings.DiscordChannelId == 747768907992924192 || channelSettings.DiscordChannelId == 774322847812157450 || channelSettings.DiscordChannelId == 774322031688679454
                            || channelSettings.DiscordChannelId == 773914288913514546)
                        {
                            // staff / bot / adminlog / modlog / teachingassistants
                            // TODO settings better
                        }*/

            if (channelSettings != null && ((BotPermissionType)channelSettings?.ChannelPermissionFlags).HasFlag(BotPermissionType.Read))
            {
                ulong? referenceMessageId = null;
                if (msg.Reference != null)
                {
                    referenceMessageId = (ulong?)msg.Reference.MessageId;

                    if (dbManager.GetDiscordMessageById(referenceMessageId) == null)
                    {
                        referenceMessageId = null; // original message is not in the db therefore do not link
                    }
                }

                dbManager.CreateDiscordMessage(new ETHBot.DataLayer.Data.Discord.DiscordMessage()
                {
                    //Channel = discordChannel,
                    DiscordChannelId = discordChannel.DiscordChannelId,
                    //DiscordUser = dbAuthor,
                    DiscordUserId = dbAuthor.DiscordUserId,
                    MessageId = msg.Id,
                    Content = msg.Content,
                    ReplyMessageId = referenceMessageId
                }, true);
            }


            await LogManager.ProcessEmojisAndPings(m.Tags, m.Author.Id, m.Id, ((SocketGuildUser)m.Author));

            var message = msg.Content;
            var randVal = msg.Author.DiscriminatorValue % 10;

            // TODO Different color for defcom bot
            switch (randVal)
            {
                case 0:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case 1:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case 2:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case 3:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case 4:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case 5:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case 6:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case 7:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                case 8:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                case 9:
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    break;
                default:
                    break;
            }

            //Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {msg.Author} wrote: {msg.Content}");
            //File.AppendAllText($"Logs\\ETHDINFK_{DateTime.Now:yyyy_MM_dd}_spam.txt", $"[{DateTime.Now:yyyy.MM.dd HH:mm:ss}] " + msg.Author + " wrote: " + msg.Content + Environment.NewLine);

            if (m.Channel.Id == 747758757395562557 || m.Channel.Id == 758293511514226718 || m.Channel.Id == 747758770393972807 ||
            m.Channel.Id == 774286694794919989)
            {
                // TODO Channel ID as config
                await m.AddReactionAsync(Emote.Parse("<:this:747783377662378004>"));
                await m.AddReactionAsync(Emote.Parse("<:that:758262252699779073>"));
            }

            if (m.Author.IsBot)
                return;

            if (user.Roles.Any(i => i.Id == 798639212818726952) && false /* disabled for now */)
            {
                HandleQuestionAnswer(msg);
            }

            int argPos = 0;

            // accept .dev only in dev mode
#if !DEBUG
            if (!msg.HasStringPrefix(".", ref argPos))
                return;
#else
            if (!msg.HasStringPrefix("dev.", ref argPos))
                return;
#endif



            if (m.Author.Id != Owner)
            {
                if (SpamCache.ContainsKey(m.Author.Id))
                {
                    if (SpamCache[m.Author.Id] > DateTime.Now.AddMilliseconds(-500))
                    {
                        SpamCache[m.Author.Id] = SpamCache[m.Author.Id].AddMilliseconds(750);

                        // TODO save last no spam message time
                        if (new Random().Next(0, 20) == 0)
                        {
                            // Ignore the user than to reply takes 1 message away from the rate limit
                            m.Channel.SendMessageAsync($"Stop spamming <@{m.Author.Id}> your current timeout is {SpamCache[m.Author.Id]}ms");
                        }

                        return;
                    }

                    SpamCache[m.Author.Id] = DateTime.Now;
                }
                else
                {
                    SpamCache.Add(m.Author.Id, DateTime.Now);
                }
            }

            Console.ResetColor();




            var context = new SocketCommandContext(Client, msg);
            commands.ExecuteAsync(context, argPos, services);
        }


        private static async void HandleQuestionAnswer(SocketMessage msg)
        {
            int timeOnFreshAnswer = 2;
            int additionalTime = 1;

            var user = (SocketGuildUser)msg.Author;
            bool inJail = true;

            // only handle users with the role
            if (CurrentDiscordOutOfJailTime.ContainsKey(user.Id))
            {
                var time = CurrentDiscordOutOfJailTime[user.Id];

                if (time > DateTime.Now)
                {
                    // he is still free to browse

                    var replyMsg = await msg.Channel.SendMessageAsync($"{user.Nickname} you still have {(time - DateTime.Now).TotalSeconds} second(s) of free time. Enjoy");
                    await Task.Delay(3000);
                    await replyMsg.DeleteAsync();

                    inJail = false;
                }
                else
                {
                    CurrentDiscordOutOfJailTime.Remove(user.Id);
                }
            }

            string reply = msg.Content.ToLower();

            if (inJail)
            {
                // delete the message
                await msg.DeleteAsync();
            }

            if (CurrentActiveQuestion.ContainsKey(user.Id))
            {
                var question = CurrentActiveQuestion[user.Id];

                if (reply != question.Answer.ToLower() && inJail)
                {
                    var replyMsg = await msg.Channel.SendMessageAsync($"{user.Nickname} you are still in jail. Answer my question! Type .repeat if you forgot it");

                    await Task.Delay(10000);
                    await replyMsg.DeleteAsync();
                }
                else if (reply == question.Answer.ToLower())
                {
                    if (inJail)
                    {
                        await msg.Channel.SendMessageAsync($"Congrats! You answered correctly. You earned yourself {timeOnFreshAnswer}min of Discord distraction GUILT FREE <:POGGERS:747783377838407691> " + Environment.NewLine +
                            "Remember you can still call .question for a random question to extend your free time");
                    }
                    else
                    {
                        await msg.DeleteAsync(); // delete the answer
                        await msg.Channel.SendMessageAsync($"Congrats! You extended your free time by {additionalTime}min");
                    }

                    if (CurrentActiveQuestion.ContainsKey(user.Id))
                        CurrentActiveQuestion.Remove(user.Id);

                    if (CurrentDiscordOutOfJailTime.ContainsKey(user.Id))
                    {
                        var time = CurrentDiscordOutOfJailTime[user.Id];

                        if (time > DateTime.Now)
                        {
                            // user is still in free time but they can extent if they keep continuing to answer questions.
                            CurrentDiscordOutOfJailTime[user.Id] = CurrentDiscordOutOfJailTime[user.Id].AddMinutes(additionalTime);
                        }
                        else
                        {
                            CurrentDiscordOutOfJailTime[user.Id] = DateTime.Now.AddMinutes(timeOnFreshAnswer);
                        }
                    }
                    else
                    {
                        CurrentDiscordOutOfJailTime.Add(user.Id, DateTime.Now.AddMinutes(timeOnFreshAnswer));
                    }
                }
            }
            else if (inJail)
            {
                // THE user has no question assign one
                StudyHelper helper = new StudyHelper();

                var question = helper.GetRandomLinalgQuestion();

                CurrentActiveQuestion.Add(user.Id, question);

                var replyMsg = await msg.Channel.SendMessageAsync($"{user.Nickname} you are still in jail. Answer my question! Type .repeat if you forgot it");

                await Task.Delay(10000);
                await replyMsg.DeleteAsync();
            }
        }

        // source https://stackoverflow.com/questions/1922040/how-to-resize-an-image-c-sharp

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(System.Drawing.Image image, int height)
        {

            decimal ratio = image.Width / (decimal)image.Height;

            var destRect = new Rectangle(0, 0, (int)(height * ratio), height);
            var destImage = new Bitmap((int)(height * ratio), height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        private async static void Test()
        {

        }
    }
}
