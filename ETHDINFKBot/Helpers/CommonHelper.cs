using Discord.WebSocket;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data.Enums;
using ETHDINFKBot.Drawing;
using Reddit;
using Reddit.Controllers;
using RedditScrapper;
using SkiaSharp;
using System;
using System.Collections.Generic;

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Helpers
{
    public static class CommonHelper
    {
        public static String HexConverter(Discord.Color c)
        {
            return c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        public static bool ContainsForbiddenQuery(string command)
        {
            List<string> forbidden = new List<string>()
            {
                //"alter",
                //"analyze",
                //"attach",
                //"transaction",
                //"comment",
                //"commit",
                //"create",
                //"delete",
                //"detach",
                //"database",
                //"drop",
                //"insert",
                //"pragma",
                //"reindex",
                //"release",
                //"replace",
                //"rollback",
                //"savepoint",
                //"update",
                //"upsert",
                //"vacuum",
                "`" // to not break any formatting
            };

            foreach (var item in forbidden)
            {
                if (command.ToLower().Contains(item.ToLower()))
                    return true;
            }

            return false;
        }

        // https://stackoverflow.com/a/4423615/3144729
        public static string ToReadableString(TimeSpan span)
        {
            string formatted = string.Format("{0}{1}{2}{3}",
                span.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", span.Days, span.Days == 1 ? string.Empty : "s") : string.Empty,
                span.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", span.Hours, span.Hours == 1 ? string.Empty : "s") : string.Empty,
                span.Duration().Minutes > 0 ? string.Format("{0:0} min{1}, ", span.Minutes, span.Minutes == 1 ? string.Empty : "s") : string.Empty,
                span.Duration().Seconds > 0 ? string.Format("{0:0} sec{1}", span.Seconds, span.Seconds == 1 ? string.Empty : "s") : string.Empty);

            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";

            return formatted;
        }

        public static bool AllowedToRun(BotPermissionType type, ulong channelId, ulong authorId)
        {
            var channelSettings = DatabaseManager.Instance().GetChannelSetting(channelId);
            return authorId == Program.ApplicationSetting.Owner || ((BotPermissionType)channelSettings?.ChannelPermissionFlags).HasFlag(type);
        }

        public static (BotChannelSetting Setting, bool Inherit) GetChannelSettingByChannelId(ulong channelId, bool recursive = true)
        {
            var channelInfo = DatabaseManager.Instance().GetDiscordChannel(channelId);
            var channelSetting = DatabaseManager.Instance().GetChannelSetting(channelId);

            // If no setting found try until we reach a parent with some setting
            if (channelSetting == null && channelInfo?.ParentDiscordChannelId != null && recursive)
                return (GetChannelSettingByChannelId(channelInfo.ParentDiscordChannelId.Value).Setting, true);

            return (channelSetting, false);
        }

        public static (BotChannelSetting Setting, bool Inherit) GetChannelSettingByThreadId(ulong threadId)
        {
            // find out the parent thread id
            var thread = DatabaseManager.Instance().GetDiscordThread(threadId);
            if (thread == null)
                return (null, false);

            return GetChannelSettingByChannelId(thread.DiscordChannelId);
        }

        public static Stream GetStream(SKBitmap bitmap)
        {
            Stream ms = new MemoryStream();
            if (bitmap == null)
            {
                return null;
            }

            try
            {
                IntPtr p;
                IntPtr pixels = bitmap.GetPixels(out p); // this line and the next
                using (var img = SKImage.FromPixels(bitmap.Info, pixels, bitmap.Width * bitmap.BytesPerPixel))
                {
                    var data = img.Encode(SKEncodedImageFormat.Png, 100);
                    data.SaveTo(ms);
                }

                ms.Position = 0;
            }
            catch (Exception ex)
            {
                return null; // TODO log error
            }


            return ms;

            //await Context.Channel.SendFileAsync(ms, "test.png");
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <param name="width">(OPTIONAL) The width to resize too, if negative then ratio of the height will be taken</param>
        /// <returns>The resized image.</returns>
        public static SKBitmap ResizeImage(SKBitmap bitmap, int height, int width = -1)
        {
            SKSizeI size;
            if (width > 0)
            {
                size = new SKSizeI(width, height);
            }
            else
            {
                decimal ratio = bitmap.Width / (decimal)bitmap.Height;
                size = new SKSizeI((int)(height * ratio), height); // Verify if correct to ratio the height
            }

            var resized = bitmap.Resize(size, SKFilterQuality.High);

            return resized;
        }

        // https://stackoverflow.com/a/19553611
        public static string DisplayWithSuffix(int num)
        {
            string number = num.ToString();
            if (number.EndsWith("11")) return number + "th";
            if (number.EndsWith("12")) return number + "th";
            if (number.EndsWith("13")) return number + "th";
            if (number.EndsWith("1")) return number + "st";
            if (number.EndsWith("2")) return number + "and";
            if (number.EndsWith("3")) return number + "rd";
            return number + "th";
        }

        public static async Task ScrapReddit(List<string> subredditNames, ISocketMessageChannel channel)
        {
            foreach (var item in subredditNames)
            {
                //Context.Channel.SendMessageAsync($"Current {item} is being started", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
                await ScrapReddit(item, channel);
                await Task.Delay(500);
            }
            await channel.SendMessageAsync($"Scraper ended :)", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
        }

        public static async Task ScrapReddit(string subredditName, ISocketMessageChannel channel)
        {
            DatabaseManager.Instance().SetSubredditScaperStatus(subredditName, true);
            var reddit = new RedditClient(Program.ApplicationSetting.RedditSetting.AppId, Program.ApplicationSetting.RedditSetting.RefreshToken, Program.ApplicationSetting.RedditSetting.AppSecret);

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

                        if (posts == null)
                            return; // see what to do

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
                            await Task.Delay(25);
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
                                var imageInfos = manager.DownloadImage(Path.Combine(Program.ApplicationSetting.BasePath, "Reddit")); // TODO send path in constructor

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
                    await channel.SendMessageAsync($"{subManager.SubredditName} scraper died RIP {ex.Message}", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
                }

                //if(!ended)
                //Context.Channel.SendMessageAsync($"{subManager.SubredditName} scraper ended :D", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
            }

            DatabaseManager.Instance().SetSubredditScaperStatus(subredditName, false);
        }
    }
}
