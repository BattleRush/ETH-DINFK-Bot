using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data
{
    public class BotStartUpTime
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BotStartUpTimeId { get; set; }
        public DateTime StartUpTime { get; set; }
    }
}
