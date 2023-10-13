using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Mail;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class DiscordAttachment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public ulong DiscordAttachmentId { get; set; }

        public string ContentType { get; set; }
        public string Description { get; set; }
        public string Filename { get; set; }

        public int? Height { get; set; }
        public int? Width { get; set; }

        [ForeignKey("DiscordMessage")]
        public ulong DiscordMessageId { get; set; }
        public DiscordMessage DiscordMessage { get; set; }

        public int Size { get; set; }

        public string Url { get; set; }
 
        public string Waveform { get; set; }

        public double? Duration { get; set; }

        public bool IsSpoiler { get; set; }
    }
}
