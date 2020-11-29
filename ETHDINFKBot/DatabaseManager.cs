using Discord;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data.Enums;
using ETHBot.DataLayer.Data.Reddit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ETHDINFKBot
{
    public class DatabaseManager
    {
        private static DatabaseManager _instance;
        private static object syncLock = new object();

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

        private ETHBotDBContext ETHBotDBContext;
        protected DatabaseManager()
        {
            ETHBotDBContext = new ETHBotDBContext();
        }

        public DiscordUser GetDiscordUserById(ulong id)
        {
            return ETHBotDBContext.DiscordUsers.SingleOrDefault(i => i.DiscordUserId == id);
        }

        public DiscordUser CreateUser(DiscordUser user)
        {
            ETHBotDBContext.DiscordUsers.Add(user);
            ETHBotDBContext.SaveChanges();

            return GetDiscordUserById(user.DiscordUserId); // since this will rarely happen its fine i guess but we could probably return the original user
        }

        public DiscordServer GetDiscordServerById(ulong id)
        {
            return ETHBotDBContext.DiscordServers.SingleOrDefault(i => i.DiscordServerId == id);
        }

        public DiscordServer CreateDiscordServer(DiscordServer server)
        {
            ETHBotDBContext.DiscordServers.Add(server);
            ETHBotDBContext.SaveChanges();

            return GetDiscordServerById(server.DiscordServerId); // since this will rarely happen its fine i guess but we could probably return the original user
        }


        public DiscordChannel GetDiscordChannel(ulong id)
        {
            return ETHBotDBContext.DiscordChannels.SingleOrDefault(i => i.DiscordChannelId == id);
        }
        public DiscordChannel CreateDiscordChannel(DiscordChannel channel)
        {
            ETHBotDBContext.DiscordChannels.Add(channel);
            ETHBotDBContext.SaveChanges();

            return GetDiscordChannel(channel.DiscordChannelId); // since this will rarely happen its fine i guess but we could probably return the original user
        }


        public bool CreateDiscordMessage(DiscordMessage message)
        {
            try
            {
                ETHBotDBContext.DiscordMessages.Add(message);
                ETHBotDBContext.SaveChanges();
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public void GetMessage()
        {
            // todo
        }


        public BannedLink GetBannedLink(string link)
        {
            return ETHBotDBContext.BannedLinks.FirstOrDefault(i => i.Link == link);
        }



        public bool CreateBannedLink(BannedLink bannedLink)
        {
            ETHBotDBContext.BannedLinks.Add(bannedLink);
            ETHBotDBContext.SaveChanges();

            return true;
        }

        public bool CreateBannedLink(string link, ulong userId)
        {
            var user = GetDiscordUserById(userId);

            ETHBotDBContext.BannedLinks.Add(new BannedLink()
            {
                Link = link,
                ByUser = user,
                ReportTime = DateTimeOffset.Now
            });
            ETHBotDBContext.SaveChanges();

            return true;
        }

        public EmojiStatistic GetEmojiStatisticById(ulong id)
        {
            return ETHBotDBContext.EmojiStatistics.SingleOrDefault(i => i.EmojiId == id);
        }

        public EmojiStatistic AddEmojiStatistic(EmojiStatistic statistic, int count, bool isReaction)
        {
            var dbEmoji = GetEmojiStatisticById(statistic.EmojiId);
            if (dbEmoji == null)
            {
                ETHBotDBContext.EmojiStatistics.Add(statistic);
            }
            else
            {
                dbEmoji.UsedAsReaction += statistic.UsedAsReaction;
                dbEmoji.UsedInText += statistic.UsedInText;
                dbEmoji.UsedInTextOnce += 1;// statistic.UsedInTextOnce;
            }

            ETHBotDBContext.EmojiHistory.Add(new EmojiHistory()
            {
                DateTimePosted = DateTime.Now,
                Count = count,
                IsReaction = isReaction,
                EmojiStatistic = dbEmoji
            });

            ETHBotDBContext.SaveChanges();

            return GetEmojiStatisticById(statistic.EmojiId);
        }

        public CommandStatistic GetCommandStatisticById(BotMessageType type, ulong userId)
        {
            return ETHBotDBContext.CommandStatistics.SingleOrDefault(i => i.User.DiscordUserId == userId && i.Type.CommandTypeId == (int)type);
        }
        public CommandType GetCommandTypeById(int id)
        {
            return ETHBotDBContext.CommandTypes.Single(i => i.CommandTypeId == id);
        }
        public void AddCommandStatistic(BotMessageType type, ulong userId)
        {
            var dbCommand = GetCommandStatisticById(type, userId);
            if (dbCommand == null)
            {
                dbCommand.Count++;
            }
            else
            {
                var dbUser = GetDiscordUserById(userId);
                var commandType = GetCommandTypeById((int)type);

                ETHBotDBContext.CommandStatistics.Add(new CommandStatistic()
                {
                    Count = 1,
                    Type = commandType,
                    User = dbUser
                });
            }


            ETHBotDBContext.SaveChanges();
        }

        public CommandStatistic GetTopStatisticByType(BotMessageType type)
        {
            return ETHBotDBContext.CommandStatistics.AsQueryable().Where(i => i.Type.CommandTypeId == (int)type).OrderByDescending(i => i.Count).First(); // TODO check it works
        }


        public bool SaveMessage(SavedMessage message)
        {
            ETHBotDBContext.SavedMessages.Add(message);
            ETHBotDBContext.SaveChanges();

            return true;
        }


        public SubredditInfo GetSubreddit(string subreddit)
        {
            return ETHBotDBContext.SubredditInfos.SingleOrDefault(i => i.SubredditName == subreddit);
        }

        public string GetRandomImage(string subreddit)
        {
            Random r = new Random();


            var subredditInfo = GetSubreddit(subreddit);



            var posts = ETHBotDBContext.RedditPosts.AsQueryable().Where(i => i.SubredditInfo.SubredditId == subredditInfo.SubredditId).OrderBy(i => r.Next(0, 1000));


            //.First().RedditImages.First().Link; // I know this is performance garbage but its 1 AM so fuck you well slept future me who thinks is smarter than my past me
            return posts.First().RedditImages.First().Link;
        }

        // TODO Get saved message

    }
}
