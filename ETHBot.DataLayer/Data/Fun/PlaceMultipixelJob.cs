using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Fun
{
    public class PlaceMultipixelJob
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PlaceMultipixelJobId { get; set; }

        // Enum MultipixelJobStatus
        public int Status { get; set; }
        public int TotalPixels { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CanceledAt { get; set; }
        public DateTime? FinishedAt { get; set; }

        [ForeignKey("PlaceDiscordUser")]
        public short PlaceDiscordUserId { get; set; }
        public PlaceDiscordUser PlaceDiscordUser { get; set; }
    }
}
