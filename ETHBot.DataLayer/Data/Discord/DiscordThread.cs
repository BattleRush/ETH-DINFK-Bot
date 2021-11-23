using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHBot.DataLayer.Data.Discord
{
    public class DiscordThread
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]

        public ulong DiscordThreadId { get; set; }

        [StringLength(255)]
        public string ThreadName { get; set; }

        public bool IsArchived { get; set; }
        public bool IsLocked { get; set; }
        public bool IsNsfw { get; set; }
        public bool IsPrivateThread { get; set; }
        public int ThreadType { get; set; }

        public int MemberCount { get; set; }

        [ForeignKey("DiscordChannel")]
        public ulong DiscordChannelId { get; set; }
        public DiscordChannel DiscordChannel { get; set; }

        // TODO Perm
    }
}
