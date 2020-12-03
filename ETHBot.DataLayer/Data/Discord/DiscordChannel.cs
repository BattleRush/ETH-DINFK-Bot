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
        public string ChannelName { get; set; }

        [ForeignKey("DiscordServer")]
        public ulong DiscordServerId { get; set; }
        public DiscordServer DiscordServer { get; set; }
        // TODO Perm
    }
}
