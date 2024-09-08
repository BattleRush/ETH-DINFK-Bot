using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class DiscordEmote
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong DiscordEmoteId { get; set; }

        [StringLength(255)]
        public string EmoteName { get; set; }

        public bool Animated { get; set; }

        [StringLength(255)]
        public string Url { get; set; }

        [StringLength(255)]
        public string LocalPath { get; set; }

        public bool Blocked { get; set; }


        // to be added when i figure a good way to do this
        //public string Fingerprint { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset LastUpdatedAt { get; set; }

        public bool IsValid { get; set; }

        [ForeignKey("DiscordServer")]
        public ulong? DiscordServerId { get; set; }
        public DiscordServer DiscordServer { get; set; } 
    }
}
