using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHBot.DataLayer.Data.ETH.Food
{
    public class MenuAlergy
    {
        [Key, Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [ForeignKey("Menu")]
        public int MenuId { get; set; }
        public Menu Menu { get; set; }

        [Key, Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [ForeignKey("Alergy")]
        public int AlergyId { get; set; }
        public Alergy Alergy { get; set; }
    }
}
