using Discord;
using Discord.WebSocket;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Log
{
    /*
        public enum BotMessageType
        {
            Neko = 0,
            Search = 1,
            NekoGif = 2,
            Holo = 3,
            Waifu = 4,
            Baka = 5,
            Smug = 6,
            Fox = 7,
            Avatar = 8,
            NekoAvatar = 9,
            Wallpaper = 10,
            Animalears = 11,
            Foxgirl = 12,
            Other = 999
        }*/

    public class LogManager
    {
        private readonly ILogger _logger = new Logger<LogManager>(Program.Logger);
        private DatabaseManager DatabaseManager;
        public LogManager(DatabaseManager databaseManager)
        {
            DatabaseManager = databaseManager;
        }

        // TODO lock

        public static DateTime LastUpdate = DateTime.MinValue;

        public static DateTime LastGlobalUpdate = DateTime.MinValue;
        public async void AddReaction(Emote emote, bool isBot)
        {
            try
            {
                var statistic = new EmojiStatistic()
                {
                    Animated = emote.Animated,
                    EmojiId = emote.Id,
                    EmojiName = emote.Name,
                    Url = emote.Url,
                    CreatedAt = emote.CreatedAt,
                    UsedAsReaction = 1
                };

                DatabaseManager.AddEmojiStatistic(statistic, 1, true, isBot);
                //Program.GlobalStats.EmojiInfoUsage.Single(i => i.EmojiId == emote.Id).UsedAsReaction++;
            }
            catch (Exception ex)
            {

            }
        }
        public async void RemoveReaction(Emote emote, bool isBot)
        {
            var statistic = new EmojiStatistic()
            {
                Animated = emote.Animated,
                EmojiId = emote.Id,
                EmojiName = emote.Name,
                Url = emote.Url,
                CreatedAt = emote.CreatedAt,
                UsedAsReaction = -1
            };

            DatabaseManager.AddEmojiStatistic(statistic, -1, true, isBot);

            /*
            if (Program.GlobalStats.EmojiInfoUsage.Any(i => i.EmojiId == emote.Id))
            {
                Program.GlobalStats.EmojiInfoUsage.Single(i => i.EmojiId == emote.Id).UsedAsReaction--;
            }
            else
            {
                // You shouldnt be here LOL

                Console.WriteLine("DANGER!!!!!!!!!!!!!!!!!!!!!!!!!!");
            }*/
        }

        public async Task ProcessEmojisAndPings(IReadOnlyCollection<ITag> tags, ulong authorId, bool isBot)
        {
            try
            {
                if (tags.Count == 0)
                    return;

                Dictionary<ulong, int> listOfEmotes = new Dictionary<ulong, int>();
                foreach (Tag<Emote> tag in tags.Where(i => i.Type == TagType.Emoji))
                {
                    if (listOfEmotes.ContainsKey(tag.Value.Id))
                    {
                        listOfEmotes[tag.Value.Id]++;
                    }
                    else
                    {
                        listOfEmotes.Add(tag.Value.Id, 1);
                    }
                }

                foreach (var emote in listOfEmotes)
                {
                    Tag<Emote> tag = (Tag<Emote>)tags.First(i => i.Type == TagType.Emoji && ((Tag<Emote>)i).Value.Id == emote.Key);

                    var stat = new EmojiStatistic()
                    {
                        Animated = tag.Value.Animated,
                        EmojiId = tag.Value.Id,
                        EmojiName = tag.Value.Name,
                        Url = tag.Value.Url,
                        CreatedAt = tag.Value.CreatedAt,
                        UsedInText = emote.Value,
                        UsedInTextOnce = 1,
                        UsedAsReaction = 0
                    };

                    DatabaseManager.AddEmojiStatistic(stat, emote.Value, false, isBot);

                }

                /*
                if (!listOfEmotes.Contains(tag.Value.Id))
                {
                    Program.GlobalStats.EmojiInfoUsage.Single(i => i.EmojiId == tag.Value.Id).UsedInTextOnce++;
                    listOfEmotes.Add(tag.Value.Id);
                }*/

                Dictionary<ulong, int> listOfUsers = new Dictionary<ulong, int>();

                foreach (Tag<Discord.IUser> tag in tags.Where(i => i.Type == TagType.UserMention))
                {
                    if (tag?.Value == null)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("UNKNOWN USER PINGED OR MENTIONED");
                        continue;
                    }


                    //var guildUser = tag.Value as SocketGuildUser;

                    if (tag.Key == authorId)
                        continue; // Cant self ping


                    if (listOfUsers.ContainsKey(tag.Key))
                    {
                        listOfUsers[tag.Key]++;
                    }
                    else
                    {
                        listOfUsers.Add(tag.Key, 1);
                    }


                }

                foreach (var pingInfo in listOfUsers)
                {
                    DatabaseManager.AddPingStatistic(pingInfo.Key, pingInfo.Value, isBot);
                }

                /*

                if (!Program.GlobalStats.PingInformation.Any(i => i.DiscordUser.DiscordId == guildUser?.Id))
                {
                    Program.GlobalStats.PingInformation.Add(new PingInformation()
                    {
                        DiscordUser = new Stats.DiscordUser()
                        {
                            DiscordId = guildUser.Id,
                            DiscordDiscriminator = guildUser.DiscriminatorValue,
                            DiscordName = guildUser.Username,
                            ServerUserName = guildUser.Nickname ?? guildUser.Username // User Nickname -> Update
                        },
                        PingCount = 1
                    });
                }
                else
                {
                    Program.GlobalStats.PingInformation.Single(i => i.DiscordUser.DiscordId == tag.Value.Id).PingCount++;
                }

                if (!listOfUsers.Contains(tag.Value.Id))
                {
                    Program.GlobalStats.PingInformation.Single(i => i.DiscordUser.DiscordId == tag.Value.Id).PingCountOnce++;
                    listOfUsers.Add(tag.Value.Id);
                }
            }

            if (LastGlobalUpdate < DateTime.Now.AddSeconds(-30))
            {
                LastGlobalUpdate = DateTime.Now;
                Program.SaveGlobalStats();
            }*/

            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR STATS: " + ex.ToString()); ;
                // TODO handle
            }
        }

        public void ProcessMessage(SocketUser user, BotMessageType type)
        {
            try
            {
                var guildUser = user as SocketGuildUser;

                DatabaseManager.AddCommandStatistic(type, guildUser.Id);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            /*
            if (!Program.BotStats.DiscordUsers.Any(i => i.DiscordId == user.Id))
            {
                Program.BotStats.DiscordUsers.Add(new Stats.DiscordUser()
                {
                    DiscordId = guildUser.Id,
                    DiscordDiscriminator = guildUser.DiscriminatorValue,
                    DiscordName = guildUser.Username,
                    ServerUserName = guildUser.Nickname ?? guildUser.Username, // User Nickname -> Update
                    Stats = new Stats.UserStats()
                });
            }

            var statUser = Program.BotStats.DiscordUsers.Single(i => i.DiscordId == user.Id);

            if (guildUser != null && statUser.ServerUserName != guildUser.Nickname)
            {
                // To update username changes
                statUser.ServerUserName = guildUser.Nickname ?? guildUser.Username;
            }

            // To prevent stats format from breaking
            statUser.ServerUserName = statUser.ServerUserName.Replace("*", "").Replace("~", "").Replace("|", "");
            statUser.DiscordName = statUser.DiscordName.Replace("*", "").Replace("~", "").Replace("|", "");

            switch (type)
            {
                case BotMessageType.Neko:
                    statUser.Stats.TotalNeko++;
                    break;
                case BotMessageType.Search:
                    statUser.Stats.TotalSearch++;
                    break;
                case BotMessageType.NekoGif:
                    statUser.Stats.TotalNekoGif++;
                    break;
                case BotMessageType.Holo:
                    statUser.Stats.TotalHolo++;
                    break;
                case BotMessageType.Waifu:
                    statUser.Stats.TotalWaifu++;
                    break;
                case BotMessageType.Baka:
                    statUser.Stats.TotalBaka++;
                    break;
                case BotMessageType.Smug:
                    statUser.Stats.TotalSmug++;
                    break;
                case BotMessageType.Fox:
                    statUser.Stats.TotalFox++;
                    break;
                case BotMessageType.Avatar:
                    statUser.Stats.TotalAvatar++;
                    break;
                case BotMessageType.NekoAvatar:
                    statUser.Stats.TotalNekoAvatar++;
                    break;
                case BotMessageType.Wallpaper:
                    statUser.Stats.TotalWallpaper++;
                    break;
                case BotMessageType.Animalears:
                    statUser.Stats.TotalAnimalears++;
                    break;
                case BotMessageType.Foxgirl:
                    statUser.Stats.TotalFoxgirl++;
                    break;
                default:
                    break;
            }

            statUser.Stats.TotalCommands++;

            if (LastUpdate < DateTime.Now.AddSeconds(-30))
            {
                LastUpdate = DateTime.Now;
                Program.SaveStats();
            }*/
        }
    }
}
