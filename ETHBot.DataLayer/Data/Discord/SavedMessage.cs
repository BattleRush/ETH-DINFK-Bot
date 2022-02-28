using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    // Not doing nxn relation for the reason that the messages could be edited and me being lazy
    public class SavedMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SavedMessageId { get; set; }

        public ulong? DiscordMessageId { get; set; }

        [ForeignKey("DiscordMessageId")]
        public virtual DiscordMessage DiscordMessage { get; set; }

        [StringLength(128)]
        public string DirectLink { get; set; }

        [StringLength(4000)]
        public string Content { get; set; } // No file support yet

        public ulong SavedByDiscordUserId { get; set; }

        [ForeignKey("SavedByDiscordUserId")]
        public virtual DiscordUser SavedByDiscordUser { get; set; }

        public ulong ByDiscordUserId { get; set; }

        [ForeignKey("ByDiscordUserId")]

        public virtual DiscordUser ByDiscordUser { get; set; } // obsolete since the message has the author

        public bool TriggeredByCommand { get; set; }
        public ulong? DMDiscordMessageId { get; set; }
        public bool DeletedFromDM { get; set; }
    }
}
