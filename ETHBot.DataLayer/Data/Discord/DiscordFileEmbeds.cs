using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class DiscordFileEmbeds
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DiscordFileEmbedId { get; set; }

        [ForeignKey("DiscordFile")]
        public int DiscordFileId { get; set; }
        public DiscordFile DiscordFile { get; set; }

        [ForeignKey("PytorchModel")]
        public int PytorchModelId { get; set; }
        public PytorchModel PytorchModel { get; set; }

        public byte[] Embed { get; set; }        
    }
}
