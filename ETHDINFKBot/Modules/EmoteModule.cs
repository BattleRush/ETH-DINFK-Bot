using Discord;
using Discord.Commands;
using Discord.Net;
using ETHBot.DataLayer.Data.Enums;
using ETHBot.DataLayer.Data.Fun;
using ETHDINFKBot.Data;
using ETHDINFKBot.Drawing;
using ETHDINFKBot.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ETHDINFKBot.Modules
{
    // TODO Remove Priority once the main emote command is gone in DiscordModule

    [Group("emote")]
    public class EmoteModule : ModuleBase<SocketCommandContext>
    {
        [Command("help"), Priority(1000)]
        public async Task EmoteHelp()
        {
            if (!CommonHelper.AllowedToRun(BotPermissionType.EnableType2Commands, Context.Message.Channel.Id, Context.Message.Author.Id))
                return;

            var author = Context.Message.Author;

            EmbedBuilder builder = new();

            builder.WithTitle($"{Program.Client.CurrentUser.Username} Help");
            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");

            builder.WithDescription($@"Emote Help");

            builder.WithColor(64, 64, 255);
            builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
            builder.WithCurrentTimestamp();

            builder.AddField($"{Program.CurrentPrefix}emote help", $"This page");
            builder.AddField($"{Program.CurrentPrefix}emote set <emote_id> <name>", $"Set favourite emote with the <emote_id> and <name>. To see the emote_id use {Program.CurrentPrefix}emote fav <search>");
            builder.AddField($"{Program.CurrentPrefix}emote search <name>", $"Search for emotes");
            builder.AddField($"{Program.CurrentPrefix}emote fav {Environment.NewLine}{Program.CurrentPrefix}emote favourite", $"See your own favourite emotes");
            builder.AddField($"{Program.CurrentPrefix}emote fav <name> {Environment.NewLine}{Program.CurrentPrefix}emote favourite <name>", $"Seach for emotes you want to favourite");

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("set"), Priority(1000)]
        public async Task SetEmoteFavourite(ulong emoteId, string name)
        {
            return;
            //name = name.Replace("`", ""); // Dont allow people to escape the code blocks
            //var discordEmote = DatabaseManager.EmoteDatabaseManager.GetDiscordEmoteById(emoteId);

            //if (discordEmote == null)
            //{
            //    // THIS EMOTE ID IS UNKNOWN

            //    Context.Message.ReplyAsync("This emote id does not exist in the database.");
            //    return;
            //}

            //var existingFavEmotes = DatabaseManager.EmoteDatabaseManager.GetFavouriteEmotes(Context.User.Id);

            //if (existingFavEmotes == null) return; // TODO error?

            //if (existingFavEmotes.Any(i => i.DiscordEmoteId == emoteId))
            //{
            //    // EMOTE IS ALREADY MAPPED

            //    Context.Message.ReplyAsync("Emote is already in your favourites"); // -> UPDATE

            //    return;
            //}

            //if (existingFavEmotes.Any(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase)))
            //{
            //    // THIS USER MAPPED THE NAME ALREADY FOR AN EMOTE

            //    Context.Message.ReplyAsync("You reserved this name for some other emote already."); // -> DELETE FIRST THEN CREATE NEW
            //    return;
            //}


            //// Clear to create a new fav mapping

            //FavouriteDiscordEmote newFawEmote = new FavouriteDiscordEmote()
            //{
            //    DiscordEmoteId = emoteId,
            //    DiscordUserId = Context.User.Id,
            //    Name = name
            //};

            //var addedFavEmote = DatabaseManager.EmoteDatabaseManager.AddFavouriteEmote(newFawEmote);

            //await Context.Message.ReplyAsync($"``Successfully added {name} as a new favourite emote. You can call the emote with {Program.CurrentPrefix}{name}``");
        }

        [Command("favourite"), Priority(1000)]
        [Alias("fav")]
        public async Task ViewFavouriteEmotes()
        {
            var userFavEmotes = DatabaseManager.EmoteDatabaseManager.GetFavouriteEmotes(Context.User.Id);
            var emotes = DatabaseManager.EmoteDatabaseManager.GetDiscordEmotes(userFavEmotes.Select(i => i.DiscordEmoteId).ToList());

            string fileName = $"emote_fav_{new Random().Next(int.MaxValue)}.png";

            // Show the user specified emote name
            foreach (var emote in emotes)
                emote.EmoteName = userFavEmotes.SingleOrDefault(i => i.DiscordEmoteId == emote.DiscordEmoteId)?.Name ?? "N/A";

            // TODO List guild emotes

            var emoteDrawing = DiscordHelper.DrawPreviewImage(emotes, new List<GuildEmote>(), 10, 10);

            DrawingHelper.SaveToDisk(Path.Combine(Program.ApplicationSetting.CDNPath, fileName), emoteDrawing.Bitmap);

            EmbedBuilder builder = new EmbedBuilder()
            {
                ImageUrl = $"https://cdn.battlerush.dev/{fileName}",
                Description = "Your Favourited emotes",
                Color = Color.DarkRed,
                Title = "Image full size",
                Footer = new EmbedFooterBuilder()
                {
                    Text = "" + " Page: " + -1
                },
                ThumbnailUrl = "https://cdn.battlerush.dev/bot_xmas.png",
                Timestamp = DateTimeOffset.Now,
                Url = $"https://cdn.battlerush.dev/{fileName}",
            };
            builder.WithAuthor(Context.User);

            var msg2 = await Context.Channel.SendMessageAsync("", false, builder.Build(), null, null, null, null);
        }

        [Command("favourite"), Priority(1000)]
        [Alias("fav")]
        public async Task EmoteFavourite(string search, bool secondTry = false)
        {
            int rows = 4;
            int columns = 5;

            if (!CommonHelper.AllowedToRun(BotPermissionType.EnableType2Commands, Context.Message.Channel.Id, Context.Message.Author.Id))
                return;

            Stopwatch watch = new Stopwatch();
            watch.Start();

            var author = Context.Message.Author;

            if (search.Length < 2 && author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync($"Search term needs to be atleast 2 characters long", false); // to prevent from db overload
                return;
            }

            var emoteResult = DiscordHelper.SearchEmote(search, Context.Guild.Id, 0, false, rows, columns);


            watch.Stop();

            int page = 0;

            string desc = $"**Available({page * emoteResult.PageSize}-{Math.Min((page + 1) * emoteResult.PageSize, emoteResult.TotalEmotesFound)}/{emoteResult.TotalEmotesFound}) '{search}' emojis to use (Usage .<name>)**" + Environment.NewLine;

            try
            {
                EmbedBuilder builder = new EmbedBuilder()
                {
                    ImageUrl = emoteResult.Url,
                    Description = desc,
                    Color = Color.DarkRed,
                    Title = "Image full size",
                    Footer = new EmbedFooterBuilder()
                    {
                        Text = search + " Page: " + page
                    },
                    ThumbnailUrl = "https://cdn.battlerush.dev/bot_xmas.png",
                    Timestamp = DateTimeOffset.Now,
                    Url = emoteResult.Url,
                };
                builder.WithAuthor(Context.User);

                //foreach (var item in emoteResult.Fields)
                //    builder.AddField(item.Key, item.Value);


                // TODO create common place for button ids
                var builderComponent = new ComponentBuilder();
                //.WithButton("Prev <", "emote-get-prev-page", ButtonStyle.Danger, null, null, true)
                //.WithButton("> Next", "emote-get-next-page", ButtonStyle.Success, null, null, true); // TODO properly calc max page
                //.WithButton("Row 1", "emote-get-row-1", ButtonStyle.Secondary, null, null, false, 1)
                //.WithButton("Row 2", "emote-get-row-2", ButtonStyle.Secondary, null, null, false, 1)
                //.WithButton("Row 3", "emote-get-row-3", ButtonStyle.Secondary, null, null, false, 1)
                //.WithButton("Row 4", "emote-get-row-4", ButtonStyle.Secondary, null, null, false, 1)
                //.WithButton("Row 5", "emote-get-row-5", ButtonStyle.Secondary, null, null, false, 1);

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

                builderComponent.WithButton("Prev <", $"emote-fav-get-prev-page-{search}-{page}", ButtonStyle.Danger, null, null, page == 0, row);
                builderComponent.WithButton("> Next", $"emote-fav-get-next-page-{search}-{page}", ButtonStyle.Success, null, null, (page + 1) * emoteResult.PageSize > emoteResult.TotalEmotesFound, row);

                var msg2 = await Context.Channel.SendMessageAsync("", false, builder.Build(), null, null, null, builderComponent.Build());
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
                }

                // call yourself again to retry -> 
                if (secondTry == false)
                    await EmoteFavourite(search, true);

                // Some emotes may no lonver be valid -> db entry to invalidate the emote

            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString(), false);
            }
        }


        [Command("search"), Priority(1000)]
        public async Task EmoteSearch(string search, bool debug = false)
        {
            if (!CommonHelper.AllowedToRun(BotPermissionType.EnableType2Commands, Context.Message.Channel.Id, Context.Message.Author.Id))
                return;

            Stopwatch watch = new Stopwatch();
            watch.Start();

            var author = Context.Message.Author;

            if (search.Length < 2 && author.Id != Program.ApplicationSetting.Owner)
            {
                await Context.Channel.SendMessageAsync($"Search term needs to be atleast 2 characters long", false); // to prevent from db overload
                return;
            }


            var emoteResult = DiscordHelper.SearchEmote(search, Context.Guild.Id, 0, debug);

            watch.Stop();

            int page = 0;

            string desc = $"**Available({page * emoteResult.PageSize}-{Math.Min((page + 1) * emoteResult.PageSize, emoteResult.TotalEmotesFound)}/{emoteResult.TotalEmotesFound}) '{search}' emojis to use (Usage .<name>)**" + Environment.NewLine;


            EmbedBuilder builder = new EmbedBuilder()
            {
                ImageUrl = emoteResult.Url,
                Description = desc,
                Color = Color.DarkRed,
                Title = "Image full size",
                Footer = new EmbedFooterBuilder()
                {
                    Text = $"{search}, Page: {page}, Debug: {debug}"
                },
                ThumbnailUrl = "https://cdn.battlerush.dev/bot_xmas.png",
                Timestamp = DateTimeOffset.Now,
                Url = emoteResult.Url,
            };
            builder.WithAuthor(Context.User);

            //foreach (var item in emoteResult.Fields)
            //    builder.AddField(item.Key, item.Value);

            try
            {
                // TODO create common place for button ids
                var builderComponent = new ComponentBuilder()
                    .WithButton("Prev <", "emote-get-prev-page", ButtonStyle.Danger, null, null, page == 0)
                    .WithButton("> Next", "emote-get-next-page", ButtonStyle.Success, null, null, (page + 1) * emoteResult.PageSize > emoteResult.TotalEmotesFound); // TODO properly calc max page
                                                                                                                                                                     //.WithButton("Row 1", "emote-get-row-1", ButtonStyle.Secondary, null, null, false, 1)
                                                                                                                                                                     //.WithButton("Row 2", "emote-get-row-2", ButtonStyle.Secondary, null, null, false, 1)
                                                                                                                                                                     //.WithButton("Row 3", "emote-get-row-3", ButtonStyle.Secondary, null, null, false, 1)
                                                                                                                                                                     //.WithButton("Row 4", "emote-get-row-4", ButtonStyle.Secondary, null, null, false, 1)
                                                                                                                                                                     //.WithButton("Row 5", "emote-get-row-5", ButtonStyle.Secondary, null, null, false, 1);

                var msg2 = await Context.Channel.SendMessageAsync(emoteResult.textBlock, false, builder.Build(), null, null, null, builderComponent.Build());
            }
            catch (Exception ex)
            {

            }
        }
    }
}
