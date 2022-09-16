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
    public class DiscordUserFavouriteRestaturant
    {        
        [Key, Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [ForeignKey("Restaurant")]
        public int RestaurantId { get; set; }
        public Restaurant Restaurant { get; set; }

        [Key, Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [ForeignKey("DiscordUser")]
        public ulong DiscordUserId { get; set; }
        public DiscordUser DiscordUser { get; set; }
    }
}
