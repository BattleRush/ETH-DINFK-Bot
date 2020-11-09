using System;
using System.Collections.Generic;
using System.Text;

namespace ETHDINFKBot.Stats
{
    public class DiscordUser
    {
        public ulong DiscordId { get; set; }
        public string DiscordName { get; set; }
        public ushort DiscordDiscriminator { get; set; }
        public string ServerUserName { get; set; } // TODO correct name for number

        public UserStats Stats { get; set; }
    }
}
