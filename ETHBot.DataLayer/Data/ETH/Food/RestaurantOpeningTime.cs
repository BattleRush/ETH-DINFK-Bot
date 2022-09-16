using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHBot.DataLayer.Data.ETH.Food
{
    public class RestaurantOpeningTime
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RestaurantOpeningTimeId { get; set; }

        [ForeignKey("Restaurant")]
        public int RestaurantId { get; set; }
        public Restaurant Restaurant { get; set; }

        public TimeOnly From { get; set; }
        public TimeOnly Until { get; set; }
        public int MealType { get; set; }
        public int Weekday { get; set; }

        /*
        public enum MealTime
        {
        0 unknown
            Breakfast = 1,
            Lunch = 2,
            Dinner = 3
        }*/
    }
}
