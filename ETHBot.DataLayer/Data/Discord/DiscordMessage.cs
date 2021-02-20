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
        public ulong MessageId { get; set; }

        public string Content { get; set; }

        [ForeignKey("Channel")]
        public ulong DiscordChannelId { get; set; }
        public DiscordChannel Channel { get; set; }

        [ForeignKey("DiscordUser")]
        public ulong DiscordUserId { get; set; }
        public DiscordUser DiscordUser { get; set; }

        [ForeignKey("ReplyMessage")]
        public ulong? ReplyMessageId { get; set; }
        public DiscordMessage ReplyMessage { get; set; }

        public bool Preloaded { get; set; } // for older messages that were loaded afterwards
    }
}
