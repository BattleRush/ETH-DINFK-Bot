using ETHBot.DataLayer.Data.Discord;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHBot.DataLayer.Data.Fun
{
    public class FavouriteDiscordEmote
    {
        [Key, Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [ForeignKey("DiscordEmote")]
        public ulong DiscordEmoteId { get; set; }
        public DiscordEmote DiscordEmote { get; set; }

        [Key, Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [ForeignKey("DiscordUser")]
        public ulong DiscordUserId { get; set; }
        public DiscordUser DiscordUser { get; set; }

        public string Name { get; set; }
    }
}
