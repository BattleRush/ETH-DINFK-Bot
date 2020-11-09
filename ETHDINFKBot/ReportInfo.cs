using ETHDINFKBot.Stats;
using System;
using System.Collections.Generic;
using System.Text;

namespace ETHDINFKBot
{
    public class ReportInfo
    {
        public string ImageUrl { get; set; }
        public DateTime ReportedAt { get; set; }
        public DiscordUser ReportedBy { get; set; }
    }
}
