using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Classes
{
    public class ParsedGraphInfo
    {
        public ulong? ChannelId { get; set; }
        public string ChannelName { get; set; }

        public ulong? DiscordEmoteId { get; set; }
        public string DiscordEmoteName { get; set; }

        public ulong? DiscordUserId { get; set; }
        public string DiscordUsername { get; set; }

        public SKBitmap Image { get; set; }
        public Dictionary<DateTimeOffset, int> Info { get; set; }
        public SKColor Color { get; set; }

        public string GetName()
        {
            // TODO Do trough enum
            if (!string.IsNullOrEmpty(ChannelName))
                return ChannelName;
            if (!string.IsNullOrEmpty(DiscordEmoteName))
                return DiscordEmoteName;
            if (!string.IsNullOrEmpty(DiscordUsername))
                return DiscordUsername;

            return "no label name";
        }
    }
}
