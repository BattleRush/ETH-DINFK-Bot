using Discord.Interactions;
using Discord.WebSocket;
using ETHBot.DataLayer.Data.Fun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Handlers
{
    
    // Defines the modal that will be sent.
    public class FoodModal : IModal
    {
        public string Title => "";

        [ModalTextInput("custom-emote-name")]
        public string CustomEmoteName { get; set; }
    }

    public class ModalHandler : InteractionModuleBase<SocketInteractionContext>
    {
        // Responds to the modal.
        [ModalInteraction("emote-fav-modal-*")]
        public async Task ModalResponce(string discordEmoteId, FoodModal modal)
        {
            var emoteId = Convert.ToUInt64(discordEmoteId);
            string name = modal.CustomEmoteName.Replace("`", ""); // Dont allow people to escape the code blocks
            var discordEmote = DatabaseManager.EmoteDatabaseManager.GetDiscordEmoteById(emoteId);

            if (discordEmote == null)
            {
                // THIS EMOTE ID IS UNKNOWN

                Context.Interaction.RespondAsync("This emote id does not exist in the database.");
                return;
            }

            var existingFavEmotes = DatabaseManager.EmoteDatabaseManager.GetFavouriteEmotes(Context.User.Id);

            if (existingFavEmotes == null) return; // TODO error?

            if (existingFavEmotes.Any(i => i.DiscordEmoteId == emoteId))
            {
                // EMOTE IS ALREADY MAPPED

                Context.Interaction.RespondAsync("Emote is already in your favourites"); // -> UPDATE

                return;
            }

            if (existingFavEmotes.Any(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                // THIS USER MAPPED THE NAME ALREADY FOR AN EMOTE

                Context.Interaction.RespondAsync("You reserved this name for some other emote already."); // -> DELETE FIRST THEN CREATE NEW
                return;
            }


            // Clear to create a new fav mapping

            FavouriteDiscordEmote newFawEmote = new FavouriteDiscordEmote()
            {
                DiscordEmoteId = emoteId,
                DiscordUserId = Context.User.Id,
                Name = name
            };

            var addedFavEmote = DatabaseManager.EmoteDatabaseManager.AddFavouriteEmote(newFawEmote);

            await Context.Interaction.RespondAsync($"``Successfully added {name} as a new favourite emote. You can call the emote with {Program.CurrentPrefix}{name}``");
            await Context.Interaction.DeferAsync();
            // Respond to the modal.
            //await RespondAsync(message, allowedMentions: mentions, ephemeral: true);
        }
    }
}
