using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Reddit
{
    public class RedditImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RedditImageId { get; set; }

        [ForeignKey("RedditPost")]
        public int RedditPostId { get; set; }
        public virtual RedditPost RedditPost { get; set; }

        [StringLength(1000)]
        public string Link { get; set; }

        [StringLength(255)]
        public string LocalPath { get; set; } // TODO local path k -> c
        public bool Downloaded { get; set; }

        public bool IsNSFW { get; set; }
        public bool IsBlockedManually { get; set; }
    }
}
