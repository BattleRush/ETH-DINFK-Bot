using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data.Enums;
using Reddit;
using Reddit.Controllers;
using RedditScrapper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Modules
{
    [Group("admin")]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public async Task AdminHelp()
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Admin Help (Admin only)");

            builder.WithColor(0, 0, 255);

            builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");
            builder.WithCurrentTimestamp();
            builder.AddField("admin help", "This message :)");
            builder.AddField("admin channel help", "Help for channel command");
            builder.AddField("admin reddit help", "Help for reddit command");
            builder.AddField("admin rant help", "Help for rant command");
            builder.AddField("admin kill", "Do I really need to explain this one");

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("kill")]
        public async Task AdminKill()
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }
            Context.Channel.SendMessageAsync("I'll be back!", false);
            Process.GetCurrentProcess().Kill();
        }


        [Group("rant")]
        public class RantAdminModule : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            public async Task AdminHelp()
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Admin Help (Admin only)");

                builder.WithColor(0, 0, 255);

                builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");
                builder.WithCurrentTimestamp();
                builder.AddField("admin rant help", "This message :)");
                builder.AddField("admin rant all", "List all types");
                builder.AddField("admin rant add <type>", "Add new type (open for all)");
                builder.AddField("admin rant dt <type id>", "Delete type");
                builder.AddField("admin rant dr <rant id>", "Delete rant");

                Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("add")]
            public async Task AddRantType(string type)
            {
                /*var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }*/

                bool success = DatabaseManager.Instance().AddRantType(type);
                Context.Channel.SendMessageAsync($"Added {type} Success: {success}", false);
            }

            [Command("dt")]
            public async Task DeleteRantType(int typeId)
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                bool success = DatabaseManager.Instance().DeleteRantType(typeId);
                Context.Channel.SendMessageAsync("Delete success: " + success, false);
            }


            [Command("dr")]
            public async Task DeleteRantMessage(int typeId)
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                bool success = DatabaseManager.Instance().DeleteRantMessage(typeId);
                Context.Channel.SendMessageAsync("Delete success: " + success, false);
            }
        }


        [Group("channel")]
        public class ChannelAdminModule : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            public async Task ChannelAdminHelp()
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Reddit Admin Help (Admin only)");

                builder.WithColor(0, 0, 255);

                builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");
                builder.WithCurrentTimestamp();
                builder.AddField("admin channel help", "This message :)");
                builder.AddField("admin channel info", "Returns info about the current channel settings");
                builder.AddField("admin channel set <permission>", "Set permissions for the current channel");
                builder.AddField("admin channel all <permission>", "Set the MINIMUM permissions for ALL channels");
                builder.AddField("admin channel flags", "Returns help with the flag infos");

                Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            /* public static IEnumerable<Enum> GetAllFlags(Enum e)
             {
                 return Enum.GetValues(e.GetType()).Cast<Enum>();
             }

             // TODO move to somewhere common
             static IEnumerable<Enum> GetFlags(Enum input)
             {
                 foreach (Enum value in Enum.GetValues(input.GetType()))
                     if (input.HasFlag(value))
                         yield return value;
             }*/

            [Command("info")]
            public async Task GetChannelInfoAsync()
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                if (Context.Message.Channel is SocketGuildChannel guildChannel)
                {
                    var channelInfo = DatabaseManager.Instance().GetChannelSetting(guildChannel.Id);

                    if (channelInfo == null)
                    {
                        Context.Channel.SendMessageAsync("channelInfo is null bad admin", false);
                        return;
                    }

                    EmbedBuilder builder = new EmbedBuilder();

                    builder.WithTitle($"Channel Info for {guildChannel.Name}");

                    builder.WithColor(255, 0, 0);

                    builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");
                    builder.WithCurrentTimestamp();




                    builder.AddField("Permission flag", channelInfo.ChannelPermissionFlags);

                    //var count = Enum.GetValues(typeof(BotPermissionType)).Length;

                    foreach (BotPermissionType flag in Enum.GetValues(typeof(BotPermissionType)))
                    {
                        if (((BotPermissionType)channelInfo.ChannelPermissionFlags).HasFlag(flag))
                        {
                            builder.AddField(flag.ToString(), "```diff\r\n+ YES```", true);
                        }
                        else
                        {
                            builder.AddField(flag.ToString(), "```diff\r\n- NO```", true);
                        }
                    }


                    Context.Channel.SendMessageAsync("", false, builder.Build());
                }
            }

            [Command("set")]
            public async Task SetChannelInfoAsync(int flag)
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                if (Context.Message.Channel is SocketGuildChannel guildChannel)
                {
                    DatabaseManager.Instance().UpdateChannelSetting(guildChannel.Id, flag);
                    Context.Channel.SendMessageAsync($"Set flag {flag} for channel {guildChannel.Name}", false);
                }
            }

            [Command("all")]
            public async Task SetAllChannelInfoAsync(int flag)
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                if (Context.Message.Channel is SocketGuildChannel guildChannel)
                {
                    var channels = DatabaseManager.Instance().GetDiscordAllChannels(guildChannel.Guild.Id);

                    foreach (var item in channels)
                    {
                        DatabaseManager.Instance().UpdateChannelSetting(item.DiscordChannelId, flag, true);
                        Context.Channel.SendMessageAsync($"Set flag {flag} for channel {item.ChannelName}", false);
                    }
                }
            }


            [Command("flags")]
            public async Task GetChannelInfoFlagsAsync()
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"Available flags");

                builder.WithColor(255, 0, 0);

                builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");
                builder.WithCurrentTimestamp();
                string inlineString = "```";
                foreach (BotPermissionType flag in Enum.GetValues(typeof(BotPermissionType)))
                {
                    inlineString += $"{flag} ({(int)(flag)})\r\n";
                }

                inlineString = inlineString.Trim() + "```";
                builder.AddField("BotPermissionType", inlineString);

                Context.Channel.SendMessageAsync("", false, builder.Build());
            }
        }


        [Group("reddit")]
        public class RedditAdminModule : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            public async Task RedditAdminHelp()
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Reddit Admin Help (Admin only)");

                builder.WithColor(0, 0, 255);

                builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");
                builder.WithCurrentTimestamp();
                builder.AddField("admin reddit help", "This message :)");
                builder.AddField("admin reddit status", "Returns if there are currently active scrapers");
                builder.AddField("admin reddit add <name>", "Add Subreddit to SubredditInfos");
                builder.AddField("admin reddit ban <name>", "Manually ban");
                builder.AddField("admin reddit start <name>", "Starts the scraper for a specific subreddit if no scraper is currently running");

                Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("status")]
            public async Task CheckStatusAsync()
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }
                CheckReddit();
            }

            [Command("add")]
            public async Task AddSubredditAsync(string subredditName)
            {
                subredditName = subredditName.ToLower();

                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("Ping the owner and he will add it for you", false);
                    return;
                }

                AddSubreddit(subredditName);
            }

            [Command("ban")]
            public async Task BanSubredditAsync(string subredditName)
            {
                subredditName = subredditName.ToLower();

                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                DatabaseManager.Instance().BanSubreddit(subredditName);
                Context.Channel.SendMessageAsync("Banned " + subredditName, false);

            }

            [Command("start")]
            public async Task StartScraperAsync(string subredditName)
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                var subreddits = DatabaseManager.Instance().GetSubredditsByStatus();
                if (subreddits.Count > 0)
                {
                    Context.Channel.SendMessageAsync($"{subreddits.First().SubredditName} is currently running. Try again later", false);
                    return;
                }
                // TODO check if no scraper is active
                Context.Channel.SendMessageAsync($"Started {subredditName} please wait :)", false);

                if (subredditName.ToLower() == "all")
                {
                    var allSubreddits = DatabaseManager.Instance().GetSubredditsByStatus(false);

                    var allNames = allSubreddits.Select(i => i.SubredditName).ToList();

                    Context.Channel.SendMessageAsync($"Starting", false);

                    for (int i = 0; i < allNames.Count; i += 100)
                    {
                        var items = allNames.Skip(i).Take(100);
                        Context.Channel.SendMessageAsync($"{string.Join(", ", items)}", false);
                        // Do something with 100 or remaining items
                    }

                    Context.Channel.SendMessageAsync($"Please wait :)", false);


                    Task.Factory.StartNew(() => ScrapReddit(allNames));
                }
                else
                {
                    Task.Factory.StartNew(() => ScrapReddit(subredditName));
                }
            }


            private async void CheckReddit()
            {
                var subreddits = DatabaseManager.Instance().GetSubredditsByStatus();

                foreach (var subreddit in subreddits)
                {
                    Context.Channel.SendMessageAsync($"{subreddit.SubredditName} is active", false);
                }

                if (subreddits.Count == 0)
                {
                    Context.Channel.SendMessageAsync($"No subreddits are currently active", false);
                }
            }

            private async void AddSubreddit(string subredditName)
            {
                var reddit = new RedditClient(Program.RedditAppId, Program.RedditRefreshToken, Program.RedditAppSecret);
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    SubredditManager subManager = new SubredditManager(subredditName, reddit, context);
                    Context.Channel.SendMessageAsync($"{subManager.SubredditName} was added to the list", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
                }
            }


            // TODO cleanup this mess

            private async Task ScrapReddit(List<string> subredditName)
            {
                foreach (var item in subredditName)
                {
                    //Context.Channel.SendMessageAsync($"Current {item} is being started", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
                    await ScrapReddit(item);
                    await Task.Delay(500);
                }
                Context.Channel.SendMessageAsync($"Scraper ended :)", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
            }

            private async Task ScrapReddit(string subredditName)
            {
                DatabaseManager.Instance().SetSubredditScaperStatus(subredditName, true);
                var reddit = new RedditClient(Program.RedditAppId, Program.RedditRefreshToken, Program.RedditAppSecret);

                using (var context = new ETHBotDBContext())
                {
                    SubredditManager subManager = new SubredditManager(subredditName, reddit, context);

                    bool ended = subManager.SubredditInfo.ReachedOldest;
                    try
                    {
                        bool beforeWasNotFull = false;

                        string last = "";
                        DateTime lastTime = subManager.SubredditInfo.ReachedOldest ? DateTime.MinValue : DateTime.MaxValue;
                        while (true)
                        {

                            List<Post> posts = null;

                            if (!subManager.SubredditInfo.ReachedOldest)
                                posts = subManager.GetAfterPosts();
                            else
                                posts = subManager.GetBeforePosts();

                            if (!subManager.SubredditInfo.ReachedOldest && posts.Count == 0)
                            {
                                //Context.Channel.SendMessageAsync($"{subManager.SubredditName} scraper reached the end. Setting up end flags", false); // NSFW: {subManager.SubredditInfo.IsNSFW}

                                subManager.ConfirmOldestPost(last, lastTime, true);
                                break;
                            }

                            if (posts.Count == 0)
                            {
                                //Context.Channel.SendMessageAsync($"{subManager.SubredditName} scraper reached the newest post. Setting up end flags", false); // NSFW: {subManager.SubredditInfo.IsNSFW}

                                subManager.ConfirmNewestPost(last, lastTime);
                                break;
                            }
                            string lastPrev = last;
                            foreach (var post in posts)
                            {
                                if (post.Created < lastTime && !subManager.SubredditInfo.ReachedOldest)
                                {
                                    last = post.Fullname;
                                    lastTime = post.Created;
                                }

                                if (post.Created > lastTime && subManager.SubredditInfo.ReachedOldest)
                                {
                                    last = post.Fullname;
                                    lastTime = post.Created;
                                }

                                PostManager manager = new PostManager(post, subManager.SubredditInfo, context);

                                if (manager.IsImage())
                                {
                                    var imageInfos = manager.DownloadImage(Path.Combine(Program.BasePath, "Reddit")); // TODO send path in contructor

                                    context.RedditImages.AddRange(imageInfos);
                                    context.SaveChanges();

                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    //Console.WriteLine($"IGNORED {post.Title} at {last}");
                                    Console.ForegroundColor = ConsoleColor.White;
                                }
                            }

                            if (lastPrev == last)
                            {
                                // TODO set end reached if its not yet set?
                                //Context.Channel.SendMessageAsync($"{subManager.SubredditName} stopped because last/first did not change Count ({posts.Count})", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
                                break;
                            }

                            if (!subManager.SubredditInfo.ReachedOldest)
                            {
                                subManager.ConfirmOldestPost(last, lastTime);
                                //Context.Channel.SendMessageAsync($"{subManager.SubredditName} scraper is happy and well :) Count ({posts.Count}) after {subManager.SubredditInfo.OldestPost}/{subManager.SubredditInfo.OldestPostDate}", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
                            }
                            else
                            {
                                subManager.ConfirmNewestPost(last, lastTime);
                                //subManager.GetBeforePosts();
                                //Context.Channel.SendMessageAsync($"{subManager.SubredditName} scraper is happy and well :) Count ({posts.Count}) before {subManager.SubredditInfo.NewestPost}/{subManager.SubredditInfo.NewestPostDate}", false); // NSFW: {subManager.SubredditInfo.IsNSFW}

                            }


                        }
                    }
                    catch (Exception ex)
                    {
                        Context.Channel.SendMessageAsync($"{subManager.SubredditName} scraper died RIP {ex.Message}", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
                    }

                    //if(!ended)
                    //Context.Channel.SendMessageAsync($"{subManager.SubredditName} scraper ended :D", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
                }

                DatabaseManager.Instance().SetSubredditScaperStatus(subredditName, false);
            }
        }
    }
}