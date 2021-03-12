using ETHBot.DataLayer.Data.Discord;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data
{
    public class BotChannelSetting
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BotChannelSettingId { get; set; }

        public int ChannelPermissionFlags { get; set; }

        [ForeignKey("DiscordChannel")]
        public ulong DiscordChannelId { get; set; }

        public virtual DiscordChannel DiscordChannel { get; set; }

        // we dont save the message id explicitly
        public DateTimeOffset? OldestPostTimePreloaded { get; set; }
        public DateTimeOffset? NewestPostTimePreloaded { get; set; }
        public bool ReachedOldestPreload { get; set; }

    }
}
