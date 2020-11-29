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
        [DatabaseGenerated(DatabaseGeneratedOption.None)]

        public ulong MessageId { get; set; }

        public string Content { get; set; }

        public DiscordChannel Channel { get; set; }
        public DiscordUser User { get; set; }
    }
}
