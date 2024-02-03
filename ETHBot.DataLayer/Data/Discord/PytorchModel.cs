using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class PytorchModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PytorchModelId { get; set; }
        public string ModelName { get; set; }
        public bool Main { get; set; }

        public bool Active { get; set; } 

        public bool ForImage { get; set; }

        public bool ForVideo { get; set; }

        public bool ForAudio { get; set; }
    }
}
