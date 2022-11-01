using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using ETHDINFKBot.Data;
using ETHDINFKBot.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ETHDINFKBot.Interactions
{
    // Interation modules must be public and inherit from an IInterationModuleBase
    public class ButtonInteractions : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction("delete-saved-message-id")]
        public async Task DeleteSavePostInDM()
        {
            var t = Context.Interaction as SocketMessageComponent;

            DatabaseManager.Instance().DeleteInDmSavedMessage(t.Message.Id);
            await t.Message.DeleteAsync();
            await Context.Interaction.DeferAsync();
        }

        [ComponentInteraction("emote-fav-*")]
        public async Task ButtonPress(string id)
        {
            var emote = DatabaseManager.EmoteDatabaseManager.GetDiscordEmoteById(ulong.Parse(id));

            var mb = new ModalBuilder()
.WithTitle("Favourite Emote form")
.WithCustomId($"emote-fav-modal-{emote.DiscordEmoteId}")
//.AddComponents(new List<IMessageComponent>() { menuBuilder.Build() }, 0)
.AddTextInput($"Name for {emote.EmoteName}", "custom-emote-name", placeholder: emote.EmoteName, required: true);

            await Context.Interaction.RespondWithModalAsync(mb.Build());
            await Context.Interaction.DeferAsync();
        }

        [ComponentInteraction("emote-del-*")]
        public async Task DeleteFavEmote(string id)
        {
            await Context.Interaction.DeferAsync();
            
            var emote = DatabaseManager.EmoteDatabaseManager.GetDiscordEmoteById(ulong.Parse(id));

            var mb = new ModalBuilder()
.WithTitle("Delete favourite emote")
.WithCustomId($"emote-fav-delete-modal-{emote.DiscordEmoteId}")
//.AddComponents(new List<IMessageComponent>() { menuBuilder.Build() }, 0)
.AddTextInput($"Confirm delete by typing \"DELETE\"", "custom-emote-name-delete", placeholder: "DELETE", required: true);

            await Context.Interaction.RespondWithModalAsync(mb.Build());         
        }


        [ComponentInteraction("emote-get-prev-page-*-*-*")]
        public async Task EmoteGetPrevPage(string searchTerm, string page, string debug)
        {
            await Context.Interaction.DeferAsync();
            await EmoteGetPage(-1, searchTerm, Convert.ToInt32(page), Convert.ToBoolean(debug));
        }

        [ComponentInteraction("emote-get-next-page-*-*-*")]
        public async Task EmoteGetNextPage(string searchTerm, string page, string debug)
        {
            await Context.Interaction.DeferAsync();
            await EmoteGetPage(1, searchTerm, Convert.ToInt32(page), Convert.ToBoolean(debug));
        }

        [ComponentInteraction("emote-fav-get-prev-page-*-*")]
        public async Task EmoteFavGetPrevPage(string searchTerm, string page)
        {
            await Context.Interaction.DeferAsync();
            await FavEmoteGetPage(-1, searchTerm, Convert.ToInt32(page));
        }

        [ComponentInteraction("emote-fav-get-next-page-*-*")]
        public async Task EmoteFavGetNextPage(string searchTerm, string page)
        {
            await Context.Interaction.DeferAsync();
            await FavEmoteGetPage(1, searchTerm, Convert.ToInt32(page));
        }

        private async Task<bool> EmoteGetPage(int dir, string searchTerm, int page, bool debug, int rows = 5, int columns = 10)
        {
            var message = Context.Interaction as SocketMessageComponent;
            var user = Context.Interaction.User;
            if (message.Message.Embeds.First().Author.Value.Name != $"{user.Username}#{user.Discriminator}")
            {
                await Context.Interaction.RespondAsync("You did not invoke this message.", null, false, true);
                //Context.Interaction.DeferAsync();
                return false;
            }

            page += dir;

            if (page < 0)
                return false; // disable prev button on page 0

            var emoteResult = DiscordHelper.SearchEmote(searchTerm, Context.Guild.Id, page, debug, rows, columns); // TODO pass debug info aswell
            string desc = $"Available({page * emoteResult.PageSize}-{Math.Min((page + 1) * emoteResult.PageSize, emoteResult.TotalEmotesFound)}/{emoteResult.TotalEmotesFound}) '{searchTerm}' {Environment.NewLine}**To use the emotes (Usage .<name>)**";

            EmbedBuilder builder = new EmbedBuilder()
            {
                //ImageUrl = emoteResult.Url,
                ImageUrl = $"attachment://{emoteResult.Url}",
                Description = desc,
                Color = Color.DarkRed,
                Title = "Image full size",
                Footer = new EmbedFooterBuilder()
                {
                    Text = searchTerm + " Page: " + page
                },
                ThumbnailUrl = Program.Client.CurrentUser.GetAvatarUrl(),
                Timestamp = DateTimeOffset.Now
                //Url = emoteResult.Url,
            };
            builder.WithAuthor(Context.Interaction.User);

            //foreach (var item in emoteResult.Fields)
            //    builder.AddField(item.Key, item.Value);

            // TODO create common place for button ids
            var builderComponent = new ComponentBuilder()
                .WithButton("Prev <", $"emote-get-prev-page-{searchTerm}-{page}-{debug}", ButtonStyle.Danger, null, null, page == 0)
                .WithButton("> Next", $"emote-get-next-page-{searchTerm}-{page}-{debug}", ButtonStyle.Success, null, null, (page + 1) * emoteResult.PageSize > emoteResult.TotalEmotesFound); // TODO detect max page

            FileAttachment attachment = new FileAttachment(Path.Combine(Program.ApplicationSetting.CDNPath, emoteResult.Url));
            Optional<IEnumerable <FileAttachment>> attachments = new List<FileAttachment>() { attachment };

            await message.Message.ModifyAsync(i => {
                i.Attachments = attachments;
                i.Embed = builder.Build(); 
                i.Content = emoteResult.textBlock; 
                i.Components = builderComponent.Build(); 
            });

            //Context.Interaction.DeferAsync();

            return true;
        }

        private async Task<bool> FavEmoteGetPage(int dir, string searchTerm, int page, int rows = 4, int columns = 5, bool secondTry = false)
        {
            var message = Context.Interaction as SocketMessageComponent;
            var user = Context.Interaction.User;
            if (message.Message.Embeds.First().Author.Value.Name != $"{user.Username}#{user.Discriminator}")
            {
                await Context.Interaction.RespondAsync("You did not invoke this message.", null, false, true);
                //Context.Interaction.DeferAsync();
                return false;
            }

            page += dir;

            if (page < 0)
                return false; // disable prev button on page 0

            var emoteResult = DiscordHelper.SearchEmote(searchTerm, Context.Guild.Id, page, false, rows, columns); // TODO pass debug info aswell
            string desc = $"Available({page * emoteResult.PageSize}-{Math.Min((page + 1) * emoteResult.PageSize, emoteResult.TotalEmotesFound)}/{emoteResult.TotalEmotesFound}) '{searchTerm}' {Environment.NewLine}**To use the emotes (Usage .<name>)**";

            EmbedBuilder builder = new EmbedBuilder()
            {
                //ImageUrl = emoteResult.Url,
                ImageUrl = $"attachment://{emoteResult.Url}",
                Description = desc,
                Color = Color.DarkRed,
                Title = "Image full size",
                Footer = new EmbedFooterBuilder()
                {
                    Text = searchTerm + " Page: " + page
                },
                ThumbnailUrl = Program.Client.CurrentUser.GetAvatarUrl(),
                Timestamp = DateTimeOffset.Now
                //Url = emoteResult.Url,
            };
            builder.WithAuthor(Context.Interaction.User);

            var builderComponent = new ComponentBuilder();

            try
            {
                int row = 0;
                int col = 0;
                foreach (var emote in emoteResult.EmoteList)
                {
                    if (emoteResult.valid.Skip(row * columns + col).First())
                    {
                        builderComponent.WithButton(emote.Value, $"emote-fav-{emote.Key}", ButtonStyle.Primary, Emote.Parse($"<:{emote.Value}:{emote.Key}>"), null, false, row);
                    }
                    else
                    {
                        builderComponent.WithButton(emote.Value, $"emote-fav-{emote.Key}", ButtonStyle.Primary, null, null, false, row);
                    }

                    col++;

                    if (col == columns)
                    {
                        row++;
                        col = 0;
                    }
                }

                // Start fresh row for paging
                if (col > 0)
                    row++;

                builderComponent.WithButton("Prev <", $"emote-fav-get-prev-page-{searchTerm}-{page}", ButtonStyle.Danger, null, null, page == 0, row);
                builderComponent.WithButton("> Next", $"emote-fav-get-next-page-{searchTerm}-{page}", ButtonStyle.Success, null, null, (page + 1) * emoteResult.PageSize > emoteResult.TotalEmotesFound, row);

                FileAttachment attachment = new FileAttachment(Path.Combine(Program.ApplicationSetting.CDNPath, emoteResult.Url));
                Optional<IEnumerable<FileAttachment>> attachments = new List<FileAttachment>() { attachment };

                await message.Message.ModifyAsync(i => {
                    i.Attachments = attachments;
                    i.Embed = builder.Build();
                    i.Components = builderComponent.Build();
                });
            }
            catch (HttpException ex)
            {
                foreach (var error in ex.Errors)
                {
                    if (error.Errors.Any(i => i.Code == "BUTTON_COMPONENT_INVALID_EMOJI"))
                    {
                        var parts = error.Path.Split('.');

                        int error_row = Convert.ToInt32(Regex.Replace(parts[0], "[^0-9]", ""));
                        int error_column = Convert.ToInt32(Regex.Replace(parts[1], "[^0-9]", ""));


                        var brokenEmote = emoteResult.EmoteList.Skip(error_row * columns + error_column).First();
                        EmoteDBManager.Instance().ChangeValidStatus(brokenEmote.Key, false);
                    }
                    else
                    {
                        await Context.Interaction.RespondAsync("Error: " + ex.ToString(), null, false, true);
                    }
                }

                // call yourself again to retry -> 
                if (secondTry == false)
                    await FavEmoteGetPage(dir, searchTerm, (page -= dir), rows = 4, columns = 5, true);

                // Some emotes may no lonver be valid -> db entry to invalidate the emote

            }
            catch(Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString());
    }
            //Context.Interaction.DeferAsync();
            return true;
        }
    }
}
