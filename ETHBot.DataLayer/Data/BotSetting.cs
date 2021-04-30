using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data
{
    public class BotSetting
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BotSettingId { get; set; }

        [StringLength(255)]
        public string SpaceXSubredditCheckCronJob { get; set; }

        [StringLength(255)]
        public string LastSpaceXRedditPost { get; set; }
        public bool PlaceLocked { get; set; }
    }
}
