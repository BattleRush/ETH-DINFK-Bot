using Discord;
using Discord.WebSocket;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data.Enums;
using ETHDINFKBot.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Log
{

    // TODO OUTSOURCE MOST OF IT OUT
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
        //public async void AddReaction(Emote emote, ulong discordMessageId, SocketGuildUser user)
        //{
        //    try
        //    {
        //        var discordEmote = new DiscordEmote()
        //        {
        //            Animated = emote.Animated,
        //            DiscordEmoteId = emote.Id,
        //            EmoteName = emote.Name,
        //            Url = emote.Url,
        //            CreatedAt = emote.CreatedAt,
        //            Blocked = false,
        //            LastUpdatedAt = DateTime.Now, // todo chech changes
        //            LocalPath = null
        //        };

        //        DatabaseManager.EmoteDatabaseManager.ProcessDiscordEmote(discordEmote, discordMessageId, 1, true, user, false);
        //        //Program.GlobalStats.EmojiInfoUsage.Single(i => i.EmojiId == emote.Id).UsedAsReaction++;
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

        //public async void RemoveReaction(Emote emote, ulong discordMessageId, SocketGuildUser user)
        //{
        //    var discordEmote = new DiscordEmote()
        //    {
        //        Animated = emote.Animated,
        //        DiscordEmoteId = emote.Id,
        //        EmoteName = emote.Name,
        //        Url = emote.Url,
        //        CreatedAt = emote.CreatedAt,
        //        Blocked = false,
        //        LastUpdatedAt = DateTime.Now, // todo chech changes
        //        LocalPath = null
        //    };

        //    DatabaseManager.EmoteDatabaseManager.ProcessDiscordEmote(discordEmote, discordMessageId, -1, true, user, false);
        //}

        public async Task ProcessEmojisAndPings(IReadOnlyCollection<ITag> tags, ulong authorId, SocketMessage message, SocketGuildUser fromUser, bool isPreload = false)
        {
            try
            {
                if (tags.Count == 0)
                    return;

                Stopwatch stopwatch = Stopwatch.StartNew();
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

                var guildUser = (fromUser as IGuildUser);
                string messages = "";
                foreach (var emote in listOfEmotes)
                {
                    Stopwatch stopwatch2 = Stopwatch.StartNew();
                    Tag<Emote> tag = (Tag<Emote>)tags.First(i => i.Type == TagType.Emoji && ((Tag<Emote>)i).Value.Id == emote.Key);

                    var stat = new DiscordEmote()
                    {
                        Animated = tag.Value.Animated,
                        DiscordEmoteId = tag.Value.Id,
                        EmoteName = tag.Value.Name,
                        Url = tag.Value.Url,
                        CreatedAt = tag.Value.CreatedAt,
                        Blocked = false,
                        LastUpdatedAt = DateTime.Now,
                        LocalPath = null
                    };


                    /*cavebob stuff outdated*/
                    //if (emote.Value == 10 && tag.Value?.Id == 747783377146347590)
                    //{

                    //    ulong caveBobGang = 824425544333656104;

                    //    if (!guildUser.RoleIds.Contains(caveBobGang))
                    //    {
                    //        var role = guildUser.Guild.Roles.FirstOrDefault(x => x.Id == caveBobGang);

                    //        // cavebob gang role
                    //        await guildUser.AddRoleAsync(role);
                    //    }
                    //}

                    long elapsedDownload = await DatabaseManager.EmoteDatabaseManager.ProcessDiscordEmote(stat, message.Id, emote.Value, false, fromUser, isPreload);

                    stopwatch2.Stop();
                    messages += $"{stopwatch2.ElapsedMilliseconds} ms (Emote process) {tag.Value.Name} - Download: {elapsedDownload} ms" + Environment.NewLine;
                }

                if (message.Author.Id == 155419933998579713 && message.Tags.Count > 5)
                    message.Channel.SendMessageAsync(messages.Substring(0, Math.Min(messages.Length, 2000)));

                stopwatch.Stop();
                if (message.Author.Id == 155419933998579713 && message.Tags.Count > 5)
                    message.Channel.SendMessageAsync($"{stopwatch.ElapsedMilliseconds} ms (Inner loop)");

                // TODO dont hammer the db after each call (check if any new emotes have been added
                long emoteCount = DatabaseManager.EmoteDatabaseManager.TotalEmoteCount();
                if (Program.TotalEmotes != emoteCount)
                {
                    Program.TotalEmotes = emoteCount;
                    // place pixels is now tracked all 5 mins
                    await Program.Client.SetGameAsync($"{Program.TotalEmotes} emotes", null, ActivityType.Watching);
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
                    DatabaseManager.AddPingStatistic(pingInfo.Key, pingInfo.Value, fromUser);
                    var dbMessage = DatabaseManager.GetDiscordMessageById(message.Id);
                    var user = DatabaseManager.GetDiscordUserById(pingInfo.Key);

                    // The pinged user doesnt exist in our db -> dont create ping
                    // TODO create an entry for this user
                    // TODO log these cases
                    if (user == null)
                        continue;

                    DatabaseManager.CreatePingHistory(new PingHistory()
                    {
                        DiscordMessageId = dbMessage != null ? message.Id : null,
                        DiscordRoleId = null,
                        DiscordUserId = pingInfo.Key,
                        FromDiscordUserId = fromUser.Id
                    });
                }

                // most of the older roles probably dont exist -> TODO check if they dont exist and then ignore
                if (!isPreload)
                {
                    foreach (Tag<Discord.IRole> role in tags.Where(i => i.Type == TagType.RoleMention))
                    {
                        var dbRole = DatabaseManager.GetDiscordRole(role.Key);
                        if (dbRole == null)
                        {
                            // some role is missing
                            DiscordHelper.ReloadRoles(fromUser.Guild);
                        }

                        var dbMessage = DatabaseManager.GetDiscordMessageById(message.Id);
                        DatabaseManager.CreatePingHistory(new PingHistory()
                        {
                            DiscordMessageId = dbMessage != null ? message.Id : null,
                            DiscordRoleId = role.Key,
                            DiscordUserId = null,
                            FromDiscordUserId = fromUser.Id
                        });


                        // Ping Hell
                        if (role.Key == 895231323034222593)
                        {
                            // If the user doesnt have the Ping Hell role assign the role to it
                            ulong pingHellRole = 895231323034222593;

                            var user = await message.Channel.GetUserAsync(guildUser.Id) as SocketGuildUser; // Download the user -> to refresh the cache

                            if (!guildUser.RoleIds.Contains(pingHellRole) || (user != null && !user.Roles.Any(i => i.Id == pingHellRole)))
                            {
                                var rolePingHell = guildUser.Guild.Roles.FirstOrDefault(x => x.Id == pingHellRole);

                                await guildUser.AddRoleAsync(rolePingHell);
                                await message.Channel.SendMessageAsync($"<@{authorId}> welcome to PingHell!");
                            }
                        }
                    }
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
            catch (Exception ex)
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
