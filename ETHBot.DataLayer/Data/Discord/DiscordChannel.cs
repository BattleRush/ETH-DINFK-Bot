using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class DiscordChannel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]

        public ulong DiscordChannelId { get; set; }

        [StringLength(255)]
        public string ChannelName { get; set; }


        [ForeignKey("DiscordServer")]
        public ulong DiscordServerId { get; set; }
        public DiscordServer DiscordServer { get; set; }

        public bool IsCategory { get; set; }

        public int Position { get; set; }


        [ForeignKey("ParentDiscordChannel")]
        public ulong? ParentDiscordChannelId { get; set; }
        public DiscordChannel ParentDiscordChannel { get; set; }
    }
}
