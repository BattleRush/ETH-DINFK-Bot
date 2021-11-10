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
        public bool ChannelOrderLocked { get; set; }

        // ID of the last PixelId that has been saved in a chunk
        public int PlacePixelIdLastChunked { get; set; }
        public short PlaceLastChunkId { get; set; }
    }
}
