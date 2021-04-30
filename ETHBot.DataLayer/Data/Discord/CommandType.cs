using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;


namespace ETHBot.DataLayer.Data.Discord
{
    // TODO script that check automatically the ids if they are missing
    public class CommandType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CommandTypeId { get; set; }

        [StringLength(255)]
        public string Name { get; set; }
    }
}
