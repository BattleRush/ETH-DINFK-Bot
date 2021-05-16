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
using System.Threading;
using Microsoft.Extensions.Hosting;
using ETHDINFKBot.CronJobs;
using ETHDINFKBot.CronJobs.Jobs;
using ETHDINFKBot.Handlers;
using TimeZoneConverter;
//using WebSocketSharp.Server;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using NetCoreServer;
using System.Text;
using System.Net.Sockets;

namespace ETHDINFKBot
{

    class PlaceServer : WssServer
    {
        public PlaceServer(SslContext context, IPAddress address, int port) : base(context, address, port) { }

        protected override SslSession CreateSession() { return new PlaceSession(this); }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat WebSocket server caught an error with code {error}");
        }
    }


    class Program
    {
        public static DiscordSocketClient Client;
        private CommandService commands;

        private IServiceProvider services;
        private static IConfiguration Configuration;
        private static string DiscordToken { get; set; }
        public static ulong Owner { get; set; }
        public static long TotalEmotes { get; set; }

        // TODO one object and somewhere else but im lazy
        public static string RedditAppId { get; set; }
        public static string RedditRefreshToken { get; set; }
        public static string RedditAppSecret { get; set; }
        public static string BasePath { get; set; }
        public static string ConnectionString { get; set; }
        public static string MariaDBFullUserName { get; set; }
        public static string MariaDBReadOnlyUserName { get; set; }
        public static string MariaDBReadOnlyConnectionString { get; set; }
        public static string MariaDBDBName { get; set; }

        public static string CurrentPrefix { get; set; }

        // TODO maybe compiler warning -> but longterm settings need to be moved from here
        public static string FULL_MariaDBReadOnlyConnectionString { get; set; }

        // TODO Move settings to an object
        public static bool TempDisableIncomming { get; set; }

        public static Dictionary<ulong, Question> CurrentActiveQuestion = new Dictionary<ulong, Question>();
        public static Dictionary<ulong, DateTime> CurrentDiscordOutOfJailTime = new Dictionary<ulong, DateTime>();

        public static TimeZoneInfo TimeZoneInfo = TZConvert.GetTimeZoneInfo("Europe/Zurich");
        public static ILoggerFactory Logger { get; set; }

        private static DateTime LastNewDailyMessagePost = DateTime.Now;

        private static List<BotChannelSetting> BotChannelSettings;

        private static List<string> AllowedBotCommands;

        //public static WebSocketServer PlaceWebsocket;
        public static PlaceServer PlaceServer;



        //private static BotStats BotStats = new BotStats()
        //{
        //    DiscordUsers = new List<Stats.DiscordUser>()
        //};


        //private static GlobalStats GlobalStats = new GlobalStats()
        //{
        //    EmojiInfoUsage = new List<EmojiInfo>(),
        //    PingInformation = new List<PingInformation>()
        //};

        //private static List<ReportInfo> BlackList = new List<ReportInfo>();

        private DatabaseManager DatabaseManager = DatabaseManager.Instance();
        private LogManager LogManager = new LogManager(DatabaseManager.Instance());

        public static BotSetting BotSetting;

        static void Main(string[] args)
        {
            CurrentPrefix = ".";

#if DEBUG
            CurrentPrefix = "dev.";
#endif

            AllowedBotCommands = new List<string>() { CurrentPrefix + "place setpixel ", CurrentPrefix + "place pixelverify " };

            try
            {
                // TODO may cause problems if the bot is hosted in a timezone that doesnt switch to daylight at the same time as the hosting region
                LastNewDailyMessagePost = DateTime.UtcNow.AddHours(TimeZoneInfo.IsDaylightSavingTime(DateTime.Now) ? 2 : 1);

                Logger = LoggerFactory.Create(builder => { builder.AddConsole(); });

                BotSetting = DatabaseManager.Instance().GetBotSettings();

                var host = new HostBuilder()
                   .ConfigureServices((hostContext, services) =>
                   {
                       // TODO read from DB

                       //services.AddCronJob<CronJobTest>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = @"* * * * *"; });

                       // once a day at 1 or 2 AM CET/CEST
                       services.AddCronJob<CleanUpServerSuggestions>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = @"0 0 * * *"; });

                       // TODO adjust for summer time in CET/CEST
                       services.AddCronJob<DailyStatsJob>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = @"0 23 * * *"; });

                       // TODO adjust for summer time in CET/CEST
                       // TODO Enable for Maria DB
                       services.AddCronJob<PreloadJob>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = @"0 3 * * *"; });// 3 am utc -> 4 am cet

                       // TODO adjust for summer time in CET/CEST
                       services.AddCronJob<SpaceXSubredditJob>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = BotSetting?.SpaceXSubredditCheckCronJob ?? "*/10 * * * *"; }); //BotSetting.SpaceXSubredditCheckCronJob "*/ 10 * * * *"

                       // TODO adjust for summer time in CET/CEST
                       services.AddCronJob<StartAllSubredditsJobs>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = @"0 4 * * *"; });// 4 am utc -> 5 am cet

                       // TODO adjust for summer time in CET/CEST
                       //services.AddCronJob<BackupDBJob>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = @"0 4 * * *"; });// 0 am utc
                   })
                   .StartAsync();


                Configuration = new ConfigurationBuilder()
                  .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                  .Build();

                DiscordToken = Configuration["DiscordToken"];
                Owner = Convert.ToUInt64(Configuration["Owner"]);
                BasePath = Configuration["BasePath"];
                ConnectionString = Configuration["ConnectionString"];
                // TODO Update for new connection strings and dev/prod

                MariaDBReadOnlyConnectionString = Configuration.GetConnectionString("ConnectionString_ReadOnly").ToString();
                FULL_MariaDBReadOnlyConnectionString = Configuration.GetConnectionString("ConnectionString_Full").ToString();

                MariaDBFullUserName = Configuration["MariaDB_FullUserName"];
                MariaDBReadOnlyUserName = Configuration["MariaDB_ReadOnlyUserName"];
                MariaDBDBName = Configuration["MariaDB_DBName"];

                RedditAppId = Configuration["Reddit:AppId"];
                RedditRefreshToken = Configuration["Reddit:RefreshToken"];
                RedditAppSecret = Configuration["Reddit:AppSecret"];


                //BackupDBOnStartup();

                new Program().MainAsync(DiscordToken).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine("CRITICAL ERROR: " + ex.ToString());
            }
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

                var path2 = Path.Combine(BasePath, "Database", "Backup", $"ETHBot_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                File.Copy(path, path2);
            }
        }

        public static void LoadChannelSettings()
        {
            BotChannelSettings = DatabaseManager.Instance().GetAllChannelSettings();
        }

        public async Task MainAsync(string token)
        {

            /*
            // TODO If debug -> dont use secure
            #if DEBUG
                        PlaceWebsocket = new WebSocketServer(9000);
                        PlaceWebsocket.AddWebSocketService<PlaceWebsocket>("/place");
                        PlaceWebsocket.Start();
#else

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            PlaceWebsocket = new WebSocketServer(9001, true);
            PlaceWebsocket.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
            /*PlaceWebsocket.SslConfiguration.ClientCertificateValidationCallback =
              (sender, certificate, chain, sslPolicyErrors) => {
                  // Do something to validate the server certificate.
     

                return true; // If the server certificate is valid.
              };
            */
            /*
            var cert = new X509Certificate2(Path.Combine(Configuration["CertFilePath"], "battlerush.dev.pfx"));
            PlaceWebsocket.SslConfiguration.ServerCertificate = cert;

            PlaceWebsocket.AddWebSocketService<PlaceWebsocket>("/place");

            PlaceWebsocket.Log.Level = WebSocketSharp.LogLevel.Debug;
            PlaceWebsocket.Log.File = Path.Combine(BasePath, "Log", "WebsocketLog.txt");

            //PlaceWebsocket.SslConfiguration.ClientCertificateRequired = false;
            //PlaceWebsocket.SslConfiguration.CheckCertificateRevocation = false;
            PlaceWebsocket.Start();
#endif
            */

            // WebSocket server content path
            string www = "/var/www/wss";

            //string www = @"C:\Temp\wss";
            // Create and prepare a new SSL server context
            var context = new SslContext(SslProtocols.Tls12, new X509Certificate2(Path.Combine(Configuration["CertFilePath"], "battlerush.dev.pfx")));
            //var context = new SslContext(SslProtocols.Tls12);
            // Create a new WebSocket server
            PlaceServer = new PlaceServer(context, IPAddress.Any, 9000);
            PlaceServer.AddStaticContent(www, "/place");

            // Start the server
            Console.Write("Server starting...");
            PlaceServer.Start();
            Console.WriteLine("Done!");



            DatabaseManager.Instance().SetAllSubredditsStatus();
            LoadChannelSettings();
            DatabaseManager.Instance().AddBotStartUp(); // register bot startup time

            var config = new DiscordSocketConfig
            {
                MessageCacheSize = 250,
                AlwaysDownloadUsers = true
            };

            Client = new DiscordSocketClient(config);

            Client.MessageReceived += HandleCommandAsync;
            Client.ReactionAdded += Client_ReactionAdded;
            Client.ReactionRemoved += Client_ReactionRemoved;
            Client.MessageDeleted += Client_MessageDeleted;
            Client.MessageUpdated += Client_MessageUpdated;
            Client.RoleCreated += Client_RoleCreated;
            Client.Ready += Client_Ready;

            Client.Log += Client_Log;

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


            if (DatabaseManager.GetDiscordServerById(747752542741725244) == null)
            {
                // in ready event we should start the migration
                TempDisableIncomming = true;
            }


            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Client_Log(LogMessage arg)
        {
            if (arg.Severity == LogSeverity.Error || arg.Severity == LogSeverity.Critical)
                Console.Write(arg.Message);

            return Task.CompletedTask;
            //throw new NotImplementedException();
        }


        //https://www.gngrninja.com/code/2019/4/1/c-discord-bot-command-handling
        //public async Task InitializeAsync()
        //{
        //    // register modules that are public and inherit ModuleBase<T>.
        //    await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        //}

        private Task Client_Ready()
        {
            if (!TempDisableIncomming)
                return Task.CompletedTask;
            //OnlyHereToTestMyBadCodingSkills

            // todo config
            ulong guildId = 747752542741725244;
            ulong spamChannel = 768600365602963496;
            var guild = Program.Client.GetGuild(guildId);

            var textChannel = guild.GetTextChannel(spamChannel);

            try
            {
                textChannel.SendMessageAsync("Starting DB Migration");


                MigrateSQLiteToMariaDB migration = new MigrateSQLiteToMariaDB();

                int count = migration.MigrateDiscordServers();
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} DiscordServers");

                count = migration.MigrateDiscordChannels();
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} DiscordChannels");

                count = migration.MigrateDiscordUsers();
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} DiscordUsers");


                count = migration.MigrateDiscordMessages(textChannel);
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} DiscordMessagess");


                count = migration.MigrateDiscordEmotes(textChannel);
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} DiscordEmotes");


                count = migration.MigrateDiscordEmoteStatistics(textChannel);
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} DiscordEmoteStatistics");

                count = migration.MigrateDiscordEmoteHistory(textChannel);
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} DiscordEmoteHistory");

                count = migration.MigrateBannedLinks();
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} BannedLinks");

                count = migration.MigrateCommandTypes();
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} CommandTypes");

                count = migration.MigrateCommandStatistics();
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} CommandStatistics");

                count = migration.MigrateDiscordRoles();
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} DiscordRoles");

                count = migration.MigratePingHistory(textChannel);
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} PingHistory");

                count = migration.MigratePingStatistics();
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} PingStatistics");

                count = migration.MigrateRantTypes();
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} RantTypes");

                count = migration.MigrateRantMessages();
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} RantMessages");

                count = migration.MigrateSavedMessages();
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} SavedMessages");

                count = migration.MigratePlaceBoardPerformanceInfos();
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} PlaceBoardPerformanceInfos");

                count = migration.MigratePlaceBoardPixels(textChannel);
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} PlaceBoardPixels");

                count = migration.MigratePlaceBoardDiscordUsers();
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} PlaceBoardDiscordUsers");

                count = migration.MigratePlaceBoardPixelHistory(textChannel);
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} PlaceBoardPixelHistory"); // (SKIPED) // TODO Convert snowflake id to datetime

                count = migration.MigrateSubredditInfos();
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} SubredditInfos");

                count = migration.MigrateRedditPosts(textChannel);
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} RedditPosts");

                count = migration.MigrateRedditImages(textChannel);
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} RedditImages");

                count = migration.MigrateBotChannelSettings();
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} BotChannelSettings");


                /*count = migration.MigrateDiscordMessages(textChannel);
                textChannel.SendMessageAsync($"Migrated {count.ToString("N0")} DiscordMessagess");
                */

                textChannel.SendMessageAsync($"Migration done. Releasing DB.");
            }
            catch (Exception ex)
            {
                textChannel.SendMessageAsync(ex.ToString());
            }

            TempDisableIncomming = false;

            return Task.CompletedTask;
        }

        private Task Client_RoleCreated(SocketRole arg)
        {
            DiscordHelper.ReloadRoles(arg.Guild);

            return Task.CompletedTask;
            //throw new NotImplementedException();
        }

        // to find ghost pings
        private Task Client_MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel arg3)
        {
            return Task.CompletedTask;
            // TODO check EditedTimestamp if first edit

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
            return Task.CompletedTask;
            if (TempDisableIncomming)
                return Task.CompletedTask;

            if (message.HasValue)
            {
                IMessage messageValue = message.Value;

                if (!AllowedToRun(message.Value.Channel.Id, BotPermissionType.RemovedPingMessage))
                    return Task.CompletedTask;

                if (message.Value.Tags?.Where(i => i.Type == TagType.UserMention || i.Type == TagType.RoleMention).Count() > 0)
                {
                    if (message.Value.Content.StartsWith("$q"))
                        return Task.CompletedTask; // exclude quotes

                    if (message.Value.CreatedAt.UtcDateTime < DateTime.UtcNow.AddSeconds(-30))
                        return Task.CompletedTask; // only track for first 30 secs

                    EmbedBuilder builder = new EmbedBuilder();
                    var guildUser = message.Value.Author as SocketGuildUser;
                    builder.WithTitle($"{guildUser.Nickname} is a really bad person because he deleted a message with a ping");

                    string messageText = "";
                    foreach (var item in message.Value.Tags.Where(i => i.Type == TagType.UserMention || i.Type == TagType.RoleMention))
                    {
                        string pefixForRole = item.Type == TagType.RoleMention ? "&" : "";
                        messageText += $"Poor <@{pefixForRole}{item.Key}>" + Environment.NewLine;
                    }

                    messageText += Environment.NewLine;
                    messageText += $"{guildUser.Nickname} you just got a new role <:yay:778745219733520426> Next time dont delete ghost pings" + Environment.NewLine;

                    var socketChannel = messageValue.Channel as SocketGuildChannel;

                    AssignMutedRoleToUser(socketChannel.Guild, guildUser);

                    builder.WithDescription(messageText);
                    builder.WithColor(255, 64, 128);

                    builder.WithAuthor(message.Value.Author);

                    builder.WithCurrentTimestamp();

                    message.Value.Channel.SendMessageAsync("", false, builder.Build());
                }
            }

            return Task.CompletedTask;
        }

        private async void AssignMutedRoleToUser(SocketGuild guild, SocketGuildUser user)
        {
            /*
            try
            {
                var role = guild.Roles.FirstOrDefault(x => x.Name == "STFU"); //765542118701400134
                await (user as IGuildUser).AddRoleAsync(role);

                Thread.Sleep(TimeSpan.FromMinutes(15));

                await (user as IGuildUser).RemoveRoleAsync(role);
            }
            catch (Exception ex)
            {

            }*/
        }

        private async void HandleReaction(Cacheable<IUserMessage, ulong> argMessage, ISocketMessageChannel argMessageChannel, SocketReaction argReaction, bool addedReaction)
        {
            try
            {
                if (TempDisableIncomming)
                    return;

                IMessage currentMessage = null;
                if (argMessage.HasValue)
                {
                    currentMessage = argMessage.Value;
                }
                else
                {
                    currentMessage = await argMessageChannel.GetMessageAsync(argMessage.Id);
                }

                var channelSettings = BotChannelSettings?.SingleOrDefault(i => i.DiscordChannelId == argMessageChannel.Id);

                ReactionHandler reactionHandler = new ReactionHandler(currentMessage, argReaction, channelSettings, addedReaction);
                reactionHandler.Run();
            }
            catch (Exception ex)
            {

            }
        }
        private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> argMessage, ISocketMessageChannel argMessageChannel, SocketReaction argReaction)
        {
            HandleReaction(argMessage, argMessageChannel, argReaction, true);
        }

        private async Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> argMessage, ISocketMessageChannel argMessageChannel, SocketReaction argReaction)
        {
            HandleReaction(argMessage, argMessageChannel, argReaction, false);
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


        private static Dictionary<ulong, DateTime> SpamCache = new Dictionary<ulong, DateTime>();

        public static bool SendedAnWChallenge = false;
        public async void NewAnWChallenge()
        {
            var anwChannel = Client.GetGuild(747752542741725244).GetTextChannel(772551551818268702);

            // todo do more dynamic
            /*
            string name = "Count the Divisors";
            string due = "Thursday, March 11, 2021 10:00:59 AM GMT+01:00";
            int exp = 100;

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle($"A new AnW challenge has been uploaded");
            builder.WithColor(128, 255, 128);
            builder.WithDescription($"Task Name: **{name}** " + Environment.NewLine + $"Due date: {due}" + Environment.NewLine + $"EXP: {exp}");

            builder.WithCurrentTimestamp();

            await anwChannel.SendMessageAsync("May the fastest speedrunner win :)", false, builder.Build());*/
        }

        public async Task HandleCommandAsync(SocketMessage m)
        {
            if (TempDisableIncomming)
                return;

            if (m is not SocketUserMessage msg) return;

            if (msg.Channel is not SocketGuildChannel guildChannel)
            {
                // no DM parsing for now (maybe delete saved post) in the future
                return;
            }

            // check if the emote is a command -> block
            List<CommandInfo> commandList = commands.Commands.ToList();



            // ignore this channel -> high msg volume
            if (msg.Channel.Id != 819966095070330950)
            {
                var channelSettings = BotChannelSettings?.SingleOrDefault(i => i.DiscordChannelId == msg.Channel.Id);

                MessageHandler msgHandler = new MessageHandler(msg, commandList, channelSettings);
                await msgHandler.Run();


                //if (!m.Author.IsBot && !commandList.Any(i => i.Name.ToLower() == msg.Content.ToLower().Replace(".", "")) && await TryToParseEmoji(msg))
                //    return; // emoji was found and we can exit here

                // TODO private channels

                var user = (SocketGuildUser)msg.Author;

                var dbManager = DatabaseManager.Instance();

                // todo do this in the future as scheduler

                var openUtcDate = new DateTime(2021, 03, 04, 17, 0, 0, DateTimeKind.Utc);

                if (!SendedAnWChallenge && (openUtcDate < DateTime.UtcNow) && (openUtcDate.AddMinutes(5) > DateTime.UtcNow)/*to prevent on restart that it sends again*/)
                {
                    SendedAnWChallenge = true;
                    NewAnWChallenge();
                }

                // Use discord snowflake
                // TODO may cause problems if the bot is hosted in a timezone that doesnt switch to daylight at the same time as the hosting region
                var timeNow = SnowflakeUtils.FromSnowflake(m.Id).AddHours(TimeZoneInfo.IsDaylightSavingTime(DateTime.Now) ? 2 : 1); // CEST CONVERSION

                if (LastNewDailyMessagePost.Day != timeNow.Day && !user.IsBot)
                {
                    // Reset time 
                    LastNewDailyMessagePost = DateTime.UtcNow.AddHours(TimeZoneInfo.IsDaylightSavingTime(DateTime.Now) ? 2 : 1);

                    // This person is the first one to post a new message

                    var firstPoster = dbManager.GetDiscordUserById(msg.Author.Id);
                    dbManager.UpdateDiscordUser(new ETHBot.DataLayer.Data.Discord.DiscordUser()
                    {
                        DiscordUserId = user.Id,
                        DiscriminatorValue = user.DiscriminatorValue,
                        AvatarUrl = user.GetAvatarUrl(),
                        IsBot = user.IsBot,
                        IsWebhook = user.IsWebhook,
                        Nickname = user.Nickname,
                        Username = user.Username,
                        JoinedAt = user.JoinedAt,
                        FirstDailyPostCount = firstPoster.FirstDailyPostCount + 1
                    });


                    EmbedBuilder builder = new EmbedBuilder();

                    builder.WithTitle($"{firstPoster.Nickname ?? firstPoster.Username} IS THE FIRST POSTER TODAY");
                    builder.WithColor(0, 0, 255);
                    builder.WithDescription($"This is the {firstPoster.FirstDailyPostCount + 1}. time you are the first poster of the day");

                    builder.WithAuthor(msg.Author);
                    builder.WithCurrentTimestamp();

                    List<string> randomGifs = new List<string>()
                    {
                        "https://tenor.com/view/confetti-hooray-yay-celebration-party-gif-11214428",
                        "https://tenor.com/view/qoobee-agapi-confetti-surprise-celebrate-gif-11679728",
                        "https://tenor.com/view/confetti-celebrate-colorful-celebration-gif-15816997",
                        "https://tenor.com/view/celebrate-awesome-yay-confetti-party-gif-8571772",
                        "https://tenor.com/view/mao-mao-cat-hurrah-confetti-gif-9948046",
                        "https://tenor.com/view/stop-it-oh-spongebob-confetti-gif-13772176",
                        "https://tenor.com/view/win-confetti-gif-5026830",
                        "https://tenor.com/view/kawaii-confetti-happiness-confetti-gif-11981055",
                        "https://tenor.com/view/wow-fireworks-3d-gifs-artist-woohoo-gif-18062148"
                    };

                    string randomGif = randomGifs[new Random().Next(randomGifs.Count)];
                    await m.Channel.SendMessageAsync(randomGif);
                    await m.Channel.SendMessageAsync("", false, builder.Build());

                    // run it only once a day // todo find better scheduler
                    DiscordHelper.ReloadRoles(user.Guild);
                }

                //Discord.Image img = new Discord.Image(new Stream()); // stream
                //await user.Guild.ModifyAsync(msg => msg.Banner = img);

                try
                {
                    await LogManager.ProcessEmojisAndPings(m.Tags, m.Author.Id, m.Id, (SocketGuildUser)m.Author);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }



            if (!(m.Channel.Id == 819966095070330950 && AllowedBotCommands.Any(i => !m.Content.StartsWith(i))))
                if (m.Author.IsBot) // make exception for place command
                    return;

            /* disabled for now */
            /*if (user.Roles.Any(i => i.Id == 798639212818726952) && false )
            {
                HandleQuestionAnswer(msg);
            }*/

            int argPos = 0;

            // accept .dev only in dev mode

            if (!msg.HasStringPrefix(CurrentPrefix, ref argPos))
                return;

            if (!m.Author.IsBot && m.Author.Id != Owner)
            {
                if (SpamCache.ContainsKey(m.Author.Id))
                {
                    if (SpamCache[m.Author.Id] > DateTime.Now.AddMilliseconds(-500))
                    {
                        SpamCache[m.Author.Id] = SpamCache[m.Author.Id].AddMilliseconds(750);

                        // TODO save last no spam message time
                        if (new Random().Next(0, 5) == 0)
                        {
                            // Ignore the user than to reply takes 1 message away from the rate limit
                            m.Channel.SendMessageAsync($"Stop spamming <@{m.Author.Id}> your current timeout is {SpamCache[m.Author.Id]} UTC");
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


            var context = new SocketCommandContext(Client, msg);
            commands.ExecuteAsync(context, argPos, services);
        }


        // TODO move to message handler
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
    }
}
