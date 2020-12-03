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
        public string Link { get; set; }
        public DateTimeOffset ReportTime { get; set; }

        [ForeignKey("ByUser")]
        public ulong ByUserId { get; set; }
        public DiscordUser ByUser { get; set; }
    }
}
