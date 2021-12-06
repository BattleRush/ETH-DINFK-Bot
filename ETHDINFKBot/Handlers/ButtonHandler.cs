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
                case "emote-get-prev-page":
                    return await EmoteGetPage(-1);
                case "emote-get-next-page":
                    return await EmoteGetPage(1);
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

        private async Task<bool> EmoteGetPage(int dir)
        {
            var embed = SocketUserMessage.Embeds.FirstOrDefault();

            if (embed.Footer.HasValue)
            {
                string footerText = embed.Footer.Value.Text;

                string searchTerm = footerText.Substring(0, footerText.IndexOf("Page:") - 1);
                int page = Convert.ToInt32(footerText.Substring(footerText.IndexOf("Page:") + 6)); // Tryparse?

                page += dir;
                var socketGuildChannel = SocketUserMessage.Channel as SocketGuildChannel; // can only be requested in guild channels anyway

                if (page < 0)
                    return false; // disable prev button on page 0

                var emoteResult = DiscordHelper.SearchEmote(searchTerm, socketGuildChannel.Guild.Id, page, false); // TODO pass debug info aswell
                string desc = $"Available({page * emoteResult.PageSize}-{Math.Min((page + 1) * emoteResult.PageSize, emoteResult.TotalEmotesFound)}/{emoteResult.TotalEmotesFound}) '{searchTerm}' {Environment.NewLine}**To use the emotes (Usage .<name>)**";

                EmbedBuilder builder = new EmbedBuilder()
                {
                    ImageUrl = emoteResult.Url,
                    Description = desc,
                    Color = Color.DarkRed,
                    Title = "Image full size",
                    Footer = new EmbedFooterBuilder()
                    {
                        Text = searchTerm + " Page: " + page
                    },
                    ThumbnailUrl = "https://cdn.battlerush.dev/bot_xmas.png",
                    Timestamp = DateTimeOffset.Now,
                    Url = emoteResult.Url,
                };
                builder.WithAuthor(SocketUserMessage.Author);

                //foreach (var item in emoteResult.Fields)
                //    builder.AddField(item.Key, item.Value);

                // TODO create common place for button ids
                var builderComponent = new ComponentBuilder()
                    .WithButton("Prev <", "emote-get-prev-page", ButtonStyle.Danger, null, null, page == 0)
                    .WithButton("> Next", "emote-get-next-page", ButtonStyle.Success, null, null, false); // TODO detect max page

                await SocketUserMessage.ModifyAsync(i => { i.Embed = builder.Build(); i.Content = emoteResult.textBlock; i.Components = builderComponent.Build(); });
            }

            return true;

        }
    }
}
