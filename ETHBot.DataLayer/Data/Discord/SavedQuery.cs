using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    // Not doing nxn relation for the reason that the messages could be edited and me being lazy
    public class SavedQuery
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SavedQueryId { get; set; }

        public string CommandName { get; set; }

        public string Description { get; set; }

        public string Content { get; set; } // No file support yet


        [ForeignKey("DiscordUser")]
        public ulong DiscordUserId { get; set; }

        public DiscordUser DiscordUser { get; set; } // obsolete since the message has the author
    }
}
