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
        public string RedditImageId { get; set; }

        public string Link { get; set; }
        public string LokalPath { get; set; }
        public bool Downloaded { get; set; }

        public bool IsNSFW { get; set; }
        public bool IsBlockedManually { get; set; }


        public virtual RedditPost RedditPost { get; set; }
    }
}
