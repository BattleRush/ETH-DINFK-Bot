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
            //// TODO Hangle with IDs dynamically
            //switch (SocketMessageComponent.Data.CustomId)
            //{
            //    case "delete-saved-message-id":
            //        return await DeleteSavePostInDM();
            //    case "emote-get-prev-page":
            //        return await EmoteGetPage(-1);
            //    case "emote-get-next-page":
            //        return await EmoteGetPage(1);
            //    case "emote-fav-get-prev-page":
            //        return await FavEmoteGetPage(-1);
            //    case "emote-fav-get-next-page":
            //        return await FavEmoteGetPage(1);
            //    case string s when s.StartsWith("emote-fav-"):
            //        FavouriteEmote(s);
            //        break;
            //    default:
            //        break;
            //}

            return false;
        }

//        private async Task<bool> FavouriteEmote(string s)
//        {
//            string id = s.Substring(s.LastIndexOf('-') + 1);
//            var emote = DatabaseManager.EmoteDatabaseManager.GetDiscordEmoteById(ulong.Parse(id));

//            var menuBuilder = new SelectMenuBuilder()
//    .WithPlaceholder("Select an option")
//    .WithCustomId("menu-1")
//    .WithMinValues(1)
//    .WithMaxValues(1)
//    .AddOption("Option A", "opt-a", "Option B is lying!")
//    .AddOption("Option B", "opt-b", "Option A is telling the truth!");

//            var mb = new ModalBuilder()
//.WithTitle("Favourite Emote form")
//.WithCustomId($"emote-fav-model")
////.AddComponents(new List<IMessageComponent>() { menuBuilder.Build() }, 0)
//.AddTextInput($"Define a name for {emote.EmoteName} emote", emote.DiscordEmoteId.ToString(), placeholder: emote.EmoteName);

//            try
//            {
//                await SocketMessageComponent.RespondWithModalAsync(mb.Build());
//            }
//            catch (Exception ex)
//            {

//            }
//            //await SocketMessageComponent.FollowupAsync($"Selected emote: {emote.EmoteName}");
//            //await SocketMessageComponent.Channel.SendMessageAsync(emote.Url);

//            // TODO Implement trough modal
//            //await SocketMessageComponent.Channel.SendMessageAsync($"Enter preferred name for the emote above: {Environment.NewLine} ``{Program.CurrentPrefix}emote set {id} EMOTE_NAME``");

//            return true;
//        }

//        private async Task<bool> DeleteSavePostInDM()
//        {
//            DatabaseManager.DeleteInDmSavedMessage(SocketUserMessage.Id);
//            await SocketUserMessage.DeleteAsync();
//            return true;
//        }

//        private async Task<bool> FavEmoteGetPage(int dir, int rows = 4, int columns = 5, bool debug = false)
//        {

//            var embed = SocketUserMessage.Embeds.FirstOrDefault();

//            if (embed.Footer.HasValue)
//            {
//                string footerText = embed.Footer.Value.Text;

//                string searchTerm = footerText.Substring(0, footerText.IndexOf("Page:") - 1);
//                int page = Convert.ToInt32(footerText.Substring(footerText.IndexOf("Page:") + 6)); // Tryparse?

//                page += dir;
//                var socketGuildChannel = SocketUserMessage.Channel as SocketGuildChannel; // can only be requested in guild channels anyway

//                if (page < 0)
//                    return false; // disable prev button on page 0

//                var emoteResult = DiscordHelper.SearchEmote(searchTerm, socketGuildChannel.Guild.Id, page, debug, rows, columns); // TODO pass debug info aswell
//                string desc = $"Available({page * emoteResult.PageSize}-{Math.Min((page + 1) * emoteResult.PageSize, emoteResult.TotalEmotesFound)}/{emoteResult.TotalEmotesFound}) '{searchTerm}' {Environment.NewLine}**To use the emotes (Usage .<name>)**";

//                EmbedBuilder builder = new EmbedBuilder()
//                {
//                    ImageUrl = emoteResult.Url,
//                    Description = desc,
//                    Color = Color.DarkRed,
//                    Title = "Image full size",
//                    Footer = new EmbedFooterBuilder()
//                    {
//                        Text = searchTerm + " Page: " + page
//                    },
//                    ThumbnailUrl = "https://cdn.battlerush.dev/bot_xmas.png",
//                    Timestamp = DateTimeOffset.Now,
//                    Url = emoteResult.Url,
//                };
//                builder.WithAuthor(SocketUserMessage.Author);

//                var builderComponent = new ComponentBuilder();

//                try
//                {
//                    int row = 0;
//                    int col = 0;
//                    foreach (var emote in emoteResult.EmoteList)
//                    {
//                        builderComponent.WithButton(emote.Value, $"emote-fav-{emote.Key}", ButtonStyle.Primary, Emote.Parse($"<:{emote.Value}:{emote.Key}>"), null, false, row);
//                        col++;

//                        if (col == columns)
//                        {
//                            row++;
//                            col = 0;
//                        }
//                    }

//                    // Start fresh row for paging
//                    if (col > 0)
//                        row++;

//                    builderComponent.WithButton("Prev <", "emote-fav-get-prev-page", ButtonStyle.Danger, null, null, page == 0, row);
//                    builderComponent.WithButton("> Next", "emote-fav-get-next-page", ButtonStyle.Success, null, null, (page + 1) * emoteResult.PageSize > emoteResult.TotalEmotesFound, row);

//                    await SocketUserMessage.ModifyAsync(i => { i.Embed = builder.Build(); i.Components = builderComponent.Build(); });
//                }
//                catch (Exception ex)
//                {
//                   // TODO go trough emote errors -> invalidate -> let user retry
//                }
//            }

//            return true;
//        }



//        private async Task<bool> EmoteGetPage(int dir, int rows = 5, int columns = 10, bool debug = false)
//        {
//            var embed = SocketUserMessage.Embeds.FirstOrDefault();

//            if (embed.Footer.HasValue)
//            {
//                string footerText = embed.Footer.Value.Text;


//                var footerTextParts = footerText.Split(',');

//                string searchTerm = footerTextParts[0];

//                int page = Convert.ToInt32(footerTextParts[1].Substring(footerTextParts[1].IndexOf(":") + 1).Trim());
//                debug = Convert.ToBoolean(footerTextParts[2].Substring(footerTextParts[2].IndexOf(":") + 1).Trim());

//                page += dir;
//                var socketGuildChannel = SocketUserMessage.Channel as SocketGuildChannel; // can only be requested in guild channels anyway

//                if (page < 0)
//                    return false; // disable prev button on page 0

//                var emoteResult = DiscordHelper.SearchEmote(searchTerm, socketGuildChannel.Guild.Id, page, debug, rows, columns); // TODO pass debug info aswell
//                string desc = $"Available({page * emoteResult.PageSize}-{Math.Min((page + 1) * emoteResult.PageSize, emoteResult.TotalEmotesFound)}/{emoteResult.TotalEmotesFound}) '{searchTerm}' {Environment.NewLine}**To use the emotes (Usage .<name>)**";

//                EmbedBuilder builder = new EmbedBuilder()
//                {
//                    ImageUrl = emoteResult.Url,
//                    Description = desc,
//                    Color = Color.DarkRed,
//                    Title = "Image full size",
//                    Footer = new EmbedFooterBuilder()
//                    {
//                        Text = searchTerm + " Page: " + page
//                    },
//                    ThumbnailUrl = "https://cdn.battlerush.dev/bot_xmas.png",
//                    Timestamp = DateTimeOffset.Now,
//                    Url = emoteResult.Url,
//                };
//                builder.WithAuthor(SocketUserMessage.Author);

//                //foreach (var item in emoteResult.Fields)
//                //    builder.AddField(item.Key, item.Value);

//                // TODO create common place for button ids
//                var builderComponent = new ComponentBuilder()
//                    .WithButton("Prev <", "emote-get-prev-page", ButtonStyle.Danger, null, null, page == 0)
//                    .WithButton("> Next", "emote-get-next-page", ButtonStyle.Success, null, null, (page + 1) * emoteResult.PageSize > emoteResult.TotalEmotesFound); // TODO detect max page

//                await SocketUserMessage.ModifyAsync(i => { i.Embed = builder.Build(); i.Content = emoteResult.textBlock; i.Components = builderComponent.Build(); });
//            }

//            return true;
//        }
    }
}
