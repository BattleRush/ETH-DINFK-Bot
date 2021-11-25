using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Handlers
{
    public class ButtonHandler
    {
        private SocketMessageComponent SocketMessageComponent;
        private SocketUserMessage SocketUserMessage;
        private DatabaseManager DatabaseManager;
        public ButtonHandler(SocketMessageComponent socketMessageComponent)
        {
            SocketMessageComponent = socketMessageComponent;
            SocketUserMessage = SocketMessageComponent.Message;

            DatabaseManager = DatabaseManager.Instance();
        }

        public async Task<bool> Run()
        {
            // TODO Hangle with IDs dynamically
            switch (SocketMessageComponent.Data.CustomId)
            {
                case "delete-saved-message-id":
                    return await DeleteSavePostInDM();
                default:
                    break;
            }

            return false;
        }

        private async Task<bool> DeleteSavePostInDM()
        {
            DatabaseManager.DeleteInDmSavedMessage(SocketUserMessage.Id);
            await SocketUserMessage.DeleteAsync();
            return true;
        }
    }
}
