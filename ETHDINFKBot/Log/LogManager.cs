using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ETHDINFKBot.Log
{

    public enum BotMessageType
    {
        Neko = 0,
        Search = 1,
        Other = 2
    }

    public class LogManager
    {
        // TODO lock

        public static DateTime LastUpdate = DateTime.MinValue;

        public static void ProcessMessage(SocketUser user, BotMessageType type)
        {
            if (!Program.GlobalStats.DiscordUsers.Any(i => i.DiscordId == user.Id))
            {
                Program.GlobalStats.DiscordUsers.Add(new Stats.DiscordUser()
                {
                    DiscordId = user.Id,
                    DiscordDiscriminator = user.DiscriminatorValue,
                    DiscordName = user.Username,
                    ServerUserName = user.Username, // User Nickname -> Update
                    Stats = new Stats.UserStats()
                });
            }

            var statUser = Program.GlobalStats.DiscordUsers.Single(i => i.DiscordId == user.Id);

            switch (type)
            {
                case BotMessageType.Neko:
                    statUser.Stats.TotalNeko++;
                    break;
                case BotMessageType.Search:
                    statUser.Stats.TotalSearch++;
                    break;
                default:
                    break;
            }

            statUser.Stats.TotalCommands++;

            if (LastUpdate < DateTime.Now.AddSeconds(-30))
            {
                LastUpdate = DateTime.Now;
                Program.SaveStats();
            }
        }
    }
}
