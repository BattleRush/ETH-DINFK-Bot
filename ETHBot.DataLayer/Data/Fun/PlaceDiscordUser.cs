using ETHBot.DataLayer.Data.Discord;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Fun
{
    // This class is to link DiscordUsers to PlaceBoardHistory -> save around 6 bytes per record
    public class PlaceDiscordUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public short PlaceDiscordUserId { get; set; } // use int16 instead of int8 since that would limit to 255 users at max

        // 8 bytes
        [ForeignKey("DiscordUser")]
        public ulong DiscordUserId { get; set; }
        public DiscordUser DiscordUser { get; set; }

        // Computed column
        public int TotalPixelsPlaced { get; set; }
    }
}
