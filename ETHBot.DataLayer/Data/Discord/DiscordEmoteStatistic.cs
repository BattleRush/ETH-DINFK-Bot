using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class DiscordEmoteStatistic
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [ForeignKey("DiscordEmote")]
        public ulong DiscordEmoteId { get; set; }

        public int UsedAsReaction { get; set; }
        public int UsedInText { get; set; }
        public int UsedInTextOnce { get; set; }
        public int UsedByBots { get; set; }

        public DiscordEmote DiscordEmote { get; set; }
    }
}
