using Discord;
using Discord.WebSocket;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        private DatabaseManager DatabaseManager;
        public LogManager(DatabaseManager databaseManager)
        {
            DatabaseManager = databaseManager;
        }

        // TODO lock

        public static DateTime LastUpdate = DateTime.MinValue;

        public static DateTime LastGlobalUpdate = DateTime.MinValue;
        public async void AddReaction(Emote emote)
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

            DatabaseManager.AddEmojiStatistic(statistic, 1, true);
            //Program.GlobalStats.EmojiInfoUsage.Single(i => i.EmojiId == emote.Id).UsedAsReaction++;
        }
        public async void RemoveReaction(Emote emote)
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

            DatabaseManager.AddEmojiStatistic(statistic, -1, true);

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

        public static async void ProcessEmojisAndPings(IReadOnlyCollection<ITag> tags, ulong authorId)
        {
            try
            {
                if (tags.Count == 0)
                    return;

                List<ulong> listOfEmotes = new List<ulong>();
                foreach (Tag<Emote> tag in tags.Where(i => i.Type == TagType.Emoji))
                {
                    //var emote = (Emote)tag.Value;

                    if (Program.GlobalStats.EmojiInfoUsage.Any(i => i.EmojiId == tag.Value.Id))
                    {
                        Program.GlobalStats.EmojiInfoUsage.Single(i => i.EmojiId == tag.Value.Id).UsedInText++;
                    }
                    else
                    {
                        // TODO Race condition prof it
                        Program.GlobalStats.EmojiInfoUsage.Add(new EmojiInfo()
                        {
                            Animated = tag.Value.Animated,
                            EmojiId = tag.Value.Id,
                            EmojiName = tag.Value.Name,
                            Url = tag.Value.Url,
                            CreatedAt = tag.Value.CreatedAt,
                            UsedInText = 1 // First use :)
                        });
                    }

                    if (!listOfEmotes.Contains(tag.Value.Id))
                    {
                        Program.GlobalStats.EmojiInfoUsage.Single(i => i.EmojiId == tag.Value.Id).UsedInTextOnce++;
                        listOfEmotes.Add(tag.Value.Id);
                    }
                }

                List<ulong> listOfUsers = new List<ulong>();

                foreach (Tag<Discord.IUser> tag in tags.Where(i => i.Type == TagType.UserMention))
                {
                    if (tag?.Value == null)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("UNKNOWN USER PINGED OR MENTIONED");
                        continue;
                    }


                    var guildUser = tag.Value as SocketGuildUser;

                    if (guildUser?.Id == authorId)
                        continue; // Cant self ping

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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR STATS: " + ex.ToString()); ;
                // TODO handle
            }
        }

        public void ProcessMessage(SocketUser user, BotMessageType type)
        {
            var guildUser = user as SocketGuildUser;

            DatabaseManager.AddCommandStatistic(type, guildUser.Id);

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
