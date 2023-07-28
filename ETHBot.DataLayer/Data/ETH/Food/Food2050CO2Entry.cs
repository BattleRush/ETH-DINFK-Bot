using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHBot.DataLayer.Data.ETH.Food
{
    public class Food2050CO2Entry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Food2050CO2EntryId { get; set; }
        public DateTime DateTime { get; set; }

        [ForeignKey("Restaurant")]
        public int RestaurantId { get; set; }
        public Restaurant Restaurant { get; set; }

        public double CO2Delta { get; set; }
        public double CO2Total { get; set; }

        public double TemperatureChange { get; set; }
        public double TemperatureChangeDelta { get; set; }
    }
}
