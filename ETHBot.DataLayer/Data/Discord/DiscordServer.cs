using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class DiscordServer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]

        public ulong DiscordServerId { get; set; }

        [StringLength(2000)]
        public string ServerName { get; set; }
        // TODO Perm
    }
}
