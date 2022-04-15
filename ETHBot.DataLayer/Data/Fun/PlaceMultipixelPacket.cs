using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Fun
{
    public class PlaceMultipixelPacket
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PlaceMultipixelPacketId { get; set; }

        [ForeignKey("PlaceMultipixelJob")]
        public int PlaceMultipixelJobId { get; set; }
        public PlaceMultipixelJob PlaceMultipixelJob { get; set; }

        public int InstructionCount { get; set; }

        // Reason to not insert each instruction into the db as it would result in up to 100k inserts and each pixel paint process would need to read, paint and mark instruction as painted
        // Each Instruction
        // X|Y|Color(Hex)
        // Separated by ;
        public string Instructions { get; set; }
        public bool Done { get; set; }
    }
}
