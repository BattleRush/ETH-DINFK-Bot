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

namespace ETHDINFKBot
{
    class Program
    {
        private DiscordSocketClient Client;
        private CommandService commands;

        private IServiceProvider services;
        private static IConfiguration Configuration;
        private static string DiscordToken { get; set; }
        public static ulong Owner { get; set; }

        // TODO one object and somewhere else but im lazy
        public static string RedditAppId { get; set; }
        public static string RedditRefreshToken { get; set; }
        public static string RedditAppSecret { get; set; }
        public static string RedditBasePath { get; set; }





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

            RedditAppId = Configuration["Reddit:AppId"];
            RedditRefreshToken = Configuration["Reddit:RefreshToken"];
            RedditAppSecret = Configuration["Reddit:AppSecret"];
            RedditBasePath = Configuration["Reddit:BasePath"];



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

        public static void BackUpStats()
        {
            File.Copy("Stats\\stats.json", $"Stats\\Backup\\stats_{DateTime.Now.ToString("yyyyMMdd_hhmmss")}.json");
        }

        public static void BackUpGlobalStats()
        {
            File.Copy("Stats\\global_stats.json", $"Stats\\Backup\\global_stats_{DateTime.Now.ToString("yyyyMMdd_hhmmss")}.json");
        }
        public static void BackUpBlackList()
        {
            File.Copy("Blacklist\\blacklist.json", $"Blacklist\\Backup\\blacklist_{DateTime.Now.ToString("yyyyMMdd_hhmmss")}.json");
        }

        public static void LoadStats()
        {
            if (File.Exists("Stats\\stats.json"))
            {
                BackUpStats();
                string content = File.ReadAllText("Stats\\stats.json");
                BotStats = JsonConvert.DeserializeObject<BotStats>(content);
            }
        }

        public static void LoadBlacklist()
        {
            if (File.Exists("Blacklist\\Blacklist.json"))
            {
                BackUpBlackList();
                string content = File.ReadAllText("Blacklist\\blacklist.json");
                BlackList = JsonConvert.DeserializeObject<List<ReportInfo>>(content);

                // Remove duplicates
                BlackList = BlackList.GroupBy(i => i.ImageUrl).Select(i => i.First()).ToList();
            }
        }

        public static void LoadGlobalStats()
        {
            if (File.Exists("Stats\\global_stats.json"))
            {
                BackUpGlobalStats();
                string content = File.ReadAllText("Stats\\global_stats.json");
                GlobalStats = JsonConvert.DeserializeObject<GlobalStats>(content);
            }
        }

        public static void SaveStats()
        {
            string content = JsonConvert.SerializeObject(BotStats);
            File.WriteAllText("Stats\\stats.json", content);
        }

        public static void SaveGlobalStats()
        {
            string content = JsonConvert.SerializeObject(GlobalStats);
            File.WriteAllText("Stats\\global_stats.json", content);
        }

        public static void SaveBlacklist()
        {
            string content = JsonConvert.SerializeObject(BlackList);
            File.WriteAllText("Blacklist\\blacklist.json", content);
        }

        private static void MigrateToSqliteDb()
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                if (context.CommandTypes.Count() == 0)
                {
                    // TODO check for updates
                    var count = Enum.GetValues(typeof(ETHBot.DataLayer.Data.Enums.BotMessageType)).Length;
                    var types = new List<CommandType>();
                    for (var i = 0; i < count; i++)
                    {
                        types.Add(new CommandType
                        {
                            CommandTypeId = i,
                            Name = ((ETHBot.DataLayer.Data.Enums.BotMessageType)i).ToString()
                        });
                    }
                    context.CommandTypes.AddRange(types);
                    context.SaveChanges();
                }

                if (context.CommandStatistics.Count() == 0)
                {
                    foreach (var item in BotStats.DiscordUsers)
                    {
                        var user = context.DiscordUsers.SingleOrDefault(i => i.DiscordUserId == item.DiscordId);
                        if (user == null)
                        {
                            context.DiscordUsers.Add(new ETHBot.DataLayer.Data.Discord.DiscordUser()
                            {
                                DiscordUserId = item.DiscordId,
                                DiscriminatorValue = item.DiscordDiscriminator,
                                //AvatarUrl = item.ReportedBy.,
                                IsBot = false,
                                IsWebhook = false,
                                Nickname = item.ServerUserName,
                                Username = item.DiscordName//,
                                //JoinedAt = null
                            });
                            context.SaveChanges();
                        }

                        user = context.DiscordUsers.SingleOrDefault(i => i.DiscordUserId == item.DiscordId);



                        if (item.Stats.TotalNeko > 0)
                        {
                            var type = context.CommandTypes.Single(i => i.CommandTypeId == 1);
                            context.CommandStatistics.Add(new CommandStatistic()
                            {
                                Type = type,
                                DiscordUser = user,
                                Count = item.Stats.TotalNeko
                            });
                        }

                        if (item.Stats.TotalSearch > 0)
                        {
                            var type = context.CommandTypes.Single(i => i.CommandTypeId == 2);
                            context.CommandStatistics.Add(new CommandStatistic()
                            {
                                Type = type,
                                DiscordUser = user,
                                Count = item.Stats.TotalSearch
                            });
                        }

                        if (item.Stats.TotalNekoGif > 0)
                        {
                            var type = context.CommandTypes.Single(i => i.CommandTypeId == 3);
                            context.CommandStatistics.Add(new CommandStatistic()
                            {
                                Type = type,
                                DiscordUser = user,
                                Count = item.Stats.TotalNekoGif
                            });
                        }

                        if (item.Stats.TotalHolo > 0)
                        {
                            var type = context.CommandTypes.Single(i => i.CommandTypeId == 4);
                            context.CommandStatistics.Add(new CommandStatistic()
                            {
                                Type = type,
                                DiscordUser = user,
                                Count = item.Stats.TotalHolo
                            });
                        }


                        if (item.Stats.TotalWaifu > 0)
                        {
                            var type = context.CommandTypes.Single(i => i.CommandTypeId == 5);
                            context.CommandStatistics.Add(new CommandStatistic()
                            {
                                Type = type,
                                DiscordUser = user,
                                Count = item.Stats.TotalWaifu
                            });
                        }

                        if (item.Stats.TotalBaka > 0)
                        {
                            var type = context.CommandTypes.Single(i => i.CommandTypeId == 6);
                            context.CommandStatistics.Add(new CommandStatistic()
                            {
                                Type = type,
                                DiscordUser = user,
                                Count = item.Stats.TotalBaka
                            });
                        }

                        if (item.Stats.TotalSmug > 0)
                        {
                            var type = context.CommandTypes.Single(i => i.CommandTypeId == 7);
                            context.CommandStatistics.Add(new CommandStatistic()
                            {
                                Type = type,
                                DiscordUser = user,
                                Count = item.Stats.TotalSmug
                            });
                        }

                        if (item.Stats.TotalFox > 0)
                        {
                            var type = context.CommandTypes.Single(i => i.CommandTypeId == 8);
                            context.CommandStatistics.Add(new CommandStatistic()
                            {
                                Type = type,
                                DiscordUser = user,
                                Count = item.Stats.TotalFox
                            });
                        }

                        if (item.Stats.TotalAvatar > 0)
                        {
                            var type = context.CommandTypes.Single(i => i.CommandTypeId == 9);
                            context.CommandStatistics.Add(new CommandStatistic()
                            {
                                Type = type,
                                DiscordUser = user,
                                Count = item.Stats.TotalAvatar
                            });
                        }


                        if (item.Stats.TotalNekoAvatar > 0)
                        {
                            var type = context.CommandTypes.Single(i => i.CommandTypeId == 10);
                            context.CommandStatistics.Add(new CommandStatistic()
                            {
                                Type = type,
                                DiscordUser = user,
                                Count = item.Stats.TotalNekoAvatar
                            });
                        }


                        if (item.Stats.TotalWallpaper > 0)
                        {
                            var type = context.CommandTypes.Single(i => i.CommandTypeId == 11);
                            context.CommandStatistics.Add(new CommandStatistic()
                            {
                                Type = type,
                                DiscordUser = user,
                                Count = item.Stats.TotalWallpaper
                            });
                        }


                        if (item.Stats.TotalAnimalears > 0)
                        {
                            var type = context.CommandTypes.Single(i => i.CommandTypeId == 12);
                            context.CommandStatistics.Add(new CommandStatistic()
                            {
                                Type = type,
                                DiscordUser = user,
                                Count = item.Stats.TotalAnimalears
                            });
                        }

                        if (item.Stats.TotalFoxgirl > 0)
                        {
                            var type = context.CommandTypes.Single(i => i.CommandTypeId == 13);
                            context.CommandStatistics.Add(new CommandStatistic()
                            {
                                Type = type,
                                DiscordUser = user,
                                Count = item.Stats.TotalFoxgirl
                            });
                        }



                        context.SaveChanges();

                    }




                }

                if (context.PingStatistics.Count() == 0)
                {
                    foreach (var item in GlobalStats.PingInformation)
                    {
                        var user = context.DiscordUsers.SingleOrDefault(i => i.DiscordUserId == item.DiscordUser.DiscordId);
                        if (user == null)
                        {
                            context.DiscordUsers.Add(new ETHBot.DataLayer.Data.Discord.DiscordUser()
                            {
                                DiscordUserId = item.DiscordUser.DiscordId,
                                DiscriminatorValue = item.DiscordUser.DiscordDiscriminator,
                                //AvatarUrl = item.ReportedBy.,
                                IsBot = false,
                                IsWebhook = false,
                                Nickname = item.DiscordUser.ServerUserName,
                                Username = item.DiscordUser.DiscordName//,
                                //JoinedAt = null
                            });
                            context.SaveChanges();
                        }

                        user = context.DiscordUsers.SingleOrDefault(i => i.DiscordUserId == item.DiscordUser.DiscordId);


                        context.PingStatistics.Add(new PingStatistic()
                        {
                            DiscordUser = user,
                            PingCount = item.PingCount,
                            PingCountOnce = item.PingCountOnce,
                            PingCountBot = 0
                        });


                    }
                }

                context.SaveChanges();

                if (context.EmojiHistory.Count() == 0 && false)
                {
                    foreach (var item in GlobalStats.EmojiInfoUsage)
                    {
                        context.EmojiStatistics.Add(new EmojiStatistic()
                        {
                            Animated = item.Animated,
                            CreatedAt = item.CreatedAt,
                            EmojiId = item.EmojiId,
                            EmojiName = item.EmojiName,
                            Url = item.Url,
                            UsedAsReaction = item.UsedAsReaction,
                            UsedInText = item.UsedInText,
                            UsedInTextOnce = item.UsedInTextOnce
                        });

                        context.SaveChanges();

                        var stat = context.EmojiStatistics.Single(i => i.EmojiId == item.EmojiId);

                        for (int i = 0; i < item.UsedInTextOnce; i++)
                        {
                            context.EmojiHistory.Add(new EmojiHistory()
                            {
                                Count = 1,
                                IsReaction = false,
                                DateTimePosted = DateTime.Now,
                                EmojiStatistic = stat
                            });
                        }

                        context.EmojiHistory.Add(new EmojiHistory()
                        {
                            Count = item.UsedInText - item.UsedInTextOnce,
                            IsReaction = false,
                            DateTimePosted = DateTime.Now,
                            EmojiStatistic = stat
                        });

                        for (int i = 0; i < item.UsedAsReaction; i++)
                        {
                            context.EmojiHistory.Add(new EmojiHistory()
                            {
                                Count = 1,
                                IsReaction = true,
                                DateTimePosted = DateTime.Now,
                                EmojiStatistic = stat
                            });
                        }

                        context.SaveChanges();
                    }
                }

                context.SaveChanges();

                if (context.BannedLinks.Count() == 0)
                {
                    // not migrated yet


                    foreach (var item in BlackList)
                    {
                        var user = context.DiscordUsers.SingleOrDefault(i => i.DiscordUserId == item.ReportedBy.DiscordId);
                        if (user == null)
                        {
                            context.DiscordUsers.Add(new ETHBot.DataLayer.Data.Discord.DiscordUser()
                            {
                                DiscordUserId = item.ReportedBy.DiscordId,
                                DiscriminatorValue = item.ReportedBy.DiscordDiscriminator,
                                //AvatarUrl = item.ReportedBy.,
                                IsBot = false,
                                IsWebhook = false,
                                Nickname = item.ReportedBy.ServerUserName,
                                Username = item.ReportedBy.DiscordName//,
                                                                      //JoinedAt = null
                            });
                            context.SaveChanges();
                        }

                        user = context.DiscordUsers.SingleOrDefault(i => i.DiscordUserId == item.ReportedBy.DiscordId);

                        if (item.ImageUrl.Contains("discordapp") || !item.ImageUrl.StartsWith("https://") || context.BannedLinks.Any(i => i.Link == item.ImageUrl))
                        {
                            continue; // clean up wrong blocks
                        }

                        context.BannedLinks.Add(new ETHBot.DataLayer.Data.Discord.BannedLink()
                        {
                            ByUser = user,
                            Link = item.ImageUrl,
                            ReportTime = item.ReportedAt == DateTime.MinValue ? DateTime.Now : item.ReportedAt
                        });

                    }

                    context.SaveChanges();
                }


            }
        }

        public async Task MainAsync(string token)
        {
            DatabaseManager.Instance().SetAllSubredditsStatus();

            LoadStats();
            LoadGlobalStats();
            LoadBlacklist();
            MigrateToSqliteDb();

            var config = new DiscordSocketConfig { MessageCacheSize = 250 };
            Client = new DiscordSocketClient(config);

            Client.MessageReceived += HandleCommandAsync;
            Client.ReactionAdded += Client_ReactionAdded;
            Client.ReactionRemoved += Client_ReactionRemoved;

            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();

#if DEBUG
            await Client.SetGameAsync($"DEV MODE");
#else
            await Client.SetGameAsync($"with a neko");
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

        private Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            if (((SocketGuildUser)arg3.User).IsBot == true)
                return Task.CompletedTask;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Removed emote {arg3.Emote.Name} by {arg3.User.Value.Username}");
            LogManager.RemoveReaction((Emote)arg3.Emote, ((SocketGuildUser)arg3.User).IsBot);

            if (((Emote)arg3.Emote).Id == 780179874656419880)
            {
                // TODO Remove the post from saved
            }

            return Task.CompletedTask;
        }

        private Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            try
            {
                /*if ( == true)
                    return Task.CompletedTask;*/

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Added emote {arg3.Emote.Name} by {arg3.User.Value.Username}");
                LogManager.AddReaction((Emote)arg3.Emote, ((SocketGuildUser)arg3.User).IsBot);

                if (((Emote)arg3.Emote).Id == 780179874656419880)
                {
                    // Save the post link

                    /*          var user = DatabaseManager.GetDiscordUserById(arg1.Value.Author.Id); // Verify the user is created but should actually be available by this poitn
                    var saveBy = DatabaseManager.GetDiscordUserById(arg3.User.Value.Id); // Verify the user is created but should actually be available by this poitn
                    */

                    var guildChannel = (SocketGuildChannel)arg1.Value.Channel;
                    var link = $"https://discord.com/channels/{guildChannel.Guild.Id}/{guildChannel.Id}/{arg1.Value.Id}";
                    if (!string.IsNullOrWhiteSpace(arg1.Value.Content))
                    {
                        DatabaseManager.SaveMessage(arg1.Value.Id, arg1.Value.Author.Id, arg3.User.Value.Id, link, arg1.Value.Content);
                        // TODO parse to guild user
                        arg3.User.Value.SendMessageAsync($"Saved post from {arg1.Value.Author.Username}: {Environment.NewLine} {arg1.Value.Content} {Environment.NewLine}Direct link: [{guildChannel.Guild.Name}/{guildChannel.Name}/by {arg1.Value.Author.Username}] <{link}>");
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

                            arg3.User.Value.SendMessageAsync($"Saved post (Attachment) from {arg1.Value.Author.Username}:{Environment.NewLine} {item.Url}{Environment.NewLine}Direct link: [{guildChannel.Guild.Name}/{guildChannel.Name}/by {arg1.Value.Author.Username}] <{link}>");
                        }

                    }

                    // TODO markdown -> guild user also
                    arg1.Value.Channel.SendMessageAsync($"{arg3.User.Value.Username} saves <{link}> by {arg1.Value.Author.Username}");
                }

            }
            catch (Exception ex)
            {

            }

            return Task.CompletedTask;
        }

        private static Dictionary<ulong, DateTime> SpamCache = new Dictionary<ulong, DateTime>();
        public async Task HandleCommandAsync(SocketMessage m)
        {
            if (!(m is SocketUserMessage msg)) return;


            var dbManager = DatabaseManager.Instance();

            DiscordServer discordServer = null;
            DiscordChannel discordChannel = null;
            if (msg.Channel is SocketGuildChannel guildChannel)
            {
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


            var dbAuthor = dbManager.GetDiscordUserById(msg.Author.Id);
            // todo check for update
            if (dbAuthor == null)
            {
                var user = (SocketGuildUser)msg.Author; // todo check non socket user

                dbAuthor = dbManager.CreateUser(new ETHBot.DataLayer.Data.Discord.DiscordUser()
                {
                    DiscordUserId = user.Id,
                    DiscriminatorValue = user.DiscriminatorValue,
                    //AvatarUrl = item.ReportedBy.,
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
                // TODO Update user
            }




            dbManager.CreateDiscordMessage(new ETHBot.DataLayer.Data.Discord.DiscordMessage()
            {
                //Channel = discordChannel,
                DiscordChannelId = discordChannel.DiscordChannelId,
                //DiscordUser = dbAuthor,
                DiscordUserId = dbAuthor.DiscordUserId,
                MessageId = msg.Id,
                Content = msg.Content
            });

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

            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {msg.Author} wrote: {msg.Content}");
            File.AppendAllText($"Logs\\ETHDINFK_{DateTime.Now:yyyy_MM_dd}_spam.txt", $"[{DateTime.Now:yyyy.MM.dd HH:mm:ss}] " + msg.Author + " wrote: " + msg.Content + Environment.NewLine);

            if (m.Channel.Id == 747758757395562557 || m.Channel.Id == 758293511514226718 || m.Channel.Id == 747758770393972807 ||
            m.Channel.Id == 774286694794919989)
            {
                // TODO Channel ID as config
                await m.AddReactionAsync(Emote.Parse("<:this:747783377662378004>"));
                await m.AddReactionAsync(Emote.Parse("<:that:758262252699779073>"));
            }
 
            LogManager.ProcessEmojisAndPings(m.Tags, m.Author.Id, ((SocketGuildUser)m.Author).IsBot);

            if (m.Author.IsBot)
                return;

            int argPos = 0;
            if (!(msg.HasStringPrefix(".", ref argPos)))
            {
                return;
            }

            if (m.Author.Id != Owner)
            {
                if (SpamCache.ContainsKey(m.Author.Id))
                {
                    if (SpamCache[m.Author.Id] > DateTime.Now.AddMilliseconds(-750))
                    {
                        SpamCache[m.Author.Id] = SpamCache[m.Author.Id].AddMilliseconds(1000);

                        // TODO save last no spam message time
                        if (new Random().Next(0, 20) == 0)
                        {
                            // Ignore the user than to reply takes 1 message away from the rate limit
                            m.Channel.SendMessageAsync($"Stop spamming <@{m.Author.Id}>");
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
            await commands.ExecuteAsync(context, argPos, services);
        }

        private async static void Test()
        {

        }
    }
}
