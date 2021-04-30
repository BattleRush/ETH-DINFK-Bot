using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Reddit
{
    public class RedditPost
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RedditPostId { get; set; }

        [ForeignKey("SubredditInfo")]
        public int SubredditInfoId { get; set; }
        public virtual SubredditInfo SubredditInfo { get; set; }

        [StringLength(500)]
        public string PostTitle { get; set; }

        [StringLength(100)]
        public string PostId { get; set; }
        public bool IsNSFW { get; set; }

        public DateTime PostedAt { get; set; }

        [StringLength(100)]
        public string Author { get; set; }
        public int UpvoteCount { get; set; }
        public int DownvoteCount { get; set; }

        [StringLength(100)]
        public string Permalink { get; set; }

        [StringLength(500)]
        public string Url { get; set; }
        public bool IsText { get; set; }

        // TODO Limit?
        public string Content { get; set; } 

        //public int CommentCount { get; set; } awards?

        public ICollection<RedditImage> RedditImages { get; set; }
    }
}

