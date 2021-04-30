using Discord.WebSocket;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data.Discord;
using ETHDINFKBot.Drawing;
using Reddit;
using Reddit.Controllers;
using RedditScrapper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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

        public static Stream GetStream(Bitmap bitmap)
        {
            Stream ms = new MemoryStream();
            if(bitmap != null)
                bitmap.Save(ms, ImageFormat.Png);
            ms.Position = 0;

            return ms;

            //await Context.Channel.SendFileAsync(ms, "test.png");
        }

        // source https://stackoverflow.com/questions/1922040/how-to-resize-an-image-c-sharp

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(System.Drawing.Image image, int height)
        {

            decimal ratio = image.Width / (decimal)image.Height;

            var destRect = new Rectangle(0, 0, (int)(height * ratio), height);
            var destImage = new Bitmap((int)(height * ratio), height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }








        public static async Task ScrapReddit(List<string> subredditNames, ISocketMessageChannel channel)
        {
            foreach (var item in subredditNames)
            {
                //Context.Channel.SendMessageAsync($"Current {item} is being started", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
                await ScrapReddit(item, channel);
                await Task.Delay(500);
            }
            channel.SendMessageAsync($"Scraper ended :)", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
        }

        public static async Task ScrapReddit(string subredditName, ISocketMessageChannel channel)
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
                    channel.SendMessageAsync($"{subManager.SubredditName} scraper died RIP {ex.Message}", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
                }

                //if(!ended)
                //Context.Channel.SendMessageAsync($"{subManager.SubredditName} scraper ended :D", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
            }

            DatabaseManager.Instance().SetSubredditScaperStatus(subredditName, false);
        }

    }
}
