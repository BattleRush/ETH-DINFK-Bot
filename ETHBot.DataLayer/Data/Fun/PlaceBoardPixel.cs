using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Fun
{
    public class PlaceBoardPixel
    {
        // to encode pos in 3 bytes would save 1 byte * 1M = 1MB which is negligible 

        // 2 bytes
        [Key, Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short XPos { get; set; }

        // 2 bytes
        [Key, Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short YPos { get; set; }

        // 3 bytes
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        // Total 7 Bytes

        // TODO is this needed?
        //public ulong ChangedByDiscordUserId { get; set; }
    }
}
