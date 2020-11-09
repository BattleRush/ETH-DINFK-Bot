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
        NekoGif = 2,
        Holo = 3,
        Waifu = 4,
        Baka = 5,
        Smug = 6,
        Fox = 7,
        Avatar = 8,
        NekoAvatar = 9,
        Wallpaper = 10,
        Other = 999
    }

    public class LogManager
    {
        // TODO lock

        public static DateTime LastUpdate = DateTime.MinValue;

        public static void ProcessMessage(SocketUser user, BotMessageType type)
        {
            var guildUser = user as SocketGuildUser;

            if (!Program.GlobalStats.DiscordUsers.Any(i => i.DiscordId == user.Id))
            {
                Program.GlobalStats.DiscordUsers.Add(new Stats.DiscordUser()
                {
                    DiscordId = guildUser.Id,
                    DiscordDiscriminator = guildUser.DiscriminatorValue,
                    DiscordName = guildUser.Username,
                    ServerUserName = guildUser.Nickname ?? guildUser.Username, // User Nickname -> Update
                    Stats = new Stats.UserStats()
                });
            }

            var statUser = Program.GlobalStats.DiscordUsers.Single(i => i.DiscordId == user.Id);

            if(guildUser != null && statUser.ServerUserName != guildUser.Nickname)
            {
                // To update username changes
                statUser.ServerUserName = guildUser.Nickname ?? guildUser.Username;
            }

            // To prevent stats format from breaking
            statUser.ServerUserName = statUser.ServerUserName.Replace("*", "").Replace("~", "");
            statUser.DiscordName = statUser.DiscordName.Replace("*", "").Replace("~", "");

            switch (type)
            {
                case BotMessageType.Neko:
                    statUser.Stats.TotalNeko++;
                    break;
                case BotMessageType.Search:
                    statUser.Stats.TotalSearch++;
                    break;
                case BotMessageType.NekoGif:
                    statUser.Stats.TotalNekoGif++;
                    break;
                case BotMessageType.Holo:
                    statUser.Stats.TotalHolo++;
                    break;
                case BotMessageType.Waifu:
                    statUser.Stats.TotalWaifu++;
                    break;
                case BotMessageType.Baka:
                    statUser.Stats.TotalBaka++;
                    break;
                case BotMessageType.Smug:
                    statUser.Stats.TotalSmug++;
                    break;
                case BotMessageType.Fox:
                    statUser.Stats.TotalFox++;
                    break;
                case BotMessageType.Avatar:
                    statUser.Stats.TotalAvatar++;
                    break;
                case BotMessageType.NekoAvatar:
                    statUser.Stats.TotalNekoAvatar++;
                    break;
                case BotMessageType.Wallpaper:
                    statUser.Stats.TotalWallpaper++;
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
