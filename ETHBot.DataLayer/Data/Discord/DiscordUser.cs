using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ETHBot.DataLayer.Data.Discord
{
    public class DiscordUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong DiscordUserId { get; set; }

        public ushort DiscriminatorValue { get; set;  }

        public bool IsBot { get; set; }

        public bool IsWebhook { get; set; }

        [StringLength(255)]
        public string Username { get; set; }

        [StringLength(255)]
        public string AvatarUrl { get; set; }
        public DateTimeOffset? JoinedAt { get; set; }

        [StringLength(255)]
        public string Nickname { get; set; }
        public int FirstDailyPostCount { get; set; }
        public bool AllowedPlaceMultipixel { get; set; }
        public int FirstAfternoonPostCount { get; set; }
        //public IReadOnlyCollection<ulong> RoleIds { get; } // TODO for db

        //public ICollection<BannedLink> BannedLinks { get; set; }

        //[InverseProperty(nameof(SavedMessage.ByDiscordUser))]
        //public ICollection<SavedMessage> ByDiscordUserSaves { get; set; }

        //[InverseProperty(nameof(SavedMessage.SavedByDiscordUser))]
        //public ICollection<SavedMessage> SavedsByDiscordUser { get; set; }
    }
}
