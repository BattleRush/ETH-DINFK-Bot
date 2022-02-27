using Discord;
using Discord.WebSocket;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data.Enums;
using ETHBot.DataLayer.Data.Reddit;
using ETHDINFKBot.Data;
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
        public static EmoteDBManager EmoteDatabaseManager = EmoteDBManager.Instance();
        public static KeyValueDBManager KeyValueManager = KeyValueDBManager.Instance();

        // TODO Move all methods to Data/
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

        // TODO consider a filter by server?
        public List<DiscordUser> GetDiscordUsers()
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordUsers.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public List<DiscordUser> GetTopFirstDailyPosterDiscordUsers(int amount = 10)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordUsers.AsQueryable().OrderByDescending(i => i.FirstDailyPostCount).Take(amount).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public List<DiscordUser> GetTopFirstAfternoonPosterDiscordUsers(int amount = 10)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordUsers.AsQueryable().OrderByDescending(i => i.FirstAfternoonPostCount).Take(amount).ToList();
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

                    if (dbUser.FirstAfternoonPostCount < user.FirstAfternoonPostCount)
                    {
                        dbUser.FirstAfternoonPostCount = user.FirstAfternoonPostCount;
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

        public BotSetting SetBotSettings(BotSetting botSetting)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    // todo improve and better search
                    var settings = context.BotSetting.FirstOrDefault();

                    // temp workaround
                    if (settings != null)
                    {
                        //settings.ChannelOrderLocked = botSetting.ChannelOrderLocked;
                        settings.PlacePixelIdLastChunked = botSetting.PlacePixelIdLastChunked;
                        settings.PlaceLastChunkId = botSetting.PlaceLastChunkId;
                    }

                    context.SaveChanges();

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

        public DiscordChannel GetDiscordChannel(ulong? id)
        {
            if (id == null)
                return null;

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

        public DiscordThread GetDiscordThread(ulong? id)
        {
            if (id == null)
                return null;

            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordThreads.SingleOrDefault(i => i.DiscordThreadId == id);
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
                    {
                        CreateDiscordChannel(channel);
                        return;
                    }

                    bool changes = false;
                    if (dbDiscordChannel.ChannelName != channel.ChannelName)
                    {
                        dbDiscordChannel.ChannelName = channel.ChannelName;
                        changes = true;
                    }

                    if (dbDiscordChannel.ParentDiscordChannelId != channel.ParentDiscordChannelId)
                    {
                        dbDiscordChannel.ParentDiscordChannelId = channel.ParentDiscordChannelId;
                        changes = true;
                    }

                    if (dbDiscordChannel.Position != channel.Position)
                    {
                        dbDiscordChannel.Position = channel.Position;
                        changes = true;
                    }

                    // Likely never changes
                    if (dbDiscordChannel.IsCategory != channel.IsCategory)
                    {
                        dbDiscordChannel.IsCategory = channel.IsCategory;
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

        public void UpdateDiscordThread(DiscordThread thread)
        {
            try
            {
                using (ETHBotDBContext context = new())
                {
                    var dbDiscordThread = context.DiscordThreads.SingleOrDefault(i => i.DiscordThreadId == thread.DiscordThreadId);
                    if (dbDiscordThread == null)
                    {
                        CreateDiscordThread(thread);
                        return;
                    }

                    // TODO change the logic

                    bool changes = false;
                    if (dbDiscordThread.ThreadName != thread.ThreadName)
                    {
                        dbDiscordThread.ThreadName = thread.ThreadName;
                        changes = true;
                    }

                    if (dbDiscordThread.MemberCount != thread.MemberCount)
                    {
                        dbDiscordThread.MemberCount = thread.MemberCount;
                        changes = true;
                    }

                    if (dbDiscordThread.IsArchived != thread.IsArchived)
                    {
                        dbDiscordThread.IsArchived = thread.IsArchived;
                        changes = true;
                    }
                    if (dbDiscordThread.IsLocked != thread.IsLocked)
                    {
                        dbDiscordThread.IsLocked = thread.IsLocked;
                        changes = true;
                    }
                    if (dbDiscordThread.IsNsfw != thread.IsNsfw)
                    {
                        dbDiscordThread.IsNsfw = thread.IsNsfw;
                        changes = true;
                    }
                    if (dbDiscordThread.IsPrivateThread != thread.IsPrivateThread)
                    {
                        dbDiscordThread.IsPrivateThread = thread.IsPrivateThread;
                        changes = true;
                    }

                    if (dbDiscordThread.ThreadType != thread.ThreadType)
                    {
                        dbDiscordThread.ThreadType = thread.ThreadType;
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

        public DiscordThread CreateDiscordThread(DiscordThread thread)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.DiscordThreads.Add(thread);
                    context.SaveChanges();
                }

                return GetDiscordThread(thread.DiscordThreadId); // since this will rarely happen its fine i guess but we could probably return the original user
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
            // TODO Detect duplicate DiscordUserIds -> Create Unique on DiscordUserId
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    // TODO Change back to SingleOrDefault as soon the duplicate DiscordUserId issue is resolved
                    return context.PingStatistics.FirstOrDefault(i => i.DiscordUser.DiscordUserId == userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
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

        public DateTime GetLastBotStartUpTime()
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.BotStartUpTimes.Max(i => i.StartUpTime);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return DateTime.MinValue;
        }

        public void AddPingStatistic(ulong userId, int count, SocketGuildUser sgUser)
        {
            // TODO Prevent duplicates -> DB Constraint
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
                        var currStat = context.PingStatistics.FirstOrDefault(i => i.DiscordUser.DiscordUserId == userId);

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

        /// <summary>
        /// Queries the last queryMessageLength messages to see if someone replied to it.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="userId"></param>
        /// <param name="queryMessageLength"></param>
        /// <returns></returns>
        public List<PingHistory> GetLastReplyHistory(int amount, ulong userId, int queryMessageLength = 250)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var messageIds = context.DiscordMessages.AsQueryable().Where(i => i.DiscordUserId == userId).OrderByDescending(i => i.DiscordMessageId).Take(queryMessageLength).Select(i => i.DiscordMessageId).ToList();

                    // We query only in the last 10k messages for performance reasons
                    var messages = context.DiscordMessages.AsQueryable().OrderByDescending(i => i.DiscordMessageId).Take(10_000);/*Retreive the last 10k messages into memory*///.Where(i => messageIds.Contains(i.ReplyMessageId ?? 0));

                    List<DiscordMessage> replyMessages = new List<DiscordMessage>();

                    // TODO Check performance -> create a direct SQL query for better perf
                    foreach (var message in messages)
                    {
                        if (messageIds.Contains(message.ReplyMessageId ?? 0))
                            replyMessages.Add(message);
                    }

                    List<PingHistory> returnValue = new List<PingHistory>();

                    // Priorotize newer replies
                    foreach (var replyMessage in replyMessages.OrderByDescending(i => i.DiscordMessageId))
                    {
                        returnValue.Add(new PingHistory()
                        {
                            DiscordMessageId = replyMessage.DiscordMessageId,
                            DiscordRoleId = 1, // TODO Add flag to implement it better -> for now DiscordRoleId = 1 -> ReplyPing
                            FromDiscordUserId = replyMessage.DiscordUserId,
                            DiscordUserId = userId,
                            PingHistoryId = -1
                        });

                        if (returnValue.Count == amount)
                            break;
                    }

                    return returnValue;
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
                return context.CommandTypes.SingleOrDefault(i => i.CommandTypeId == id);
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
                    if (commandType == null)
                        return;

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
        public bool DeleteInDmSavedMessage(ulong messageId)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                try
                {
                    var savedMessage = context.SavedMessages.SingleOrDefault(i => i.DMDiscordMessageId == messageId);
                    if (savedMessage != null)
                    {
                        savedMessage.DeletedFromDM = true;
                        context.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    return false;
                }

                return true;
            }
        }

        public bool SaveMessage(ulong messageId, ulong byDiscordUserId, ulong savedByDiscordUserId, string link, string content, bool byMessageCommand, ulong? dmMessageId = null)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                bool isTracked = GetDiscordMessageById(messageId) != null;
                //    return false; // this message isnt tracked

                try
                {
                    //var user = GetDiscordUserById(byDiscordUserId); // Verify the user is created but should actually be available by this poitn
                    //var saveBy = GetDiscordUserById(savedByDiscordUserId); // Verify the user is created but should actually be available by this poitn
                    //ETHBotDBContext.SaveChanges();
                    var newSave = new SavedMessage()
                    {
                        DirectLink = link,
                        ByDiscordUserId = byDiscordUserId,
                        //ByDiscordUser = user,
                        Content = isTracked ? content : "Not tracked", // todo attachment
                        DiscordMessageId = isTracked ? messageId : null,
                        SavedByDiscordUserId = savedByDiscordUserId,
                        TriggeredByCommand = byMessageCommand,
                        DMDiscordMessageId = dmMessageId
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
