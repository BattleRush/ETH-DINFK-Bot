using Discord;
using Microsoft.Extensions.Logging;
using Reddit;
using RedditScrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETHDINFKBot.CronJobs.Jobs
{
    public class SpaceXSubredditJob : CronJobService
    {
        private readonly ILogger<SpaceXSubredditJob> _logger;
        private readonly string Name = "SpaceXSubredditJob";

        private readonly ulong GuildId = 747752542741725244;
        private readonly ulong ChannelId = 817846795367481344; // todo config?

        public SpaceXSubredditJob(IScheduleConfig<SpaceXSubredditJob> config, ILogger<SpaceXSubredditJob> logger)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} starts.");
            return base.StartAsync(cancellationToken);
        }

        public override Task DoWork(CancellationToken cancellationToken)
        {
            try
            {
                if (Program.Client == null)
                    return Task.CompletedTask;

                _logger.LogInformation($"{DateTime.Now:hh:mm:ss} {Name} is working.");
                var guild = Program.Client.GetGuild(GuildId);
                var textChannel = guild.GetTextChannel(ChannelId);

                var dbManager = DatabaseManager.Instance();
                var settings = dbManager.GetBotSettings(); // could prob use the object from program cs

                // TODO DEV Only
                //settings.LastSpaceXRedditPost = "t3_m2x82i";

                if (settings?.LastSpaceXRedditPost == null)
                {
                    _logger.LogInformation($"{DateTime.Now:hh:mm:ss} No SpaceX subreddit info defined. Abort.");
                    return Task.CompletedTask;
                }


                string subredditName = "spacex"; // spacex

                var reddit = new RedditClient(Program.ApplicationSetting.RedditSetting.AppId, Program.ApplicationSetting.RedditSetting.RefreshToken, Program.ApplicationSetting.RedditSetting.AppSecret);

                SubredditManager subManager = new SubredditManager(subredditName, reddit, null, settings.LastSpaceXRedditPost, null);

                // TODO maybe use MonitorNew ?

                var posts = subManager.GetBeforePosts();

                if (posts == null)
                    return Task.CompletedTask;

                if (posts.Count > 0)
                {
                    foreach (var redditPost in posts)
                    {

                        EmbedBuilder builder = new EmbedBuilder();
                        var title = redditPost.Title;
                        builder.WithTitle(title.Substring(0, Math.Min(256, title.Length)));
                        builder.WithUrl("https://www.reddit.com/" + redditPost.Permalink);

                        var content = redditPost.Listing.IsSelf ? redditPost.Listing.SelfText : "";

                        if (content.Length > 2000)
                        {
                            content = content.Substring(0, 2000);
                        }

                        // TODO if subreddit name null get the subreddit 
                        builder.WithDescription(content);
                        builder.WithColor(0, 0, 255);

                        //builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");
                        builder.WithCurrentTimestamp();
                        string url = redditPost.Listing.URL;
                        if (url.Contains("v.redd.it"))
                        {
                            // TODO Handle video case this doesnt work in embed yet
                            url += "/DASH_720.mp4";
                        }

                        builder.WithImageUrl(url);
                        builder.AddField("Infos", $"Posted by: {redditPost.Author} in /r/{subredditName} at {redditPost.Created}");

                        textChannel.SendMessageAsync("", false, builder.Build());


                    }

                    settings.LastSpaceXRedditPost = posts.OrderByDescending(i => i.Created).First().Fullname; // newest is on top
                    dbManager.UpdateBotSettings(settings);
                }
            }
            catch(Exception ex)
            {

            }

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
