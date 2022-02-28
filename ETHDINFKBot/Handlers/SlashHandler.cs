using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace ETHDINFKBot.Handlers
{
    public class SlashHandler : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
    {
        [SlashCommand("rant", "Create a new rant")]
        public async Task Rant()
        {
            var mb = new ModalBuilder()
                .WithTitle("New rant")
                .WithCustomId("new-rant-modal")
                .AddTextInput("What is the rant about?", "rant-type", placeholder: "ETH", minLength: 3, maxLength: 40)
                .AddTextInput("Rant message", "rant-message", TextInputStyle.Paragraph, "Your rant message");

            await Context.Interaction.RespondWithModalAsync(mb.Build());
        }
    }
}
