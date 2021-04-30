using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class PingHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PingHistoryId { get; set; }

        [ForeignKey("DiscordRole")]
        public ulong? DiscordRoleId { get; set; }
        public DiscordRole DiscordRole { get; set; }

        [ForeignKey("DiscordUser")]
        public ulong? DiscordUserId { get; set; }
        public DiscordUser DiscordUser { get; set; }
        public ulong? DiscordMessageId { get; set; }

        [ForeignKey("DiscordMessageId")]
        public virtual DiscordMessage DiscordMessage { get; set; }

        [ForeignKey("FromDiscordUser")]
        public ulong FromDiscordUserId { get; set; }
        public DiscordUser FromDiscordUser { get; set; }
    }
}
