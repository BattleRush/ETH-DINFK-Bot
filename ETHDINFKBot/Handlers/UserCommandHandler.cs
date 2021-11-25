using Discord;
using Discord.WebSocket;
using ETHDINFKBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Handlers
{
    public class UserCommandHandler
    {
        private SocketUserCommand SocketUserCommand;
        private SocketGuildUser SocketGuildUser;
        private SocketGuildUser DataSocketGuildUser;
        private string CommandName;
        private ulong CommandId;

        private DatabaseManager DatabaseManager;

        public UserCommandHandler(SocketUserCommand socketUserCommand)
        {
            SocketUserCommand = socketUserCommand;
            SocketGuildUser = socketUserCommand.User as SocketGuildUser;
            DataSocketGuildUser = socketUserCommand.Data.Member as SocketGuildUser;
            CommandName = socketUserCommand.CommandName;
            CommandId = socketUserCommand.CommandId;

            DatabaseManager = DatabaseManager.Instance();
        }

        public async Task<bool> Run()
        {
            // TODO Hangle with IDs dynamically
            switch (CommandName)
            {
                case "User's last Pings":
                    return await UserPingInfo();
                default:
                    break;
            }

            return false;
        }

        public async Task<bool> UserPingInfo()
        {
            try
            {
                var pingHistory = DiscordHelper.GetTotalPingHistory(DataSocketGuildUser, 30);
                var builder = DiscordHelper.GetEmbedForPingHistory(pingHistory, DataSocketGuildUser);

                await SocketUserCommand.Channel.SendMessageAsync("", false, builder.Build());
            }
            catch (Exception ex)
            {

            }

            return true;
        }
    }
}
