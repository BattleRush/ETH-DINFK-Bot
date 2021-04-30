using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class BannedLink
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BannedLinkId { get; set; }

        [StringLength(1000)]
        public string Link { get; set; }
        public DateTimeOffset ReportTime { get; set; }

        [ForeignKey("ByUser")]
        public ulong AddedByDiscordUserId { get; set; }
        public DiscordUser AddedByDiscordUser { get; set; }
    }
}
