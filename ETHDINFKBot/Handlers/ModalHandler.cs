using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Handlers
{
    internal class ModalHandler : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
    {
        //// Responds to the modal.
        //[ModalInteraction("food_menu")]
        //public async Task ModalResponce(FoodModal modal)
        //{
        //    // Build the message to send.
        //    string message = "hey @everyone, I just learned " +
        //        $"{Context.User.Mention}'s favorite food is " +
        //        $"{modal.Food} because {modal.Reason}.";

        //    // Specify the AllowedMentions so we don't actually ping everyone.
        //    AllowedMentions mentions = new();
        //    mentions.AllowedTypes = AllowedMentionTypes.Users;

        //    // Respond to the modal.
        //    await RespondAsync(message, allowedMentions: mentions, ephemeral: true);
        //}
    }
}
