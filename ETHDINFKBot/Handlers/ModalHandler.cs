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
    public class CustomEmoteFav : IModal
    {
        public string Title => "";

        [ModalTextInput("custom-emote-name")]
        public string CustomEmoteName { get; set; }

        [ModalTextInput("custom-emote-name-delete")]
        public string DeleteConfirm { get; set; }
    }

    public class ModalHandler : InteractionModuleBase<SocketInteractionContext>
    {
        [ModalInteraction("emote-fav-delete-modal-*")]
        public async Task DeleteEmoteFavModalResponse(string discordEmoteId, CustomEmoteFav modal)
        {
            var emoteId = Convert.ToUInt64(discordEmoteId);
            var discordEmote = DatabaseManager.EmoteDatabaseManager.GetDiscordEmoteById(emoteId);

            Context.Interaction.DeferAsync();

            if (discordEmote == null)
            {
                await Context.Interaction.FollowupAsync("This EmoteId does not exist in the database.", ephemeral: true);
                return;
            }

            if (modal.DeleteConfirm.ToLower() != "delete")
            {
                await Context.Interaction.FollowupAsync("You did not type \"delete\"", ephemeral: true);
                return;
            }

            var success = DatabaseManager.EmoteDatabaseManager.DeleteFavouriteEmote(Context.User.Id, emoteId);

            await Context.Interaction.FollowupAsync("Emote deleted success: " + success, ephemeral: true);
        }


        [ModalInteraction("emote-fav-modal-*")]
        public async Task AddEmoteFavModalResponse(string discordEmoteId, CustomEmoteFav modal)
        {
            var emoteId = Convert.ToUInt64(discordEmoteId);
            string name = modal.CustomEmoteName.Replace("`", ""); // Dont allow people to escape the code blocks
            
            Context.Interaction.DeferAsync();

            if (!name.All(Char.IsLetterOrDigit))
            {
                await Context.Interaction.FollowupAsync("You are only allowed to use alphanimeric characters.", ephemeral: true);
                return;
            }

            var discordEmote = DatabaseManager.EmoteDatabaseManager.GetDiscordEmoteById(emoteId);
            if (discordEmote == null)
            {
                await Context.Interaction.FollowupAsync("This EmoteId does not exist in the database.", ephemeral: true);
                return;
            }

            var existingFavEmotes = DatabaseManager.EmoteDatabaseManager.GetFavouriteEmotes(Context.User.Id);
            if (existingFavEmotes == null) 
                return; // TODO error?

            if (existingFavEmotes.Any(i => i.DiscordEmoteId == emoteId))
            {
                await Context.Interaction.FollowupAsync("Emote is already in your favourites", ephemeral: true);
                return;
            }

            if (existingFavEmotes.Any(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                await Context.Interaction.FollowupAsync("You reserved this name for some other emote already.", ephemeral: true);
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
