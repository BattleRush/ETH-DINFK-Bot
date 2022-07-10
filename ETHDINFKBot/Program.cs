using Discord;
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
using System.Runtime.InteropServices;
using System.Diagnostics;
using Discord.Net;
using ETHDINFKBot.Classes;
using Discord.Interactions;

namespace ETHDINFKBot
{
    class PlaceServer : TcpServer
    {
        public PlaceServer(IPAddress address, int port) : base(address, port) { }

        protected override TcpSession CreateSession() { return new PlaceSession(this); }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat TCP server caught an error with code {error}");
        }
    }

    class Program
    {
        public static DiscordSocketClient Client;
        private CommandService Commands;

        private IServiceProvider services;
        private static InteractionService _interactionService;
        private static IConfiguration Configuration;

        public static long TotalEmotes { get; set; }

        public static string CurrentPrefix { get; set; }

        // TODO Move settings to an object
        public static bool TempDisableIncoming { get; set; }

        public static Dictionary<ulong, Question> CurrentActiveQuestion = new Dictionary<ulong, Question>();
        public static Dictionary<ulong, DateTime> CurrentDiscordOutOfJailTime = new Dictionary<ulong, DateTime>();

        public static TimeZoneInfo TimeZoneInfo = TZConvert.GetTimeZoneInfo("Europe/Zurich");
        public static ILoggerFactory Logger { get; set; }

        private static DateTime LastNewDailyMessagePost = DateTime.Now;
        private static DateTime LastAfternoonMessagePost = DateTime.Now;

        //private static List<BotChannelSetting> BotChannelSettings;

        private static List<string> AllowedBotCommands = new List<string>() { CurrentPrefix + "place setpixel ", CurrentPrefix + "place pixelverify " };
        private static List<ulong> PlaceChannels = new List<ulong>() { 819966095070330950, 955751651942211604 };

        //public static WebSocketServer PlaceWebsocket;
        public static PlaceServer PlaceServer;


        // Used for restoring channel ordering (TODO Maybe move that info into the DB?)
        public static List<ChannelOrderInfo> ChannelPositions = new List<ChannelOrderInfo>();


        private DatabaseManager DatabaseManager = DatabaseManager.Instance();
        private LogManager LogManager = new LogManager(DatabaseManager.Instance());

        public static BotSetting BotSetting;
        public static IHost Host;

        public static ApplicationSetting ApplicationSetting;

        public static List<string> CommandNames { get; set; }
        private static ServiceProvider Services;
        static void Main(string[] args)
        {
            CurrentPrefix = ".";

#if DEBUG
            CurrentPrefix = "dev.";
#endif

            try
            {
                // TODO may cause problems if the bot is hosted in a timezone that doesnt switch to daylight at the same time as the hosting region
                LastNewDailyMessagePost = DateTime.UtcNow.AddHours(TimeZoneInfo.IsDaylightSavingTime(DateTime.Now) ? 2 : 1);

                LastAfternoonMessagePost = DateTime.UtcNow.AddHours(TimeZoneInfo.IsDaylightSavingTime(DateTime.Now) ? 2 : 1).AddHours(-12); // shift left by 12h

                Logger = LoggerFactory.Create(builder => { builder.AddConsole(); });

                BotSetting = DatabaseManager.Instance().GetBotSettings();

                // https://crontab.guru/

                Host = new HostBuilder()
                   .ConfigureServices((hostContext, services) =>
                   {
                       // TODO read from DB

                       //services.AddCronJob<CronJobTest>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = @"* * * * *"; });

                       // once a day at 1 or 2 AM CET/CEST
                       services.AddCronJob<DailyCleanup>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = @"50 * * * *"; }); // Changed to every hour at 30 mins

                       // TODO adjust for summer time in CET/CEST
                       services.AddCronJob<DailyStatsJob>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = @"1 23 * * *"; });

                       // TODO adjust for summer time in CET/CEST
                       // TODO Enable for Maria DB
                       //services.AddCronJob<PreloadJob>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = @"0 3 * * *"; });// 3 am utc -> 4 am cet Disable until the permission tree is fully reworked

                       // TODO adjust for summer time in CET/CEST
                       services.AddCronJob<SpaceXSubredditJob>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = BotSetting?.SpaceXSubredditCheckCronJob ?? "*/10 * * * *"; }); //BotSetting.SpaceXSubredditCheckCronJob "*/ 10 * * * *"

                       // TODO adjust for summer time in CET/CEST
                       services.AddCronJob<StartAllSubredditsJobs>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = @"0 4 * * *"; });// 4 am utc -> 5 am cet

                       // TODO adjust for summer time in CET/CEST
                       //services.AddCronJob<GitPullMessageJob>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = @"0 21 * * TUE"; });// 22 CET each Tuesday


                       // TODO adjust for summer time in CET/CEST
                       //services.AddCronJob<JWSTUpdates>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = @"*/5 * * * *"; });// Check each 5 mins

                       //// For easier find for the manual trigger
                       //services.AddByName<IHostedService>()
                       //  .Add<DailyCleanup>("DailyCleanup")
                       //  .Add<DailyStatsJob>("DailyStatsJob")
                       //  .Add<PreloadJob>("PreloadJob")
                       //  .Add<SpaceXSubredditJob>("SpaceXSubredditJob")
                       //  .Add<StartAllSubredditsJobs>("StartAllSubredditsJobs")
                       //  .Add<GitPullMessageJob>("GitPullMessageJob")
                       //  .Build();

                       // TODO adjust for summer time in CET/CEST
                       //services.AddCronJob<BackupDBJob>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = @"0 4 * * *"; });// 0 am utc
                   })
                   .Build();

                Host.StartAsync();

                // TODO check if HostBuilder Faulted -> likely wrong cron job implementation

                Configuration = new ConfigurationBuilder()
                  .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                  .Build();

                ApplicationSetting = new ApplicationSetting()
                {
                    DiscordToken = Configuration["DiscordToken"],
                    Owner = Convert.ToUInt64(Configuration["Owner"]),
                    BaseGuild = Convert.ToUInt64(Configuration["BaseGuild"]),

                    BasePath = Configuration["BasePath"],

                    // TODO Fix app setting name for these 3
                    MariaDBFullUserName = Configuration["MariaDB_FullUserName"],
                    MariaDBReadOnlyUserName = Configuration["MariaDB_ReadOnlyUserName"],
                    MariaDBName = Configuration["MariaDB_DBName"],

                    ConnectionStringsSetting = new ConnectionStringsSetting()
                    {
                        ConnectionString_Full = Configuration.GetConnectionString("ConnectionString_Full").ToString(),
                        ConnectionString_ReadOnly = Configuration.GetConnectionString("ConnectionString_ReadOnly").ToString()
                    },

                    CDNPath = Configuration["CDNPath"],
                    CertFilePath = Configuration["CertFilePath"],
                    FFMpegPath = Configuration["FFMpegPath"],

                    RedditSetting = new RedditSetting()
                    {
                        AppId = Configuration["Reddit:AppId"],
                        RefreshToken = Configuration["Reddit:RefreshToken"],
                        AppSecret = Configuration["Reddit:AppSecret"],
                    },

                    PostgreSQLSetting = new PostgreSQLSetting()
                    {
                        Host = Configuration["PostgreSQL:Host"],
                        Port = Convert.ToInt32(Configuration["PostgreSQL:Port"]),
                        OwnerUsername = Configuration["PostgreSQL:OwnerUsername"],
                        OwnerPassword = Configuration["PostgreSQL:OwnerPassword"],
                        DMDBUserUsername = Configuration["PostgreSQL:DMDBUserUsername"],
                        DMDBUserPassword = Configuration["PostgreSQL:DMDBUserPassword"]
                    }
                };



                //;

                // TODO Update for new connection strings and dev/prod

                //MariaDBReadOnlyConnectionString = ;
                //FULL_MariaDBReadOnlyConnectionString = ;



                //RedditAppId = Configuration["Reddit:AppId"];
                //RedditRefreshToken = Configuration["Reddit:RefreshToken"];
                //RedditAppSecret = Configuration["Reddit:AppSecret"];

                //Settings.FFMpegPath = Configuration["FFMpegPath"];

                //BackupDBOnStartup();

                new Program().MainAsync(ApplicationSetting.DiscordToken).GetAwaiter().GetResult();
            }
            catch (BadImageFormatException bife)
            {
                // In this case the update is running and the process loaded a half uploaded dll
                // -> RESTART

                Thread.Sleep(5000);
                Process.GetCurrentProcess().Kill();
            }
            catch (Exception ex)
            {
                Console.WriteLine("CRITICAL ERROR: " + ex.ToString());
            }
        }

        public static void BackupDBOnStartup()
        {
            var path = Path.Combine(ApplicationSetting.BasePath, "Database", "ETHBot.db");
            if (File.Exists(path))
            {
                var backupPath = Path.Combine(ApplicationSetting.BasePath, "Database", "Backup");
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

                var path2 = Path.Combine(ApplicationSetting.BasePath, "Database", "Backup", $"ETHBot_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                File.Copy(path, path2);
            }
        }

        public static void LoadChannelSettings()
        {
            // TODO load settings into cache
            //BotChannelSettings = DatabaseManager.Instance().GetAllChannelSettings();
        }

        public async Task MainAsync(string token)
        {
            // TODO MOVE WEBSOCKET STUFF

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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
#if !DEBUG
                string www = "/var/www/wss";
                try
                {
                /*
                    //string www = @"C:\Temp\wss";
                    // Create and prepare a new SSL server context
                    var context = new SslContext(SslProtocols.Tls12, new X509Certificate2(Path.Combine(Configuration["CertFilePath"], "battlerush.dev.pfx")));
                    //var context = new SslContext(SslProtocols.Tls12);
                    // Create a new WebSocket server
                    PlaceServer = new PlaceServer(context, IPAddress.Any, 9000);
                    PlaceServer.AddStaticContent(www, "/place");

                    PlaceServer.OptionKeepAlive = true;*/

                    PlaceServer = new PlaceServer(IPAddress.Any, 9000);
                    PlaceServer.OptionKeepAlive = true;
                    PlaceServer.OptionAcceptorBacklog = 8192;
                    PlaceServer.OptionSendBufferSize = 10_000_000;

                    // Start the server
                    Console.Write("Server starting...");
                    PlaceServer.Start();
                    Console.WriteLine("Done!");
                }
                catch (Exception ex)
                {
                    Console.Write("Error while starting WS: " + ex.ToString());
                }
#endif
            }
            else
            {
                try
                {

                    //string www = @"C:\Temp\wss";
                    // Create and prepare a new SSL server context
                    //var context = new SslContext(SslProtocols.Tls12, new X509Certificate2(Path.Combine(Configuration["CertFilePath"], "battlerush.dev.pfx")));
                    var context = new SslContext(SslProtocols.Tls12);
                    // Create a new WebSocket server
                    PlaceServer = new PlaceServer(IPAddress.Any, 9000);
                    PlaceServer.OptionKeepAlive = true;
                    PlaceServer.OptionAcceptorBacklog = 8192;
                    //PlaceServer.OptionNoDelay = true;

                    //PlaceServer.AddStaticContent(www, "/place");

                    // Start the server
                    Console.Write("Server starting...");
                    PlaceServer.Start();
                    Console.WriteLine("Done!");
                }
                catch (Exception ex)
                {
                    Console.Write("Error while starting WS: " + ex.ToString());
                }
            }



            DatabaseManager.Instance().SetAllSubredditsStatus();
            LoadChannelSettings();

            var config = new DiscordSocketConfig
            {
                MessageCacheSize = 250,
                AlwaysDownloadUsers = true,
                GatewayIntents = GatewayIntents.All
            };

            Client = new DiscordSocketClient(config);

            Client.MessageReceived += HandleCommandAsync;
            Client.ReactionAdded += Client_ReactionAdded;
            Client.ReactionRemoved += Client_ReactionRemoved;
            Client.MessageDeleted += Client_MessageDeleted;
            Client.MessageUpdated += Client_MessageUpdated;
            Client.RoleCreated += Client_RoleCreated;
            Client.Ready += Client_Ready;

            Client.ChannelUpdated += Client_ChannelUpdated;
            Client.ChannelCreated += Client_ChannelCreated;
            Client.ChannelDestroyed += Client_ChannelDestroyed;
            Client.JoinedGuild += Client_JoinedGuild;
            Client.LeftGuild += Client_LeftGuild;

            Client.Log += Client_Log;

            // For message commands
            Client.MessageCommandExecuted += MessageCommandHandler;

            // For user commands
            Client.UserCommandExecuted += UserCommandHandler;

            //Client.ButtonExecuted += Client_ButtonExecuted;
            //Client.SlashCommandExecuted += Client_SlashCommandExecuted;

            Client.ModalSubmitted += Client_ModalSubmitted;
            Client.InteractionCreated += Client_InteractionCreated;

            Services = new ServiceCollection()
                .AddSingleton(Client)
                //.AddSingleton<InteractiveService>()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                //.AddSingleton<CommandHandler>()
                .BuildServiceProvider();

            Commands = new CommandService();
            await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), Services);

            CommandNames = new List<string>();

            List<string> buildCommandString(Discord.Commands.ModuleInfo module)
            {
                List<string> returnInfo = new List<string>();

                foreach (var command in module.Commands)
                    returnInfo.Add(command.Aliases.First());

                foreach (var subModule in module.Submodules)
                    returnInfo.AddRange(buildCommandString(subModule));

                return returnInfo;
            };

            foreach (var module in Commands.Modules.Where(i => !i.IsSubmodule))
                CommandNames.AddRange(buildCommandString(module));

            PlaceMultipixelHandler multipixelHandler = new PlaceMultipixelHandler();
            multipixelHandler.MultiPixelProcess();


            // Here we can initialize the service that will register and execute our commands
            //await Services.GetRequiredService<CommandHandler>().InitializeAsync();

            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();

#if DEBUG
            await Client.SetGameAsync($"DEV MODE");
#else
            //await Client.SetGameAsync($"with a neko");
            TotalEmotes = DatabaseManager.EmoteDatabaseManager.TotalEmoteCount();
            await Client.SetGameAsync($"{TotalEmotes} emotes", null, ActivityType.Watching);
#endif

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }


        // TODO flag in db the users
        private Task Client_LeftGuild(SocketGuild arg)
        {
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }

        private Task Client_JoinedGuild(SocketGuild arg)
        {
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }

        private async Task Client_InteractionCreated(SocketInteraction arg)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
                var ctx = new SocketInteractionContext(Client, arg);
                await _interactionService.ExecuteCommandAsync(ctx, Services);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (arg.Type == InteractionType.ApplicationCommand)
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        private Task Client_ModalSubmitted(SocketModal arg)
        {
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }

        private Task Client_SlashCommandExecuted(SocketSlashCommand arg)
        {
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }

        private async Task Client_ButtonExecuted(SocketMessageComponent arg)
        {
            return;
            ButtonHandler buttonHandler = new ButtonHandler(arg);
            var result = await buttonHandler.Run();
            await arg.DeferAsync(false);
        }

        public async Task MessageCommandHandler(SocketMessageCommand arg)
        {
            MessageCommandHandler mch = new MessageCommandHandler(arg);
            await mch.Run();

            await arg.RespondAsync("Requested " + arg.CommandName, null, false, true);
        }

        public async Task UserCommandHandler(SocketUserCommand arg)
        {
            UserCommandHandler uch = new UserCommandHandler(arg);
            var result = await uch.Run();

            if (result)
                await arg.RespondAsync("Requested " + arg.CommandName, null, false, true);
            else
                await arg.RespondAsync("Requested " + arg.CommandName + " failed. Likely you called the function in a channel the bot doesnt have permission to send this feature or an exception happened.", null, false, true);
        }

        private void ReloadChannelPositionLock(SocketGuild guild, bool delete, string channelName)
        {
            ulong adminBotChannel = 747768907992924192;

#if DEBUG
            adminBotChannel = 774286694794919989;
#endif
            // list should always be empty
            ChannelPositions = new List<ChannelOrderInfo>();

            // Any channels outside of categories considered?
            foreach (var category in guild.CategoryChannels)
                foreach (var channel in category.Channels)
                    ChannelPositions.Add(new ChannelOrderInfo() { ChannelId = channel.Id, ChannelName = channel.Name, CategoryId = category.Id, CategoryName = category.Name, Position = channel.Position });

            //var textChannel = guild.GetTextChannel(adminBotChannel);
            //textChannel.SendMessageAsync($"Global Channel Position lock has been updated. Reason: Channel {channelName} got {(delete ? "deleted" : "added")}.");
        }

        private Task Client_ChannelDestroyed(SocketChannel channel)
        {
            ReloadLock(true, channel);
            return Task.CompletedTask;
        }

        private Task Client_ChannelCreated(SocketChannel channel)
        {
            ReloadLock(false, channel);
            return Task.CompletedTask;
        }

        private void ReloadLock(bool delete, SocketChannel channel)
        {
            var keyValueDBManager = DatabaseManager.KeyValueManager;
            var isLockEnabled = keyValueDBManager.Get<bool>("LockChannelPositions");

            if (channel is SocketGuildChannel guildChannel)
            {
                ulong guildId = Program.ApplicationSetting.BaseGuild;

#if DEBUG
                guildId = 774286694794919986;
#endif

                var botSettings = DatabaseManager.Instance().GetBotSettings();

                if (guildChannel.Guild.Id == guildId && isLockEnabled)
                {
                    ReloadChannelPositionLock(Client.GetGuild(guildId), delete, guildChannel.Name);
                }
            }
        }

        private static bool Reordering = false;
        private static DateTime LastReorderTrigger = DateTime.MinValue;
        List<string> ChannelMoveDetections = new List<string>();
        private async Task<bool> EnforceChannelPositions(ulong guildId, ulong textChannelId)
        {
            if (LastReorderTrigger > DateTime.Now.AddSeconds(-5))
                return false; // last reorder happened less than 5 sec ago skip

            LastReorderTrigger = DateTime.Now;

            // Wait 1 second to receive all new orders
            await Task.Delay(TimeSpan.FromSeconds(5));

            Reordering = true;

            var categoryChannels = Client.GetGuild(guildId).CategoryChannels;
            var guild = Program.Client.GetGuild(guildId);
            var textChannel = guild.GetTextChannel(textChannelId);

            if (ChannelMoveDetections.Count > 0)
            {
                // TODO Detect 2k chars -> page
                string infoMove = $"**Move detected**{Environment.NewLine}" + string.Join(Environment.NewLine, ChannelMoveDetections);
                await textChannel.SendMessageAsync(infoMove.Substring(0, Math.Min(1970, infoMove.Length)));

                ChannelMoveDetections = new List<string>();
            }

            bool updated = false;

            string info = $"**New channel order applied**{Environment.NewLine}";

            try
            {

                foreach (var categoryChannel in categoryChannels.OrderBy(i => i.Position))
                {
                    // We need to enforce this order
                    var channelInfos = ChannelPositions.Where(i => i.CategoryId == categoryChannel.Id).OrderBy(i => i.Position).ToArray();
                    var channels = categoryChannel.Channels.OrderBy(i => i.Position);

                    // we enforce for 
                    bool anyChange = false;

                    int lastChannelPosition = categoryChannel.Position; // Default from the position the category is at

                    info += $"    Enforced order for {categoryChannel.Name}{Environment.NewLine}";

                    for (int i = 0; i < channelInfos.Count(); i++)
                    {
                        if (channels.Count() != channelInfos.Length)
                        {
                            await textChannel.SendMessageAsync($"**Someone really fatfingered this time. It looks like some channel moved categories. I wont fix this. Check the FIRST Move detected entry. This is likely the offender** {Environment.NewLine}" +
                                $"Come on, why do you make my life that difficult <:pepegun:851456702973083728>");

                            var missingChannels = channelInfos.Where(i => !channels.Any(j => j.Id == i.ChannelId));
                            var additionalChannels = channels.Where(i => !channelInfos.Any(j => j.ChannelId == i.Id));

                            if (missingChannels.Count() > 0)
                                await textChannel.SendMessageAsync($"**Missing** channels for category <#{categoryChannel.Id}>: {string.Join(", ", missingChannels.Select(i => "<#" + i.ChannelId + ">"))}");

                            if (additionalChannels.Count() > 0)
                                await textChannel.SendMessageAsync($"**Additional** channels for category <#{categoryChannel.Id}>: {string.Join(", ", additionalChannels.Select(i => "<#" + i.Id + ">"))}");

                            Reordering = false;
                            return false;
                        }

                        var currentChannelInfo = channelInfos[i];
                        var channel = channels.ElementAt(i);

                        if (currentChannelInfo.ChannelId == channel.Id && !anyChange)
                        {
                            // The order is fine -> Ignore the id because Discord will screw with us no matter what
                            lastChannelPosition = channel.Position;
                            continue;
                        }
                        else
                        {
                            // Either the channel is now what we expected or the some channel before got wrong order -> enforce for all remaining channels the positions
                            anyChange = true;
                            updated = true;
                            lastChannelPosition++;

                            channel = channels.SingleOrDefault(i => i.Id == currentChannelInfo.ChannelId);
                            if (channel != null)
                            {
                                info += $"        {channel.Name} from position {channel.Position} to {lastChannelPosition}{Environment.NewLine}";
                                await channel.ModifyAsync(c => c.Position = lastChannelPosition);
                            }
                            else
                            {
                                // TODO error?
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {

            }

            // TODO make sure its not over 2k chars -> Paging
            if (updated)
                await textChannel.SendMessageAsync(info.Substring(0, Math.Min(2000, info.Length)));

            Reordering = false;

            return false;

            /*
             * 
             *                     
             *                     
             *                     
             * // Detect if the order is correct
                    if (ChannelPositions.ContainsKey(newGuildChannel.Id)
                        && ChannelPositions[newGuildChannel.Id] != newGuildChannel.Position)
                    {
                        // Used when some channel has been reordered to restore the order back
                        var currentBotSettings = DatabaseManager.GetBotSettings();

                        // TODO Setting
                        var guild = Program.Client.GetGuild(guildId);

                        var textChannel = guild.GetTextChannel(adminBotChannel);
                        //textChannel.SendMessageAsync();

                        if (currentBotSettings.ChannelOrderLocked)
                        {
                            // only reorder if setting active
                            newGuildChannel.ModifyAsync(c => c.Position = ChannelPositions[newGuildChannel.Id]);

                            textChannel.SendMessageAsync($"Reordered {newGuildChannel.Name} from Position {originalGuildChannel.Position} to {newGuildChannel.Position}");
                        }
                    }

            */
        }

        private Task Client_ChannelUpdated(SocketChannel originalChannel, SocketChannel newChannel)
        {
            var keyValueDBManager = DatabaseManager.KeyValueManager;
            var isLockEnabled = keyValueDBManager.Get<bool>("LockChannelPositions");

            if (!isLockEnabled)
                return Task.CompletedTask;

            if (originalChannel is SocketGuildChannel originalGuildChannel
                && newChannel is SocketGuildChannel newGuildChannel)
            {
                ulong guildId = Program.ApplicationSetting.BaseGuild;
                ulong adminBotChannel = 747768907992924192;

#if DEBUG
                guildId = 774286694794919986;
                adminBotChannel = 774286694794919989;
#endif

                // only for 1 specific server
                if (originalGuildChannel.Guild.Id != guildId)
                    return Task.CompletedTask;

                if (originalGuildChannel.Position != newGuildChannel.Position)
                {
                    // ORDER CHANGED
                    var guild = Program.Client.GetGuild(guildId);

                    var textChannel = guild.GetTextChannel(adminBotChannel);

                    EnforceChannelPositions(guildId, adminBotChannel);

                    if (!Reordering)
                        ChannelMoveDetections.Add($"    {newGuildChannel.Name} move from position {originalGuildChannel.Position} to {newGuildChannel.Position}");
                }
            }

            return Task.CompletedTask;
        }

        private Task Client_Log(LogMessage arg)
        {
            if (arg.Severity == LogSeverity.Error)
            {
                if (arg.Exception is BadImageFormatException)
                {
                    // In this case the update is running and the process loaded a half uploaded dll
                    // -> RESTART
                    Thread.Sleep(1000);
                    Process.GetCurrentProcess().Kill();
                }
            }
#if DEBUG
            Console.WriteLine("Discord log: " + arg.Message);
#else
            if (arg.Severity == LogSeverity.Error || arg.Severity == LogSeverity.Critical)
                Console.Write("Discord log: " + arg.Message);
#endif

            return Task.CompletedTask;
        }


        //https://www.gngrninja.com/code/2019/4/1/c-discord-bot-command-handling
        //public async Task InitializeAsync()
        //{
        //    // register modules that are public and inherit ModuleBase<T>.
        //    await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        //}




        private static async Task SetUpApplicationCommands()
        {
            ulong guildId = 774286694794919986;

#if !DEBUG
            guildId = Program.ApplicationSetting.BaseGuild;
#endif
            // Let's build a guild command! We're going to need a guild so lets just put that in a variable.
            var guild = Client.GetGuild(guildId);

            // Next, lets create our user and message command builder. This is like the embed builder but for context menu commands.
            var guildUserCommand = new UserCommandBuilder();
            guildUserCommand.WithName("User's last pings");


            // Note: Names have to be all lowercase and match the regular expression ^[\w -]{3,32}$
            var guildMessageCommand = new MessageCommandBuilder();
            guildMessageCommand.WithName("Save Message");


            var guildMessageCommand2 = new MessageCommandBuilder();
            guildMessageCommand2.WithName("Save Message2");

            try
            {
                // Now that we have our builder, we can call the BulkOverwriteApplicationCommandAsync to make our context commands. Note: this will overwrite all your previous commands with this array.
                await guild.BulkOverwriteApplicationCommandAsync(new ApplicationCommandProperties[]
                {
                    guildUserCommand.Build(),
                    guildMessageCommand.Build()
                    //guildMessageCommand2.Build()
                });
            }
            catch (HttpException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }

            var commands = Services.GetRequiredService<InteractionService>();
            try
            {
                _interactionService = new InteractionService(Client);
                await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), Services);

                //var t = await commands.RegisterCommandsToGuildAsync(747752542741725244, true);
            }
            catch (Exception ex)
            {

            }

        }


        // TODO Cleanup -> Remove (migration only
        private Task Client_Ready()
        {
            SetUpApplicationCommands();

            ulong guildId = Program.ApplicationSetting.BaseGuild; // TODO Update
#if DEBUG
            guildId = 774286694794919986;
#endif

            var lastStartUp = DatabaseManager.Instance().GetLastBotStartUpTime();

            //ulong spamChannel = 768600365602963496;
            var guild = Program.Client.GetGuild(guildId);

            var textChannel = guild.GetTextChannel(DiscordHelper.DiscordChannels["spam"]);
            if (textChannel != null)
                textChannel.SendMessageAsync($"Restarted with Branch: {ThisAssembly.Git.Branch} and Commit: {ThisAssembly.Git.Commit}. Last Uptime was: {CommonHelper.ToReadableString(DateTime.Now - lastStartUp)} Bot client ready. <@{Program.ApplicationSetting.Owner}>");

            // Register bot startup time when bot is ready
            DatabaseManager.Instance().AddBotStartUp();

            // list should always be empty
            ChannelPositions = new List<ChannelOrderInfo>();

            // Any channels outside of categories considered?
            foreach (var category in guild.CategoryChannels)
                foreach (var channel in category.Channels)
                    ChannelPositions.Add(new ChannelOrderInfo() { ChannelId = channel.Id, ChannelName = channel.Name, CategoryId = category.Id, CategoryName = category.Name, Position = channel.Position });

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

            if (TempDisableIncoming)
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

        private Task Client_MessageDeleted(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel)
        {
            return Task.CompletedTask;
            /* if (TempDisableIncoming)
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

             return Task.CompletedTask;*/
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

        private async void HandleReaction(Cacheable<IUserMessage, ulong> argMessage, Cacheable<IMessageChannel, ulong> argMessageChannel, SocketReaction argReaction, bool addedReaction)
        {
            try
            {
                if (TempDisableIncoming)
                    return;

                IMessage currentMessage = null;
                if (argMessage.HasValue)
                {
                    currentMessage = argMessage.Value;
                }
                else
                {
                    // TODO Check argMessageChannel.Value has value
                    currentMessage = await argMessageChannel.Value.GetMessageAsync(argMessage.Id);
                }

                var channelSettings = CommonHelper.GetChannelSettingByChannelId(argMessageChannel.Id).Setting;

                ReactionHandler reactionHandler = new ReactionHandler(currentMessage, argReaction, channelSettings, addedReaction);
                reactionHandler.Run();
            }
            catch (Exception ex)
            {

            }
        }

        private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> argMessage, Cacheable<IMessageChannel, ulong> argMessageChannel, SocketReaction argReaction)
        {
            HandleReaction(argMessage, argMessageChannel, argReaction, true);
        }

        private async Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> argMessage, Cacheable<IMessageChannel, ulong> argMessageChannel, SocketReaction argReaction)
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

        // Because of the delay from discord there is a way that the "first daily" post arrives later than some other messages
        private static List<SocketMessage> FirstDailyPostsCandidates = new List<SocketMessage>();
        private static bool CollectFirstDailyPostMessages = false;
        public async void FirstDailyPost()
        {
            // only collisions happened on first daily post as there is less competition for first afternoon post
            CollectFirstDailyPostMessages = true;

            // Wait for 20 sec (10 before midnight and 10 after)
            await Task.Delay(TimeSpan.FromSeconds(20));

            // Prevent entries that were created before midnight
            SocketMessage firstMessage = null;

            do
            {
                firstMessage = FirstDailyPostsCandidates.Where(i => i.CreatedAt.AddHours(TimeZoneInfo.IsDaylightSavingTime(DateTime.Now) ? 2 : 1).Hour != 23).OrderBy(i => i.CreatedAt).FirstOrDefault();
                await Task.Delay(TimeSpan.FromSeconds(2)); // Check each 2 seconds if a new message arrived
            } while (firstMessage == null);

            // Disable collection of first daily post after one such post has been found
            CollectFirstDailyPostMessages = false;

            var timeNow = SnowflakeUtils.FromSnowflake(firstMessage.Id).AddHours(TimeZoneInfo.IsDaylightSavingTime(DateTime.Now) ? 2 : 1); // CEST CONVERSION

            var user = (SocketGuildUser)firstMessage.Author;

            var dbManager = DatabaseManager.Instance();

            var firstPoster = dbManager.GetDiscordUserById(firstMessage.Author.Id);
            dbManager.UpdateDiscordUser(new DiscordUser()
            {
                DiscordUserId = user.Id,
                DiscriminatorValue = user.DiscriminatorValue,
                AvatarUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(), // If user has no custom avatar load the url for the default avatar
                IsBot = user.IsBot,
                IsWebhook = user.IsWebhook,
                Nickname = user.Nickname,
                Username = user.Username,
                JoinedAt = user.JoinedAt,
                FirstDailyPostCount = firstPoster.FirstDailyPostCount + 1
            });


            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle($"{firstPoster.Nickname ?? firstPoster.Username} IS THE FIRST POSTER {((timeNow.Day == 1 && timeNow.Month == 1) ? $"OF {timeNow.Year}" : "TODAY")}");
            builder.WithColor(0, 0, 255);
            builder.WithDescription($"This is the {CommonHelper.DisplayWithSuffix(firstPoster.FirstDailyPostCount + 1)} time you are the first poster of the day. With {(timeNow - timeNow.Date).TotalMilliseconds.ToString("N0")}ms from midnight.");

            builder.WithAuthor(firstMessage.Author);
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

            int beforeMidnight = FirstDailyPostsCandidates.Count(i => i.CreatedAt.AddHours(TimeZoneInfo.IsDaylightSavingTime(DateTime.Now) ? 2 : 1).Hour == 23);
            int afterMidnight = FirstDailyPostsCandidates.Count() - beforeMidnight;

            int count = beforeMidnight * (-1);
            // TODO limit to maybe 10 max
            foreach (var item in FirstDailyPostsCandidates.OrderBy(i => i.Id))
            {
                if (item.Channel is SocketGuildChannel)
                {
                    var guildChannel = item.Channel as SocketGuildChannel;

                    string link = $"https://discord.com/channels/{guildChannel.Guild.Id}/{item.Channel.Id}/{item.Id}";
                    var postTime = SnowflakeUtils.FromSnowflake(item.Id).AddHours(TimeZoneInfo.IsDaylightSavingTime(DateTime.Now) ? 2 : 1);

                    string title = CommonHelper.DisplayWithSuffix(count);

                    if (count == 0)
                        title = "**WINNER!**";

                    if (count < 0)
                        title = "Too early";

                    int ms = (int)(postTime - postTime.Date).TotalMilliseconds;
                    ms -= postTime < timeNow ? 24 * 60 * 60 * 1000 : 0; // if posted before first post -> remove 86,400,000ms

                    builder.AddField($"{title} {item.Author.Username}", $"[with]({link}) {ms.ToString("N0")}ms");

                    if (count == 0)
                        count++;

                    count++;
                }
            }

            var spamChannel = Client.GetGuild(Program.ApplicationSetting.BaseGuild).GetTextChannel(768600365602963496); // #spam

            string randomGif = randomGifs[new Random().Next(randomGifs.Count)];
            await spamChannel.SendMessageAsync(randomGif);
            await spamChannel.SendMessageAsync("", false, builder.Build());

            FirstDailyPostsCandidates = new List<SocketMessage>(); // Reset

            DiscordHelper.DiscordUserBirthday(Client, Program.ApplicationSetting.BaseGuild, spamChannel.Id, true); // on first daily post trigger birthday messages -> TODO maybe move to a cron job
        }

        public async Task HandleCommandAsync(SocketMessage m)
        {
            // Ignore everyone but the owner in debug mode
#if DEBUG
            if (m.Author.Id != Program.ApplicationSetting.Owner && !m.Author.IsBot && m.Content.StartsWith(Program.CurrentPrefix))
            {
                //m.Channel.SendMessageAsync("I'll ignore you");
                //return;
            }
#endif

            if (TempDisableIncoming)
                return;

            if (m is not SocketUserMessage msg)
                return;

            if (msg.Channel is not SocketGuildChannel guildChannel)
            {
                // no DM parsing for now (maybe delete saved post) in the future
                if (msg.Content == "AddDeleteButtons")
                {
                    var builderComponent = new ComponentBuilder().WithButton("Delete Message", "delete-saved-message-id", ButtonStyle.Danger);

                    var messageToDelete = await msg.Channel.GetMessagesAsync(1000).FlattenAsync();
                    if (messageToDelete != null)
                    {
                        foreach (var item in messageToDelete)
                        {
                            if (item.Components.Count == 0)
                            {
                                try
                                {
                                    // can only edit own messages
                                    if (item.Author.IsBot)
                                        await msg.Channel.ModifyMessageAsync(item.Id, i => i.Components = builderComponent.Build());
                                }
                                catch (Exception ex)
                                {

                                }
                            }
                        }
                    }
                }

                return;
            }

            // ignore this channel -> high msg volume
            if (!PlaceChannels.Any(i => i == msg.Channel.Id))
            {
                ulong channelId = msg.Channel.Id;

                if (msg.Author.IsWebhook)
                {
                    // Message was send by Webhook
                    if (msg.Author.Id == 941068482567602206)
                    {
                        // BattleRush's Helper Webhook Id

                        if (msg.Content.StartsWith("New Build available."))
                        {
                            // New Build has been deployed -> Restart application
                            int fromBranch = msg.Content.IndexOf("Branch:");
                            int fromCommit = msg.Content.IndexOf("Commit:");

                            string branch = msg.Content.Substring(fromBranch + "Branch:".Length, fromCommit - fromBranch - "Branch:".Length).Trim();
                            string commit = msg.Content.Substring(fromCommit + "Commit:".Length, msg.Content.Length - fromCommit - "Commit:".Length).Trim();


                            await msg.Channel.SendMessageAsync($"New Update detected with Branch: {branch} and Commit: {commit}. Restarting to apply update...");
                            Process.GetCurrentProcess().Kill();
                        }
                    }
                }

                // Get the perms from the parent channel if the message was send in a thread
                if (msg.Channel is SocketThreadChannel threadChannel)
                    channelId = threadChannel.ParentChannel.Id;

                var channelSettings = CommonHelper.GetChannelSettingByChannelId(channelId).Setting;


                try
                {
                    MessageHandler msgHandler = new MessageHandler(msg, CommandNames, channelSettings);
                    await msgHandler.Run();

                }
                catch (Exception ex)
                {
                    // TODO LOG ERROR
                }

                //if (!m.Author.IsBot && !commandList.Any(i => i.Name.ToLower() == msg.Content.ToLower().Replace(".", "")) && await TryToParseEmoji(msg))
                //    return; // emoji was found and we can exit here

                // TODO private channels

                var user = (SocketGuildUser)msg.Author;

                var dbManager = DatabaseManager.Instance();


                // Use discord snowflake
                // TODO may cause problems if the bot is hosted in a timezone that doesnt switch to daylight at the same time as the hosting region
                var timeNow = SnowflakeUtils.FromSnowflake(m.Id).AddHours(TimeZoneInfo.IsDaylightSavingTime(DateTime.Now) ? 2 : 1); // CEST CONVERSION

                // Add 10 seconds so messages before 00:00 are tracked to see which one was close
                if (LastNewDailyMessagePost.Day != timeNow.AddSeconds(10).Day && !user.IsBot)
                {
                    // Reset time 
                    LastNewDailyMessagePost = DateTime.UtcNow.AddHours(TimeZoneInfo.IsDaylightSavingTime(DateTime.Now) ? 2 : 1).AddSeconds(10);

                    // This person is the first (or one of the first) one to post a new message
                    FirstDailyPost();

                    // run it only once a day // todo find better scheduler
                    DiscordHelper.ReloadRoles(user.Guild);
                }

                if (CollectFirstDailyPostMessages && !user.IsBot)
                    FirstDailyPostsCandidates.Add(m);

                // duplicate code of the code above
                if (LastAfternoonMessagePost.Day != timeNow.AddHours(-12).Day && !user.IsBot)
                {
                    // Reset time 
                    LastAfternoonMessagePost = DateTime.UtcNow.AddHours(TimeZoneInfo.IsDaylightSavingTime(DateTime.Now) ? 2 : 1).AddHours(-12);

                    // This person is the first one to post a new message

                    var firstPoster = dbManager.GetDiscordUserById(msg.Author.Id);
                    dbManager.UpdateDiscordUser(new DiscordUser()
                    {
                        DiscordUserId = user.Id,
                        DiscriminatorValue = user.DiscriminatorValue,
                        AvatarUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(), // If user has no custom avatar load the url for the default avatar
                        IsBot = user.IsBot,
                        IsWebhook = user.IsWebhook,
                        Nickname = user.Nickname,
                        Username = user.Username,
                        JoinedAt = user.JoinedAt,
                        FirstAfternoonPostCount = firstPoster.FirstAfternoonPostCount + 1
                    });


                    EmbedBuilder builder = new EmbedBuilder();

                    builder.WithTitle($"{firstPoster.Nickname ?? firstPoster.Username} IS THE FIRST AFTERNOON POSTER");
                    builder.WithColor(0, 0, 255);
                    builder.WithDescription($"This is the {CommonHelper.DisplayWithSuffix(firstPoster.FirstAfternoonPostCount + 1)} time you are the first afternoon poster of the day. With {(timeNow.AddHours(-12) - timeNow.AddHours(-12).Date).TotalMilliseconds.ToString("N0")}ms from noon.");

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

                    var spamChannel = Client.GetGuild(Program.ApplicationSetting.BaseGuild).GetTextChannel(768600365602963496); // #spam

                    //string randomGif = randomGifs[new Random().Next(randomGifs.Count)];
                    //await m.Channel.SendMessageAsync(randomGif);
                    await spamChannel.SendMessageAsync("", false, builder.Build());

                    // ONE TIME CODE TO BE DELETED
                    //if (firstPoster.DiscordUserId == 321022340412735509)
                    //{
                    //    NextStepProgress2(m, msg.Author, firstPoster);
                    //}

                    // run it only once a day // todo find better scheduler
                    DiscordHelper.ReloadRoles(user.Guild);
                }

                //Discord.Image img = new Discord.Image(new Stream()); // stream
                //await user.Guild.ModifyAsync(msg => msg.Banner = img);

                try
                {
                    await LogManager.ProcessEmojisAndPings(m.Tags, m.Author.Id, m, (SocketGuildUser)m.Author);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }



            if (!(PlaceChannels.Any(i => i == m.Channel.Id) && AllowedBotCommands.Any(i => !m.Content.StartsWith(i))))
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

            if (!m.Author.IsBot && m.Author.Id != ApplicationSetting.Owner)
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
                            await m.Channel.SendMessageAsync($"Stop spamming <@{m.Author.Id}> your current timeout is {SpamCache[m.Author.Id]} UTC");
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
            Commands.ExecuteAsync(context, argPos, services);
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

        private async Task<bool> NextStepProgress(SocketMessage m, SocketUser user, DiscordUser firstPoster)
        {
            await m.Channel.SendMessageAsync("https://cdn.discordapp.com/attachments/774286694794919989/843988949612756992/unknown.gif");
            Thread.Sleep(3000);
            await m.Channel.SendMessageAsync("System breached.. Entering Admin Control Panel");
            Thread.Sleep(3000);
            await m.Channel.SendMessageAsync("ACP opened");
            Thread.Sleep(3000);

            var breachMessage = await m.Channel.SendMessageAsync("Granting permission");
            Thread.Sleep(3000);
            await breachMessage.ModifyAsync(msg => msg.Content = @"GRANT_ADMIN_PERMISSION");
            Thread.Sleep(2000);
            await breachMessage.ModifyAsync(msg => msg.Content = @"GRANT_ADMIN_PERMISSION User: 3");
            Thread.Sleep(1000);
            await breachMessage.ModifyAsync(msg => msg.Content = @"GRANT_ADMIN_PERMISSION User: 32");
            Thread.Sleep(1000);
            await breachMessage.ModifyAsync(msg => msg.Content = @"GRANT_ADMIN_PERMISSION User: 321");
            Thread.Sleep(1000);
            await breachMessage.ModifyAsync(msg => msg.Content = @"GRANT_ADMIN_PERMISSION User: 3210");
            Thread.Sleep(1000);
            await breachMessage.ModifyAsync(msg => msg.Content = @"GRANT_ADMIN_PERMISSION User: 32102");
            Thread.Sleep(1000);
            await breachMessage.ModifyAsync(msg => msg.Content = @"GRANT_ADMIN_PERMISSION User: 321022");
            Thread.Sleep(1000);

            // unlock next stage
            var messageMalfunction = await m.Channel.SendMessageAsync("System malfunction");
            Thread.Sleep(2000);
            await messageMalfunction.ModifyAsync(msg => msg.Content = @"System malfunction..");
            Thread.Sleep(1000);
            await messageMalfunction.ModifyAsync(msg => msg.Content = @"System malfunction....");
            Thread.Sleep(3000);

            // creash report
            var messageCreash = await m.Channel.SendMessageAsync("Printing Crash report");

            Thread.Sleep(2000);
            await messageCreash.ModifyAsync(msg => msg.Content = @"Printing Crash report...");
            Thread.Sleep(2000);
            await messageCreash.ModifyAsync(msg => msg.Content = @"
A problem has been detected and Discord has been shut down to prevent damage
to your computer.

**UNKNOWN_PERMISSION_REQUEST_ACCESS**

If this is the first time you've seen this error screen,
restart your computer. If this screen appears again, follow
these steps:

Check to make sure any new hardware or software is properly installed.
If this is a new installation, ask your hardware or software manufacturer
for any Discord updates you might need.

If problems continue, disable or remove any newly installed hardware
or software. Disable BIOS memory options such as caching or shadowing.
If you need to use Safe mode to remove or disable components, restart
your computer, press F8 to select Advanced Startup Options, and then
select Safe mode.

Technical Information:

*** STOP: 0xOOOOOOED (0x80F128D0, 0xc000009c, 0x00000000, 0x00000000)");
            Thread.Sleep(3000);

            await messageCreash.ModifyAsync(msg => msg.Content = @"System malfunction....

A problem has been detected and Discord has been shut down to prevent damage
to your computer.

**UNKNOWN_PERMISSION_REQUEST_ACCESS**

If this is the first time you've seen this error screen,
restart your computer. If this screen appears again, follow
these steps:

Check to make sure any new hardware or software is properly installed.
If this is a new installation, ask your hardware or software manufacturer
for any Discord updates you might need.

If problems continue, disable or remove any newly installed hardware
or software. Disable BIOS memory options such as caching or shadowing.
If you need to use Safe mode to remove or disable components, restart
your computer, press F8 to select Advanced Startup Options, and then
select Safe mode.

Technical Information:

*** STOP: 0xOOOOOOED (0x80F128D0, 0xc000009c, 0x00000000, 0x00000000)

Rebooting");
            Thread.Sleep(3000);

            await messageCreash.ModifyAsync(msg => msg.Content = @"System malfunction....

A problem has been detected and Discord has been shut down to prevent damage
to your computer.

**UNKNOWN_PERMISSION_REQUEST_ACCESS**

If this is the first time you've seen this error screen,
restart your computer. If this screen appears again, follow
these steps:

Check to make sure any new hardware or software is properly installed.
If this is a new installation, ask your hardware or software manufacturer
for any Discord updates you might need.

If problems continue, disable or remove any newly installed hardware
or software. Disable BIOS memory options such as caching or shadowing.
If you need to use Safe mode to remove or disable components, restart
your computer, press F8 to select Advanced Startup Options, and then
select Safe mode.

Technical Information:

*** STOP: 0xOOOOOOED (0x80F128D0, 0xc000009c, 0x00000000, 0x00000000)

Rebooting...");
            Thread.Sleep(2000);
            await messageCreash.ModifyAsync(msg => msg.Content = @"System malfunction....

A problem has been detected and Discord has been shut down to prevent damage
to your computer.

**UNKNOWN_PERMISSION_REQUEST_ACCESS**

If this is the first time you've seen this error screen,
restart your computer. If this screen appears again, follow
these steps:

Check to make sure any new hardware or software is properly installed.
If this is a new installation, ask your hardware or software manufacturer
for any Discord updates you might need.

If problems continue, disable or remove any newly installed hardware
or software. Disable BIOS memory options such as caching or shadowing.
If you need to use Safe mode to remove or disable components, restart
your computer, press F8 to select Advanced Startup Options, and then
select Safe mode.

Technical Information:

*** STOP: 0xOOOOOOED (0x80F128D0, 0xc000009c, 0x00000000, 0x00000000)

Rebooting......");
            Thread.Sleep(3000);
            await messageCreash.ModifyAsync(msg => msg.Content = @"System malfunction....

A problem has been detected and Discord has been shut down to prevent damage
to your computer.

**UNKNOWN_PERMISSION_REQUEST_ACCESS**

If this is the first time you've seen this error screen,
restart your computer. If this screen appears again, follow
these steps:

Check to make sure any new hardware or software is properly installed.
If this is a new installation, ask your hardware or software manufacturer
for any Discord updates you might need.

If problems continue, disable or remove any newly installed hardware
or software. Disable BIOS memory options such as caching or shadowing.
If you need to use Safe mode to remove or disable components, restart
your computer, press F8 to select Advanced Startup Options, and then
select Safe mode.

Technical Information:

*** STOP: 0xOOOOOOED (0x80F128D0, 0xc000009c, 0x00000000, 0x00000000)

Rebooting.........");
            Thread.Sleep(7000);

            await PrintProgressBar(m);

            var initMsg = await m.Channel.SendMessageAsync("Startup done.");
            await initMsg.ModifyAsync(msg => msg.Content = @"Initializing..");
            Thread.Sleep(2000);

            await initMsg.ModifyAsync(msg => msg.Content = @"Initializing....");
            Thread.Sleep(3000);

            await initMsg.ModifyAsync(msg => msg.Content = @"Initializing......");
            Thread.Sleep(4000);

            //await initMsg.DeleteAsync();

            EmbedBuilder nextStage = new EmbedBuilder();

            nextStage.WithTitle($"To finish the process initialization. Confirm to continue.");
            nextStage.WithColor(0, 0, 255);
            nextStage.WithAuthor(user);
            nextStage.WithCurrentTimestamp();

            var reactMessage = await m.Channel.SendMessageAsync("Process Initialization Check", false, nextStage.Build());
            await reactMessage.AddReactionAsync(Emote.Parse($"<:this:{DiscordHelper.DiscordEmotes["this"]}>"));


            return true;
        }

        private async Task<bool> NextStepProgress2(SocketMessage m, SocketUser user, DiscordUser firstPoster)
        {
            await m.Channel.SendMessageAsync("Welcome back <@321022340412735509>");
            Thread.Sleep(3000);
            await m.Channel.SendMessageAsync("Type .ACP to open the Admin Control Panel");

            return true;
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
            var progressBar = await m.Channel.SendMessageAsync("<empty>");

            //10
            for (int i = 0; i < 11; i++)
            {
                string line = left[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }
            await progressText.ModifyAsync(msg => msg.Content = "Step 1/69");

            // 20
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "Step 2/69");

            // 30
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }
            await progressText.ModifyAsync(msg => msg.Content = "could this be the day..?");
            // 40
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "that marc gets admin?");
            // 50
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);

            }

            await progressText.ModifyAsync(msg => msg.Content = "nah you right not today");
            // 60
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "btw supra stole nicely from you yesterday xD");
            // 70
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "yo this loading bar");
            // 80
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + middle[0] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "soon done idk what to say");
            // 90
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[i] + right[0];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            await progressText.ModifyAsync(msg => msg.Content = "also step 35/69 (you need a few first daily posts Marc xD");
            // 100
            for (int i = 1; i < 11; i++)
            {
                string line = left[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + middle[10] + right[i];
                await progressBar.ModifyAsync(msg => msg.Content = line);
                Thread.Sleep(1100);
            }

            return true;
        }
    }
}
