using Discord;
using Discord.WebSocket;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data.Enums;
using ETHBot.DataLayer.Data.Reddit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace ETHDINFKBot
{
    public class DatabaseManager
    {
        private static DatabaseManager _instance;
        private static object syncLock = new object();
        private readonly ILogger _logger = new Logger<DiscordModule>(Program.Logger);

        public static DatabaseManager Instance()
        {
            lock (syncLock)
            {
                if (_instance == null)
                {
                    _instance = new DatabaseManager();
                }
            }

            return _instance;
        }

        //public ETHBotDBContext ETHBotDBContext;
        protected DatabaseManager()
        {
            //ETHBotDBContext = new ETHBotDBContext();
        }

        public DiscordUser GetDiscordUserById(ulong id)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordUsers.SingleOrDefault(i => i.DiscordUserId == id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public DiscordUser CreateDiscordUser(DiscordUser user)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.DiscordUsers.Add(user);
                    context.SaveChanges();
                }

                return GetDiscordUserById(user.DiscordUserId); // since this will rarely happen its fine i guess but we could probably return the original user      
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public void UpdateDiscordUser(DiscordUser user)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var dbUser = context.DiscordUsers.SingleOrDefault(i => i.DiscordUserId == user.DiscordUserId);
                    if (dbUser == null)
                        CreateDiscordUser(user);

                    bool changes = false;
                    if (dbUser.Nickname != user.Nickname)
                    {
                        dbUser.Nickname = user.Nickname;
                        changes = true;
                    }

                    if (dbUser.Username != user.Username)
                    {
                        dbUser.Username = user.Username;
                        changes = true;
                    }

                    if (dbUser.DiscriminatorValue != user.DiscriminatorValue)
                    {
                        dbUser.DiscriminatorValue = user.DiscriminatorValue;
                        changes = true;
                    }

                    // usually this doesnt change but some old users are wrong marked to fix this
                    if (dbUser.IsBot != user.IsBot)
                    {
                        dbUser.IsBot = user.IsBot;
                        changes = true;
                    }

                    // usually this doesnt change but some old users are wrong marked to fix this
                    if (dbUser.JoinedAt != user.JoinedAt)
                    {
                        dbUser.JoinedAt = user.JoinedAt;
                        changes = true;
                    }

                    if (dbUser.AvatarUrl != user.AvatarUrl)
                    {
                        dbUser.AvatarUrl = user.AvatarUrl;
                        changes = true;
                    }

                    // only update if new value is higher
                    if (dbUser.FirstDailyPostCount < user.FirstDailyPostCount)
                    {
                        dbUser.FirstDailyPostCount = user.FirstDailyPostCount;
                        changes = true;
                    }

                    if (changes)
                        context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public DiscordEmote GetEmoteByName(string emoteName)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordEmotes.FirstOrDefault(i => i.EmoteName.ToLower() == emoteName.ToLower());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public bool SetEmoteBlockStatus(ulong emoteId, bool blockStatus)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var emote = context.DiscordEmotes.FirstOrDefault(i => i.DiscordEmoteId == emoteId);
                    if (emote != null)
                    {
                        emote.Blocked = blockStatus;
                        context.SaveChanges();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }

        public List<DiscordEmote> GetEmotesByName(string name)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    // todo improve and better search
                    return context.DiscordEmotes.AsQueryable().Where(i => i.EmoteName.ToLower().Contains(name) && !i.Blocked).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public List<DiscordEmote> GetEmotesByDirectName(string name)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    // todo improve and better search
                    return context.DiscordEmotes.AsQueryable().Where(i => i.EmoteName.ToLower() == name.ToLower() && !i.Blocked).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public void UpdateBotSettings(BotSetting setting)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var dbSetting = context.BotSetting.FirstOrDefault();

                    if (dbSetting == null)
                        return;

                    dbSetting.SpaceXSubredditCheckCronJob = setting.SpaceXSubredditCheckCronJob;
                    dbSetting.LastSpaceXRedditPost = setting.LastSpaceXRedditPost;

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return;
            }
        }

        public BotSetting GetBotSettings()
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    // todo improve and better search
                    var settings = context.BotSetting.FirstOrDefault();

                    // temp workaround
                    if (settings == null)
                    {
                        // create empty record
                        context.BotSetting.Add(new BotSetting()
                        {
                            LastSpaceXRedditPost = null,
                            SpaceXSubredditCheckCronJob = "*/5 * * * *"
                        });
                        context.SaveChanges();

                        settings = context.BotSetting.FirstOrDefault();
                    }

                    return settings;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        // todo maybe a helpter method in case the local image is missing
        /*public EmojiStatistic SaveEmoteImage(ulong id, byte[] data)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var emote = context.EmojiStatistics.FirstOrDefault(i => i.EmojiId == id);
                    if (emote != null)
                    {
                        context.EmojiStatistics.FirstOrDefault(i => i.EmojiId == id).ImageData = data;
                        context.SaveChanges();
                    }
                    return emote;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }*/

        public DiscordServer GetDiscordServerById(ulong id)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordServers.SingleOrDefault(i => i.DiscordServerId == id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public DiscordServer CreateDiscordServer(DiscordServer server)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.DiscordServers.Add(server);
                    context.SaveChanges();
                }

                return GetDiscordServerById(server.DiscordServerId); // since this will rarely happen its fine i guess but we could probably return the original user
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public DiscordChannel GetDiscordChannel(ulong id)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordChannels.SingleOrDefault(i => i.DiscordChannelId == id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public void UpdateDiscordChannel(DiscordChannel channel)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var dbDiscordChannel = context.DiscordChannels.SingleOrDefault(i => i.DiscordChannelId == channel.DiscordChannelId);
                    if (dbDiscordChannel == null)
                        CreateDiscordChannel(channel);

                    bool changes = false;
                    if (dbDiscordChannel.ChannelName != channel.ChannelName)
                    {
                        dbDiscordChannel.ChannelName = channel.ChannelName;
                        changes = true;
                    }

                    if (changes)
                        context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public List<DiscordChannel> GetDiscordAllChannels(ulong serverId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordChannels.AsQueryable().Where(i => i.DiscordServerId == serverId).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public DiscordChannel CreateDiscordChannel(DiscordChannel channel)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.DiscordChannels.Add(channel);
                    context.SaveChanges();
                }

                return GetDiscordChannel(channel.DiscordChannelId); // since this will rarely happen its fine i guess but we could probably return the original user
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }


        public bool CreateDiscordMessage(DiscordMessage message, bool checkIfExists = false)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    if (checkIfExists && context.DiscordMessages.Any(i => i.DiscordMessageId == message.DiscordMessageId))
                        return false; // this message exists

                    context.DiscordMessages.Add(message);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }


            // .sql "select ri.Link, ri.IsNSFW from SubredditInfos si left join RedditPosts pp on si.SubredditId = pp.SubredditInfoId left join RedditImages ri on pp.RedditPostId = ri.RedditPostId where ri.Link is not null and pp.IsNSFW = (select RedditImageId % 2 from RedditImages ORDER BY RANDOM() LIMIT 1) and si.SubredditName like '%%' ORDER BY RANDOM() LIMIT 5"


            return true;
        }

        public void GetMessage()
        {
            // todo
        }


        public bool DeleteRantType(int typeId)
        {

            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    RantType r = new RantType() { RantTypeId = typeId };
                    context.RantTypes.Attach(r);
                    context.RantTypes.Remove(r);
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }


        public bool DeleteRantMessage(int rantId)
        {

            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    RantMessage r = new RantMessage() { RantMessageId = rantId };
                    context.RantMessages.Attach(r);
                    context.RantMessages.Remove(r);
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }


        public bool AddRantType(string type)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    if (!context.RantTypes.Any(i => i.Name.ToLower() == type.ToLower()))
                    {
                        context.RantTypes.Add(new RantType()
                        {
                            Name = type
                        });
                        context.SaveChanges();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }

        public ulong? GetOldestMessageAvailablePerChannel(ulong channelId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordMessages.AsQueryable().Where(i => i.DiscordChannelId == channelId).FirstOrDefault()?.DiscordMessageId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public int GetRantType(string type)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var rantType = context.RantTypes.SingleOrDefault(i => i.Name.ToLower() == type.ToLower());

                    if (rantType == null)
                        return -1;
                    return rantType.RantTypeId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return -1;
            }
        }

        public string GetRantTypeNameById(int typeId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var rantType = context.RantTypes.SingleOrDefault(i => i.RantTypeId == typeId);

                    if (rantType == null)
                        return null;
                    return rantType.Name;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public RantMessage GetRandomRant(string type = null)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    // TODO optimize
                    var rants = context.RantMessages.ToList();

                    if (type != null)
                    {
                        int rantTypeId = GetRantType(type);
                        rants = rants.Where(i => i.RantTypeId == rantTypeId).ToList();
                    }

                    if (rants.Count == 0)
                    {
                        return null;
                    }

                    var r = new Random();
                    return rants.ElementAt(r.Next(rants.Count()));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public Dictionary<int, string> GetAllRantTypes()
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    Dictionary<int, string> list = new Dictionary<int, string>();

                    var allTypes = context.RantTypes.ToList();

                    foreach (var item in allTypes)
                    {
                        list.Add(item.RantTypeId, item.Name);
                    }

                    return list;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }


        public bool AddRant(ulong messageId, ulong authorId, ulong channelId, int type, string content)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.RantMessages.Add(new RantMessage()
                    {
                        DiscordChannelId = channelId,
                        DiscordMessageId = messageId,
                        DiscordUserId = authorId,
                        RantTypeId = type,
                        Content = content
                    });
                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }

        public List<DiscordMessage> GetDiscordMessagesPaged(int skip)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordMessages.AsQueryable().Where(i => i.DiscordUser.IsBot == false).Skip(skip).Take(15_000).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }
        public BannedLink GetBannedLink(string link)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.BannedLinks.FirstOrDefault(i => i.Link == link);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }


        // TODO change to new tables
        /*public IEnumerable<EmojiStatistic> GetTopEmojiStatisticByBot(int count)
        {
            if (count < 1)
                count = 1;
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return null;// context.EmojiStatistics.AsQueryable().OrderByDescending(i => i.UsedByBots).Take(count).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }*/


        // TODO change to new tables
        /*public IEnumerable<EmojiStatistic> GetTopEmojiStatisticByText(int count)
        {
            if (count < 1)
                count = 1;
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return null;// context.EmojiStatistics.AsQueryable().OrderByDescending(i => i.UsedInText).Take(count).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }*/
        // TODO change to new tables
        /*public IEnumerable<EmojiStatistic> GetTopEmojiStatisticByTextOnce(int count)
        {
            if (count < 1)
                count = 1;
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return null;//context.EmojiStatistics.AsQueryable().OrderByDescending(i => i.UsedInTextOnce).Take(count).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }*/

        // TODO change to new tables
        /*public IEnumerable<EmojiStatistic> GetTopEmojiStatisticByReaction(int count)
        {
            if (count < 1)
                count = 1;
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return null;// context.EmojiStatistics.AsQueryable().OrderByDescending(i => i.UsedAsReaction).Take(count).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }*/


        // TODO change to new tables
        public IEnumerable<CommandStatistic> GetTopCommandUsage(int count, BotMessageType type)
        {
            if (count < 1)
                count = 1;

            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return null;// context.CommandStatistics.AsQueryable().Where(i => i.CommandTypeId == (int)type).OrderByDescending(i => i.Count).Take(count).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }


        public bool BanSubreddit(string subreddit)
        {
            var subredditInfo = GetSubreddit(subreddit);
            if (subredditInfo != null)
            {
                try
                {
                    using (ETHBotDBContext context = new ETHBotDBContext())
                    {
                        context.SubredditInfos.Single(i => i.SubredditId == subredditInfo.SubredditId).IsManuallyBanned = true;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    return false;
                }
            }
            return false;
        }


        public bool CreateBannedLink(BannedLink bannedLink)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.BannedLinks.Add(bannedLink);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
            return true;
        }

        public bool CreateBannedLink(string link, ulong userId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var user = GetDiscordUserById(userId);

                    context.BannedLinks.Add(new BannedLink()
                    {
                        Link = link,
                        AddedByDiscordUserId = user.DiscordUserId,
                        ReportTime = DateTimeOffset.Now
                    });
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }

            return true;
        }

        public PingStatistic GetPingStatisticByUserId(ulong userId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PingStatistics.SingleOrDefault(i => i.DiscordUser.DiscordUserId == userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public long TotalEmoteCount()
        {
            try
            {
                //using (ETHBotDBContext context = new ETHBotDBContext())
                //{
                //    return context.DiscordEmotes.Count();
                //}

                var sqlSelect = $@"SELECT COUNT(*) FROM DiscordEmotes";

                using (var connection = new MySqlConnection(Program.MariaDBReadOnlyConnectionstring))
                {
                    using (var command = new MySqlCommand(sqlSelect, connection))
                    {
                        command.CommandTimeout = 5;
                        connection.Open();

                        return (long)command.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return -1;
        }

        public void AddBotStartUp()
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.BotStartUpTimes.Add(new BotStartUpTime()
                    {
                        StartUpTime = DateTime.UtcNow
                    });

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public void AddPingStatistic(ulong userId, int count, SocketGuildUser sgUser)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var user = GetDiscordUserById(userId);
                    if (user == null)
                    {
                        // TODO can we still get the user or do we need the user to write atleast once?
                        return;
                    }
                    var stat = GetPingStatisticByUserId(userId);
                    if (stat == null)
                    {
                        context.PingStatistics.Add(new PingStatistic()
                        {
                            //DiscordUser = user,
                            PingCount = !sgUser.IsBot ? count : 0,
                            PingCountOnce = !sgUser.IsBot ? 1 : 0,
                            PingCountBot = !sgUser.IsBot ? 0 : count,
                            DiscordUserId = userId
                        });
                    }
                    else
                    {
                        // todo cleanup for perf im just lazy :/
                        var currStat = context.PingStatistics.SingleOrDefault(i => i.DiscordUser.DiscordUserId == userId);

                        currStat.PingCountOnce += !sgUser.IsBot ? 1 : 0;
                        currStat.PingCount += !sgUser.IsBot ? count : 0;
                        currStat.PingCountBot += !sgUser.IsBot ? 0 : count;
                    }

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }


        public DiscordEmote GetDiscordEmoteById(ulong id)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordEmotes.SingleOrDefault(i => i.DiscordEmoteId == id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public DiscordMessage GetDiscordMessageById(ulong? id)
        {
            if (id == null)
                return null;

            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordMessages.SingleOrDefault(i => i.DiscordMessageId == id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public BotChannelSetting GetChannelSetting(ulong channelId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.BotChannelSettings.SingleOrDefault(i => i.DiscordChannelId == channelId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public DiscordRole GetDiscordRole(ulong roleId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordRoles.SingleOrDefault(i => i.DiscordRoleId == roleId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public bool VerifyDiscordUserForPlace(ulong userId, bool verify)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.DiscordUsers.SingleOrDefault(i => i.DiscordUserId == userId).AllowedPlaceMultipixel = verify; // we ignore the possible null ref error since only admins can invoke
                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }

        public DiscordRole GetDiscordRole(DiscordRole role)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    // TODO Update role
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public DiscordRole CreateRole(DiscordRole role)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var dbRole = GetDiscordRole(role.DiscordRoleId);
                    if (dbRole != null)
                        return dbRole;

                    context.DiscordRoles.Add(role);
                    context.SaveChanges();

                    // get the created role -> could prob just return the role object
                    dbRole = GetDiscordRole(role.DiscordRoleId);

                    return dbRole;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public void CreatePingHistory(PingHistory history)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.PingHistory.Add(history);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public List<PingHistory> GetLastPingHistory(int amount, ulong? userId, ulong? roleId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PingHistory.AsQueryable().Where(i => i.DiscordRoleId == roleId && i.DiscordUserId == userId).OrderByDescending(i => i.PingHistoryId).Take(amount).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }
        public List<DiscordEmoteHistory> GetEmoteHistoryUsage(DateTime from, DateTime to)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordEmoteHistory.AsQueryable().Where(i => i.DateTimePosted > from && i.DateTimePosted < to).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public Dictionary<DateTime, int> GetMessageCountGrouped(DateTime from, DateTime to, int groupByMinutes)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var messageTimes = context.DiscordMessages.AsQueryable().Select(i => SnowflakeUtils.FromSnowflake(i.DiscordMessageId)).ToList().Where(i => from < i && i < to).ToList();


                    var groups = messageTimes.GroupBy(x =>
                    {
                        var stamp = x;
                        stamp = stamp.AddMinutes(-(stamp.Minute % groupByMinutes));
                        stamp = stamp.AddMilliseconds(-stamp.Millisecond - 1000 * stamp.Second);
                        return stamp;
                    }).Select(g => new { TimeStamp = g.Key, Value = g.Count() });

                    return groups.ToDictionary(g => g.TimeStamp.DateTime, g => g.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public void UpdateChannelSetting(ulong channelId, int permission, ulong oldestMessageId = 0, ulong newestMessageId = 0, bool? reachedEnd = null, bool onlyIfNew = false)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var channelSetting = GetChannelSetting(channelId);
                    if (channelSetting == null)
                    {
                        context.BotChannelSettings.Add(new BotChannelSetting()
                        {
                            DiscordChannelId = channelId,
                            ChannelPermissionFlags = permission
                        });
                    }
                    else if (!onlyIfNew)
                    {
                        var settings = context.BotChannelSettings.Single(i => i.DiscordChannelId == channelId);

                        if (permission > -1)
                            settings.ChannelPermissionFlags = permission;

                        if (oldestMessageId > 0)
                            settings.OldestPostTimePreloaded = SnowflakeUtils.FromSnowflake(oldestMessageId);
                        if (newestMessageId > 0)
                            settings.NewestPostTimePreloaded = SnowflakeUtils.FromSnowflake(newestMessageId);
                        if (reachedEnd.HasValue)
                            settings.ReachedOldestPreload = reachedEnd.Value;
                    }
                    context.SaveChanges();

                    // TODO better way
                    Program.LoadChannelSettings();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public List<BotChannelSetting> GetAllChannelSettings()
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.BotChannelSettings.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public DiscordEmote AddDiscordEmote(DiscordEmote discordEmote)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var dbEmote = GetDiscordEmoteById(discordEmote.DiscordEmoteId);
                    if (dbEmote == null)
                    {
                        context.DiscordEmotes.Add(discordEmote);
                        context.SaveChanges();
                    }
                    else
                    {
                        return null;
                    }
                }

                return GetDiscordEmoteById(discordEmote.DiscordEmoteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }


        private string MoveEmoteToDisk(DiscordEmote emote, byte[] imageData)
        {
            var emojiDate = SnowflakeUtils.FromSnowflake(emote.DiscordEmoteId);

            string additionalFolder = $"{emojiDate.Year}-{emojiDate.Month:00}";
            string path = Path.Combine(Program.BasePath, "Emotes", additionalFolder);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            int i = emote.Url.LastIndexOf(".");
            string fileEnding = emote.Url.Substring(i, emote.Url.Length - i);

            string filePath = Path.Combine(path, $"{emote.DiscordEmoteId}{fileEnding}");

            File.WriteAllBytes(filePath, imageData);

            return filePath;
        }


        // TODO change for new emote table
        public async void ProcessDiscordEmote(DiscordEmote emote, ulong? discordMessageId, int count, bool isReaction, SocketGuildUser user, bool isPreload)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var dbEmoji = GetDiscordEmoteById(emote.DiscordEmoteId);
                    if (dbEmoji == null)
                    {
                        using (var webClient = new WebClient())
                        {
                            byte[] bytes = webClient.DownloadData(emote.Url);

                            string filePath = MoveEmoteToDisk(emote, bytes);

                            emote.LocalPath = filePath;

                            context.DiscordEmotes.Add(emote);
                        }
                        context.SaveChanges();
                    }


                    var emojiStat = context.DiscordEmoteStatistics.SingleOrDefault(i => i.DiscordEmoteId == emote.DiscordEmoteId);
                    if (emojiStat == null)
                    {
                        context.DiscordEmoteStatistics.Add(new DiscordEmoteStatistic()
                        {
                            DiscordEmoteId = emote.DiscordEmoteId,
                            UsedAsReaction = !user.IsBot && isReaction ? count : 0,
                            UsedInText = !user.IsBot && !isReaction ? count : 0,
                            UsedInTextOnce = !user.IsBot && !isReaction ? 1 : 0,
                            UsedByBots = user.IsBot && !isReaction ? count : 0
                        });

                    }
                    else
                    {
                        emojiStat.UsedAsReaction += !user.IsBot && isReaction ? count : 0;
                        emojiStat.UsedInText += !user.IsBot && !isReaction ? count : 0;
                        emojiStat.UsedInTextOnce += !user.IsBot && !isReaction ? 1 : 0;
                        emojiStat.UsedByBots += user.IsBot && !isReaction ? count : 0;
                    }

                    var message = GetDiscordMessageById(discordMessageId);



                    if (message == null)
                        discordMessageId = null;

                    DateTime posted = DateTime.Now;

                    if (isPreload && discordMessageId.HasValue)
                    {
                        posted = SnowflakeUtils.FromSnowflake(discordMessageId.Value).UtcDateTime;
                    }

                    context.DiscordEmoteHistory.Add(new DiscordEmoteHistory()
                    {
                        DateTimePosted = posted,
                        Count = count,
                        IsReaction = isReaction,

                        DiscordUserId = user.Id,
                        DiscordEmoteId = emote.DiscordEmoteId,
                        DiscordMessageId = discordMessageId

                    });

                    context.SaveChanges();

                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return;
            }

            //return GetEmojiStatisticById(emote.DiscordEmoteId);
        }



        public CommandStatistic GetCommandStatisticById(BotMessageType type, ulong userId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                    return context.CommandStatistics.SingleOrDefault(i => i.DiscordUserId == userId && i.CommandTypeId == (int)type);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public CommandType GetCommandTypeById(int id)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                return context.CommandTypes.Single(i => i.CommandTypeId == id);
            }
        }
        public void AddCommandStatistic(BotMessageType type, ulong userId)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                //var dbCommand = GetCommandStatisticById(type, userId);
                var dbCommand = context.CommandStatistics.SingleOrDefault(i => i.DiscordUserId == userId && i.CommandTypeId == (int)type);
                if (dbCommand == null)
                {
                    var dbUser = GetDiscordUserById(userId);// maybe not even needed
                    var commandType = GetCommandTypeById((int)type);

                    context.CommandStatistics.Add(new CommandStatistic()
                    {
                        Count = 1,
                        CommandTypeId = commandType.CommandTypeId,
                        DiscordUserId = userId
                    });
                }
                else
                {
                    dbCommand.Count++;
                }


                context.SaveChanges();
            }
        }


        // TODO LOGS
        public IEnumerable<CommandStatistic> GetTopStatisticByType(BotMessageType type, int amount = 10)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                return context.CommandStatistics.AsQueryable().Where(i => i.Type.CommandTypeId == (int)type).OrderByDescending(i => i.Count).Take(amount).ToList(); // TODO check it works
            }
        }

        public bool IsSaveMessage(ulong messageId, ulong savedByDiscordUserId)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                return context.SavedMessages.Any(i => i.DiscordMessageId == messageId && i.SavedByDiscordUserId == savedByDiscordUserId); // TODO check it works
            }
        }

        public bool SaveMessage(ulong messageId, ulong byDiscordUserId, ulong savedByDiscordUserId, string link, string content)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                try
                {
                    //var user = GetDiscordUserById(byDiscordUserId); // Verify the user is created but should actually be available by this poitn
                    //var saveBy = GetDiscordUserById(savedByDiscordUserId); // Verify the user is created but should actually be available by this poitn
                    //ETHBotDBContext.SaveChanges();
                    var newSave = new SavedMessage()
                    {
                        DirectLink = link,
                        SendInDM = false,
                        ByDiscordUserId = byDiscordUserId,
                        //ByDiscordUser = user,
                        Content = content, // todo attachment
                        DiscordMessageId = messageId,
                        SavedByDiscordUserId = savedByDiscordUserId
                        //SavedByDiscordUser = saveBy
                    };

                    //context.DiscordUsers.Attach(user);
                    //context.DiscordUsers.Attach(saveBy);
                    context.SavedMessages.Add(newSave);
                    context.SaveChanges();

                }
                catch (Exception ex)
                {

                }
            }

            return true;
        }

        public void SetAllSubredditsStatus(bool status = false)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                var subreddits = context.SubredditInfos.AsQueryable().Where(i => i.IsScraping != status);
                foreach (var subreddit in subreddits)
                {
                    subreddit.IsScraping = status;
                }

                context.SaveChanges();
            }
        }

        public bool SetSubredditScaperStatus(string subreddit, bool status)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var subredditInfo = GetSubreddit(subreddit);
                    if (subredditInfo == null)
                        return false;

                    context.SubredditInfos.Single(i => i.SubredditId == subredditInfo.SubredditId).IsScraping = status;

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }


        public SubredditInfo GetSubreddit(string subreddit)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                return context.SubredditInfos.SingleOrDefault(i => i.SubredditName == subreddit);
            }
        }

        public SubredditInfo GetSubreddit(int id)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                return context.SubredditInfos.SingleOrDefault(i => i.SubredditId == id);
            }
        }

        public List<SubredditInfo> GetSubredditsByStatus(bool status = true)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                return context.SubredditInfos.AsQueryable().Where(i => i.IsScraping == status).ToList();
            }
        }

        public RedditPost GetRedditPostById(int redditPostId)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                return context.RedditPosts.SingleOrDefault(i => i.RedditPostId == redditPostId);
            }
        }

        public string GetRandomImage(string subreddit)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                Random r = new Random();


                var subredditInfo = GetSubreddit(subreddit);


                var posts = context.RedditPosts.AsQueryable().Where(i => i.SubredditInfo.SubredditId == subredditInfo.SubredditId).OrderBy(i => r.Next(0, 1000));


                //.First().RedditImages.First().Link; // I know this is performance garbage but its 1 AM so fuck you well slept future me who thinks is smarter than my past me
                return posts.First().RedditImages.First().Link;
            }
        }
        // TODO EXCEPTION LOGGING


        // TODO Get saved message

    }
}
