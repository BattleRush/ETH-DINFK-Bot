using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHBot.DataLayer.Data.ETH.Food
{
    public class Menu
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MenuId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DateTime { get; set; }
        public bool? IsVegetarian { get; set; }
        public bool? IsVegan { get; set; }
        public bool? IsLocal { get; set; }
        public bool? IsBalanced { get; set; }
        public bool? IsGlutenFree { get; set; }
        public bool? IsLactoseFree { get; set; }
        public double Amount { get; set; }
        public int Calories { get; set; }
        public double Protein { get; set; }
        public double Fat { get; set; }
        public double Carbohydrates { get; set; }
        public double Salt { get; set; }
        public string? DirectMenuImageUrl { get; set; }
        public string? FallbackMenuImageUrl { get; set; }

        [ForeignKey("MenuImage")]
        public int? MenuImageId { get; set; }
        public MenuImage MenuImage { get; set; }

        [ForeignKey("Restaurant")]
        public int RestaurantId { get; set; }
        public Restaurant Restaurant { get; set; }
    }
}
