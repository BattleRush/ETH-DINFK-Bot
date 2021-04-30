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
        [StringLength(255)]
        public string SubredditName { get; set; }
        [StringLength(255)]
        public string SubredditDescription { get; set; }
        public bool IsManuallyBanned { get; set; }
        public bool IsNSFW { get; set; }

        // Could be read from DB

        [StringLength(100)]
        public string NewestPost { get; set; }
        public DateTime NewestPostDate { get; set; }

        [StringLength(100)]
        public string OldestPost { get; set; }
        public DateTime OldestPostDate { get; set; }
        public bool IsScraping { get; set; }
        public bool ReachedOldest { get; set; }


        public ICollection<RedditPost> RedditPosts { get; set; }
    }
}
