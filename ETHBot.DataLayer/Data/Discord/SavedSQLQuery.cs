using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    // Not doing nxn relation for the reason that the messages could be edited and me being lazy
    public class SavedSQLQuery
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SavedSQLQueryId { get; set; }

        public ulong? DiscordMessageId { get; set; }

        [ForeignKey("DiscordMessageId")]
        public virtual DiscordMessage DiscordMessage { get; set; }

        public string Keyword { get; set; }

        [StringLength(4000)]
        public string Content { get; set; } // No file support yet

        public ulong DiscordUserId { get; set; }

        [ForeignKey("ByDiscordUserId")]

        public virtual DiscordUser DiscordUser { get; set; } // obsolete since the message has the author
    }
}
