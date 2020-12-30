using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class RantMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RantMessageId { get; set; }


        [ForeignKey("RantType")]
        public int RantTypeId { get; set; }
        public RantType RantType { get; set; }

        public string Content { get; set; }


        [ForeignKey("Channel")]
        public ulong DiscordChannelId { get; set; }
        public DiscordChannel Channel { get; set; }

        [ForeignKey("DiscordUser")]
        public ulong DiscordUserId { get; set; }
        public DiscordUser DiscordUser { get; set; }

        [ForeignKey("DiscordMessage")]
        public ulong DiscordMessageId { get; set; }
        public DiscordMessage DiscordMessage { get; set; }
    }
}
