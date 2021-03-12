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

        public string SpaceXSubredditCheckCronJob { get; set; }
        public string LastSpaceXRedditPost { get; set; }
    }
}
