using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class OcrBox
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OcrBoxId { get; set; }

        [ForeignKey("DiscordFile")]
        public int DiscordFileId { get; set; }
        public DiscordFile DiscordFile { get; set; }

        public int TopLeftX { get; set; }
        public int TopLeftY { get; set; }

        public int TopRightX { get; set; }
        public int TopRightY { get; set; }

        public int BottomRightX { get; set; }
        public int BottomRightY { get; set; }

        public int BottomLeftX { get; set; }
        public int BottomLeftY { get; set; }

        public string Text { get; set; }

        public double Probability { get; set; }
    }
}
