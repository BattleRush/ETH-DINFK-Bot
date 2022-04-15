using ETHBot.DataLayer;
using ETHBot.DataLayer.Data.Reddit;
using Reddit;
using Reddit.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedditScrapper
{
    public class SubredditManager
    {
        public readonly string SubredditName;
        private RedditClient API;
        private string NewestPost;
        private DateTime NewestPostDate;
        private string OldestPost;
        private DateTime OldestPostDate;

        private Subreddit Subreddit;
        public SubredditInfo SubredditInfo;

        private ETHBotDBContext ETHBotDBContext;

        public SubredditManager(string subreddit, RedditClient api, ETHBotDBContext context)
            : this(subreddit, api, context, null, null) { }

        public SubredditManager(string subreddit, RedditClient api, ETHBotDBContext context, string before, string after)
        {
            SubredditName = subreddit;
            API = api;
            NewestPost = before;
            OldestPost = after;
            ETHBotDBContext = context;

            Subreddit = API.Subreddit(SubredditName);

            // dont load info from db if we manually are managing this
            if (before == null && after == null)
                LoadSubredditInfo();
        }

        public void LoadSubredditInfo()
        {
            // TODO overwrite info from db -> over init items
            SubredditInfo = ETHBotDBContext.SubredditInfos.SingleOrDefault(i => i.SubredditName == SubredditName);
            if (SubredditInfo == null)
            {
                ETHBotDBContext.SubredditInfos.Add(new SubredditInfo()
                {
                    SubredditDescription = Subreddit.Description,
                    SubredditName = Subreddit.Name,
                    IsNSFW = Subreddit.Over18 ?? false // TODO correct to assume false otherwise?
                });

                ETHBotDBContext.SaveChanges();
            }

            // TODO Check if null
            SubredditInfo = ETHBotDBContext.SubredditInfos.SingleOrDefault(i => i.SubredditName == SubredditName);
            OldestPost = SubredditInfo.OldestPost;
            NewestPost = SubredditInfo.NewestPost;
        }
        public List<Post> GetAfterPosts()
        {
            return Subreddit.Posts.GetNew(OldestPost);
        }

        public List<Post> GetBeforePosts()
        {   
            try
            {
                return Subreddit.Posts.GetNew("", NewestPost);
            }
            catch(Exception ex)
            {
                return null;
            }
        }
        public List<Post> GetBetweenPosts()
        {
            return Subreddit.Posts.GetNew(OldestPost, NewestPost);
        }

        public void ConfirmOldestPost(string post, DateTime dateTime, bool end = false)
        {
            OldestPost = post;
            OldestPostDate = dateTime;

            if (end)
            {
                SubredditInfo.ReachedOldest = true;
            }

            SubredditInfo.OldestPost = OldestPost;
            SubredditInfo.OldestPostDate = OldestPostDate;
            ETHBotDBContext.SaveChanges();
        }

        public void ConfirmNewestPost(string post, DateTime dateTime)
        {
            NewestPost = post;
            NewestPostDate = dateTime;

            SubredditInfo.NewestPost = NewestPost;
            SubredditInfo.NewestPostDate = NewestPostDate;

            ETHBotDBContext.SaveChanges();
        }

    }
}
