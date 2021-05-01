using ETHBot.DataLayer.Data.Discord;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Fun
{
    // total 27 bytes
    // about 4-8 bytes could be saved by optimizing discorduserid and time placed
    public class PlaceBoardHistory
    {
        // over time 3-4 bytes
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PlaceBoardHistoryId { get; set; }

        // 4 bytes
        public short XPos { get; set; }
        public short YPos { get; set; }

        [ForeignKey("XPos, YPos")]
        public virtual PlaceBoardPixel PlaceBoard { get; set; }

        // 3 bytes
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }


        // 2 bytes
        [ForeignKey("PlaceDiscordUser")]
        public short PlaceDiscordUserId { get; set; }
        public PlaceDiscordUser PlaceDiscordUser { get; set; }

        // 8bytes
        public DateTime PlacedDateTime { get; set; }

        // 1 bit -> TODO Default on insert set to 0
        public bool Removed { get; set; }
    }
}
