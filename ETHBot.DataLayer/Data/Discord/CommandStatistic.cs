using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class CommandStatistic
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CommandStatisticId { get; set; }

        [ForeignKey("Type")]
        public int CommandTypeId { get; set; }
        public CommandType Type { get; set; }

        [ForeignKey("DiscordUser")]
        public ulong DiscordUserId { get; set; }
        public DiscordUser DiscordUser { get; set; }
    
        public int Count { get; set; }



        /*
        public int TotalMessages { get; set; }

        public int TotalCommands { get; set; }

        // TODO actually convert it into a dictionary and make it dynamic for new commands
        public int TotalNeko { get; set; }
        public int TotalNekoGif { get; set; }
        public int TotalHolo { get; set; }
        public int TotalWaifu { get; set; }
        public int TotalBaka { get; set; }
        public int TotalSmug { get; set; }
        public int TotalFox { get; set; }
        public int TotalAvatar { get; set; }
        public int TotalNekoAvatar { get; set; }

        // Nekos.Fun & Nekos.life
        public int TotalWallpaper { get; set; }

        // Nekos.Fun
        public int TotalAnimalears { get; set; }
        public int TotalFoxgirl { get; set; }

        public int TotalSearch { get; set; }

        */
    }
}
