using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Classes
{
    public class ParsedMessageInfo
    {
        public ulong ChannelId { get; set; }
        public string ChannelName { get; set; }
        public Dictionary<DateTimeOffset, int> Info { get; set; }
        public SKColor Color { get; set; }
    }
}
