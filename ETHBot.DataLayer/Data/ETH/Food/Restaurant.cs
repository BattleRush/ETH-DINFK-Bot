using ETHBot.DataLayer.Data.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHBot.DataLayer.Data.ETH.Food
{
    public class Restaurant
    {        
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RestaurantId { get; set; }

        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string MenuUrl { get; set; }
        
        // for food2050 this is location name
        public string InternalName { get; set; }
        // for food2050 this is mensa name
        public string AdditionalInternalName { get; set; }

        //public bool OffersBreakfast { get; set; }
        public bool OffersLunch { get; set; }
        public bool OffersDinner { get; set; }

        public bool HasMenu { get; set; }
        public bool IsOpen { get; set; }
        public DateTimeOffset LastUpdate { get; set; }

        public RestaurantLocation Location { get; set; } // TODO Enum

        public bool IsFood2050Supported { get; set; }
        public string? TimeParameter { get; set; }

        // Location 
        // 1 - ETH Zentrum
        // 2 - ETH H�ngg
        // 3 - UZH Zentrum
        // 4 - UZH Irchel
    }
}
