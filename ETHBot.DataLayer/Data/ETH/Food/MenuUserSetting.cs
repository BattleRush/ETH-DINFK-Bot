using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETHBot.DataLayer.Data.Discord;

namespace ETHBot.DataLayer.Data.ETH.Food
{
    public class MenuUserSetting
    {        
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [ForeignKey("DiscordUser")]
        public ulong DiscordUserId { get; set; }
        public DiscordUser DiscordUser { get; set; }

        public bool VegetarianPreference { get; set; }
        public bool VeganPreference { get; set; }

        public bool FullNutritions { get; set; }
        public bool DisplayAllergies { get; set; }
    }
}
