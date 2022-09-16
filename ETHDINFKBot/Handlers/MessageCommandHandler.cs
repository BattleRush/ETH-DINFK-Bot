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
    public class MessageCommandHandler
    {
        private SocketMessageCommand SocketMessageCommand;
        private SocketGuildUser SocketGuildUser;
        private SocketTextChannel SocketTextChannel;
        private SocketMessage SocketMessage;
        private string CommandName;
        private ulong CommandId;

        private DatabaseManager DatabaseManager;

        public MessageCommandHandler(SocketMessageCommand socketMessageCommand)
        {
            SocketMessageCommand = socketMessageCommand;
            SocketGuildUser = socketMessageCommand.User as SocketGuildUser;
            SocketTextChannel = socketMessageCommand.Channel as SocketTextChannel;
            SocketMessage = socketMessageCommand.Data.Message as SocketMessage;
            CommandName = socketMessageCommand.CommandName;
            CommandId = socketMessageCommand.CommandId;

            DatabaseManager = DatabaseManager.Instance();
        }

        public async Task<bool> Run()
        {
            // TODO Hangle with IDs dynamically
            switch (CommandName)
            {
                case "Save Message":
                    return await SaveMessage();
                default:
                    break;
            }

            return false;
        }

        public async Task<bool> SaveMessage()
        {
            try
            {
                await DiscordHelper.SaveMessage(SocketTextChannel, SocketGuildUser, SocketMessage, true);
            }
            catch (Exception ex)
            {
                await SocketTextChannel.SendMessageAsync(ex.Message);
            }

            return true;


            // var saveMessage = await SocketTextChannel.SendMessageAsync("", false, builder.Build());

            //DiscordHelper.DeleteMessage(saveMessage, TimeSpan.FromSeconds(45));

        }
    }
}
