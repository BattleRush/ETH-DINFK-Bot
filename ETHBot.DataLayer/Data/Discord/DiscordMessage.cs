using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class DiscordMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public ulong DiscordMessageId { get; set; }

        [StringLength(4000)]
        public string Content { get; set; }

        [ForeignKey("DiscordChannel")]
        public ulong DiscordChannelId { get; set; }
        public DiscordChannel DiscordChannel { get; set; }

        [ForeignKey("DiscordUser")]
        public ulong DiscordUserId { get; set; }
        public DiscordUser DiscordUser { get; set; }

        [ForeignKey("ReplyMessage")]
        public ulong? ReplyMessageId { get; set; }
        public DiscordMessage ReplyMessage { get; set; }

        [ForeignKey("DiscordThread")]
        public ulong? DiscordThreadId { get; set; }
        public DiscordThread DiscordThread { get; set; }

        public bool Preloaded { get; set; } // for older messages that were loaded afterwards
    }
}
