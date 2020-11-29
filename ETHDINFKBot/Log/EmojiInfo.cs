using ETHDINFKBot.Stats;
using System;
using System.Collections.Generic;
using System.Text;

namespace ETHDINFKBot.Log
{
    public class EmojiInfo
    {
        public string EmojiName { get; set; }
        public ulong EmojiId { get; set; }
        public bool Animated { get; set; }
        public int UsedAsReaction { get; set; }
        public int UsedInText { get; set; }
        public int UsedInTextOnce { get; set; }
        public string Url { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class PingInformation
    {
        public DiscordUser DiscordUser { get; set; }
        public int PingCount { get; set; }
        public int PingCountOnce { get; set; }
    }
}


