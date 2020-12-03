using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Reddit
{
    public class SubredditInfo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SubredditId { get; set; }
        public string SubredditName { get; set; }
        public string SubredditDescription { get; set; }
        public bool IsManuallyBanned { get; set; }
        public bool IsNSFW { get; set; }

        // Could be read from DB
        public string NewestPost { get; set; }
        public DateTime NewestPostDate { get; set; }
        public string OldestPost { get; set; }
        public DateTime OldestPostDate { get; set; }
        public bool IsScraping { get; set; }
        public bool ReachedOldest { get; set; }


        public ICollection<RedditPost> RedditPosts { get; set; }
    }
}
