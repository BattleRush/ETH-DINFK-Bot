using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class DiscordRole
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong DiscordRoleId { get; set; } // @everyone is id = 1 and server = null // at here = 2
       
        [StringLength(10)]
        public string ColorHex { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        //public bool IsEveryone { get; }

        public bool IsHoisted { get; set; }
        public bool IsManaged { get; set; }
        public bool IsMentionable { get; set; }

        [StringLength(2000)]
        public string Name { get; set; }
        public int Position { get; set; }

        [ForeignKey("DiscordServer")]
        public ulong? DiscordServerId { get; set; }
        public DiscordServer DiscordServer { get; set; }




        //public GuildPermissions Permissions { get; }
        //public RoleTags Tags { get; }

        // todo add maybe nxn table public IEnumerable<SocketGuildUser> Members { get; }

    }
}
