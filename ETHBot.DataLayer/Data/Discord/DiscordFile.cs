using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class DiscordFile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DiscordFileId { get; set; }

        [StringLength(255)]
        public string OriginalFileName { get; set; }

        [ForeignKey("DiscordMessage")]
        public ulong DiscordMessageId { get; set; }
        public DiscordMessage DiscordMessage { get; set; }

        [StringLength(255)]
        public string FileName { get; set; }

        public bool Downloaded { get; set; } 

        public bool IsImage { get; set; }

        public bool IsVideo { get; set; }

        public bool IsAudio { get; set; }

        public bool IsText { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public int? Duration { get; set; }

        public int FileSize { get; set; }

        public string MimeType { get; set; }

        public string Url { get; set; }

        public string OcrText { get; set; }


    }
}
