using Reddit.Controllers;
using System;
using System.Collections.Generic;
using System.Text;
using ETHBot.DataLayer.Data.Reddit;
using ETHBot.DataLayer;
using System.Linq;
using System.IO;
using System.Net;

namespace RedditScrapper
{
    public class PostManager
    {
        private Post Post;
        private RedditPost DBPost;
        private SubredditInfo SubredditInfo;
        private ETHBotDBContext ETHBotDBContext;
        private bool PostExisted = false;
        public PostManager(Post post, SubredditInfo subredditInfo, ETHBotDBContext context)
        {
            Post = post;
            SubredditInfo = subredditInfo;
            ETHBotDBContext = context;

            LoadDBRecord();
        }


        public bool IsImage()
        {
            List<string> ApprovedImageDomains = new List<string>()
            {
                "i.redd.it",
                "i.imgur.com",
                "pbs.twimg.com",
                //"reddit.com",
                //"imgur.com"
                //pixiv
                //gyfycat
                
                // Attention multi image
            };
            //Console.ForegroundColor = ConsoleColor.Yellow;
            //Console.WriteLine($"Domain {Post.Listing.Domain}");

            //Console.ForegroundColor = ConsoleColor.White;
            if (Post.Listing.Domain == "reddit.com")
            {
                // https://www.reddit.com/r/hyouka/comments/ho01kk/chichan/
            }

            return ApprovedImageDomains.Contains(Post.Listing.Domain) || Post.Listing.URL.ToLower().EndsWith(".png") || Post.Listing.URL.ToLower().EndsWith(".jpg");
        }

        public List<RedditImage> DownloadImage(string basePath)
        {
            // todo find out if the post has an album


            var returnList = new List<RedditImage>() { };

            

            /*if (Post.Listing.Domain == "reddit.com")
            {
                // https://www.reddit.com/r/hyouka/comments/ho01kk/chichan/
                //"https://www.reddit.com/gallery/jzkxk2"
            }

            if (Post.Listing.Domain == "imgur.com")
            {
                // https://www.reddit.com/r/hyouka/comments/ho01kk/chichan/
            }
            */


            string additionalFolder = $"{Post.Created.Year}-{Post.Created.Month:00}";
            string path = Path.Combine(basePath, SubredditInfo.SubredditName, additionalFolder);
                
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string url = Post.Listing.URL;

            

            string fileEnding = url.Substring(url.LastIndexOf("."));


            // check if image is in db alread TODO
            string file = Path.Combine(path, Post.Fullname+fileEnding);
            
            
            if (/*!File.Exists(file) || */!PostExisted)
            {
                try
                {
                    if (false)
                    {
                        using (WebClient client = new WebClient())
                        {

                            client.DownloadFile(new Uri($"{url}"), file);
                            Console.ForegroundColor = ConsoleColor.Green;

                            //Console.WriteLine($"DOWNLOADED {info.URL}");

                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }

                    // TODO see if the records is created already and needs update

                    returnList.Add(new RedditImage(){
                        Downloaded = true,
                        IsNSFW = Post.NSFW, 
                        Link = url, // TODO if album use each image link
                        LocalPath = null, // TODO not downloading -> set that only some subreddits are downloaded
                        RedditPost = DBPost
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed {url} WITH DOMAIN: {Post.Listing.Domain}");
                }
            }
            else
            {
                //Console.WriteLine($"FILE EXISTS {url}");
            }

            return returnList;
        }

        public void LoadDBRecord()
        {
            var redditPost = ETHBotDBContext.RedditPosts.SingleOrDefault(i => i.PostId == Post.Fullname);
            if(redditPost == null)
            {
                ETHBotDBContext.RedditPosts.Add(new RedditPost()
                {
                    PostId = Post.Fullname,
                    PostTitle = Post.Title,
                    PostedAt = Post.Created,
                    Author = Post.Author,
                    UpvoteCount = Post.UpVotes,
                    DownvoteCount = Post.DownVotes,
                    IsNSFW = Post.NSFW,
                    Permalink = Post.Permalink,
                    SubredditInfo = SubredditInfo,
                    Url = Post.Listing.URL,
                    IsText = Post.Listing.IsSelf,
                    Content = Post.Listing.IsSelf ? Post.Listing.SelfText : null
                });;

                ETHBotDBContext.SaveChanges();
                // TODO Check if the primary key is updated on save changes in the object

                redditPost = ETHBotDBContext.RedditPosts.SingleOrDefault(i => i.PostId == Post.Fullname);
            }
            else
            {
                PostExisted = true;
            }

            DBPost = redditPost;
        }
    }
}
