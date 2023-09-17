using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHBot.DataLayer.Data.Enums
{
    public enum RestaurantLocation
    {
        [Display(Name = "ETH/UZH Zentrum")]
        ETH_UZH_Zentrum = 1,

        [Display(Name = "ETH Hoengg")]
        ETH_Hoengg = 2,

        [Display(Name = "UZH Irchel/Oerlikon")]
        UZH_Irchel_Oerlikon = 3,

        [Display(Name = "ETH Basel")]
        ETH_Basel = 4,

        [Display(Name = "Zurich (ZHdK, PHZH, etc.)")]
        Zurich = 5,

        [Display(Name = "Luzern (HSLU)")]
        HSLU = 6,

        [Display(Name = "Bern (Uni Bern)")]
        Bern = 7,

        [Display(Name = "Unkwown Location")]
        Other = 99        
    }
}
