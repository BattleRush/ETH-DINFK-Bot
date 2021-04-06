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
using WebSocketSharp.Server;

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

        public static TimeZoneInfo TimeZoneInfo = TZConvert.GetTimeZoneInfo("Europe/Zurich");
        public static ILoggerFactory Logger { get; set; }

        private static DateTime LastNewDailyMessagePost = DateTime.Now;

        private static List<BotChannelSetting> BotChannelSettings;

        private static List<string> AllowedBotCommands = new List<string>()
        {
            ".place setpixel ",
            ".place pixelverify "
        };

        public static WebSocketServer PlaceWebsocket;



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
                   //services.AddCronJob<PreloadJob>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = @"0 3 * * *"; });// 3 am utc -> 4 am cet

                   // TODO adjust for summer time in CET/CEST
                   services.AddCronJob<SpaceXSubredditJob>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = BotSetting.SpaceXSubredditCheckCronJob; }); //BotSetting.SpaceXSubredditCheckCronJob "*/ 10 * * * *"

                   // TODO adjust for summer time in CET/CEST
                   services.AddCronJob<StartAllSubredditsJobs>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = @"0 4 * * *"; });// 4 am utc -> 5 am cet

                   // TODO adjust for summer time in CET/CEST
                   services.AddCronJob<BackupDBJob>(c => { c.TimeZoneInfo = TimeZoneInfo.Utc; c.CronExpression = @"0 0 * * *"; });// 0 am utc
               })
               .StartAsync();


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
            PlaceWebsocket = new WebSocketServer(9000);
            PlaceWebsocket.AddWebSocketService<PlaceWebsocket>("/place");
            PlaceWebsocket.Start();

            DatabaseManager.Instance().SetAllSubredditsStatus();
            LoadChannelSettings();


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

            string name = "Count the Divisors";
            string due = "Thursday, March 11, 2021 10:00:59 AM GMT+01:00";
            int exp = 100;

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle($"A new AnW challenge has been uploaded");
            builder.WithColor(128, 255, 128);
            builder.WithDescription($"Task Name: **{name}** " + Environment.NewLine + $"Due date: {due}" + Environment.NewLine + $"EXP: {exp}");

            builder.WithCurrentTimestamp();

            await anwChannel.SendMessageAsync("May the fastest speedrunner win :)", false, builder.Build());
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
#if !DEBUG
            if (!msg.HasStringPrefix(".", ref argPos))
                return;
#else
            if (!msg.HasStringPrefix("dev.", ref argPos))
                return;
#endif

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
