using System;
using System.Collections.Generic;
using System.Text;

namespace ETHDINFKBot.Stats
{
    public class SaveInfo
    {
        public DiscordUser DiscordUser { get; set; }

        List<string> LinksSaved { get; set; }

        Dictionary<long, string> SavedMessage { get; set; }
    }
}
