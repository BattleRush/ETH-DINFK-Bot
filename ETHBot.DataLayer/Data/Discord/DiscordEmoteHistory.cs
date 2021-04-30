using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class DiscordEmoteHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int DiscordEmoteHistoryId { get; set; }

        public bool IsReaction { get; set; }
        public int Count { get; set; }
        public DateTime DateTimePosted { get; set; }

        [ForeignKey("DiscordEmote")]
        public ulong DiscordEmoteId { get; set; }
        public DiscordEmote DiscordEmote { get; set; }

        [ForeignKey("DiscordUser")]
        public ulong? DiscordUserId { get; set; }
        public DiscordUser DiscordUser { get; set; }

        [ForeignKey("DiscordMessage")]
        public ulong? DiscordMessageId { get; set; }
        public DiscordMessage DiscordMessage { get; set; }
    }
}
