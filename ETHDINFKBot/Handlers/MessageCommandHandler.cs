using Discord;
using Discord.WebSocket;
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
        private SocketUser SocketGuildUser;
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
                if (DatabaseManager.IsSaveMessage(SocketMessage.Id, SocketGuildUser.Id))
                {
                    // dont allow double saves
                    return false;
                }

                string authorUsername = SocketGuildUser.Username; // nickname?

                var link = $"https://discord.com/channels/{SocketTextChannel.Guild.Id}/{SocketTextChannel.Id}/{SocketMessage.Id}";


                var builderComponent = new ComponentBuilder().WithButton("Delete Message", "delete-saved-message-id", ButtonStyle.Danger);


                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"Saved message from {authorUsername}");
                builder.WithColor(0, 128, 255);
                builder.WithDescription(SocketMessage.Content);

                builder.AddField("Guild", SocketTextChannel.Guild.Name, true);
                builder.AddField("Channel", SocketTextChannel.Name, true);
                builder.AddField("User", SocketMessage?.Author?.Username ?? "N/A", true);
                builder.AddField("DirectLink", $"[Link to the message]({link})");

                builder.WithAuthor(SocketGuildUser);
                builder.WithCurrentTimestamp();


                var message = await SocketGuildUser.SendMessageAsync("", false, builder.Build(), null, null, builderComponent.Build(), SocketMessage.Embeds.ToArray());
                foreach (var item in SocketMessage.Attachments)
                {
                    await SocketGuildUser.SendMessageAsync(item.Url, false, null, null, null, builderComponent.Build());
                }

                DatabaseManager.SaveMessage(SocketMessage.Id, SocketMessage?.Author?.Id ?? SocketGuildUser.Id, SocketGuildUser.Id, link, SocketMessage.Content, true, message.Id);

            }
            catch (Exception ex)
            {
                SocketTextChannel.SendMessageAsync(ex.Message);
            }

            return true;


            // var saveMessage = await SocketTextChannel.SendMessageAsync("", false, builder.Build());

            //DiscordHelper.DeleteMessage(saveMessage, TimeSpan.FromSeconds(45));

        }
    }
}
