using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using DuckSharp;
using ETHBot.DataLayer;
using ETHDINFKBot.Log;
using NekosSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ETHBot.DataLayer.Data.Enums;
using ETHBot.DataLayer.Data.Discord;
using Reddit;
using RedditScrapper;
using Reddit.Controllers;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;

namespace ETHDINFKBot
{
    public class DiscordModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger _logger = new Logger<DiscordModule>(Program.Logger);

        public static NekoClient NekoClient = new NekoClient("BattleRush's Helper");
        public static NekosFun NekosFun = new NekosFun();

        static List<RestUserMessage> LastMessages = new List<RestUserMessage>();

        DatabaseManager DatabaseManager = DatabaseManager.Instance();

        LogManager LogManager = new LogManager(DatabaseManager.Instance()); // not needed to pass a singleton actually


        // TODO Remove alot of the redundant code for loggining and stats


        private bool AllowedToRun(BotPermissionType type)
        {
            // since this is always calles works for now as workaround
            NekoClient.LogType = LogType.None;

            var channelSettings = DatabaseManager.GetChannelSetting(Context.Message.Channel.Id);
            if (Context.Message.Author.Id != Program.Owner
                && !((BotPermissionType)channelSettings?.ChannelPermissionFlags).HasFlag(type))
            {
#if DEBUG
                Context.Channel.SendMessageAsync("blocked by perms", false);
#endif
                return true;
            }

            return false;
        }



        [Command("code")]
        [Alias("source")]
        public async Task SourceCode()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Other);

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Sourcecode for BattleRush's Helper (thats me)");
            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");
            builder.WithDescription(@"TODO Create some meaningfull text here to go with such an awesome bot.
**Source code: **
**https://github.com/BattleRush/ETH-DINFK-Bot**");
            builder.WithColor(0, 255, 0);

            //builder.WithThumbnailUrl("https://avatars0.githubusercontent.com/u/11750584");

            var ownerUser = Program.Client.GetUser(Program.Owner);
            builder.WithThumbnailUrl(ownerUser.GetAvatarUrl());
            builder.WithAuthor(ownerUser);
            builder.WithCurrentTimestamp();

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("help")]
        [Alias("about")]
        public async Task HelpOutput()
        {
            // _logger.LogError("GET HelpOutput called.");


            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;


            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Other);

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("BattleRush's Helper Help");
            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");
            builder.WithDescription(@"Prefix for all comands is "".""
Help is in EBNF form, so I hope for you all reading this actually paid attention to Thomas how to use it");
            builder.WithColor(0, 0, 255);

            builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");
            //builder.WithFooter($"If you can read this then ping Mert | TroNiiXx | [13]");
            builder.WithCurrentTimestamp();
            //builder.WithAuthor(author);
            builder.AddField("Misc", "```.help .source .stats .lb```");
            builder.AddField("Search", "```.google|duck <search term>```");
            builder.AddField("Images", "```.neko[avatar] .fox .waifu .baka .smug .holo .avatar .wallpaper```");
            builder.AddField("Reddit", "```.r[p] <subreddit>|all```");
            builder.AddField("Rant", "```.rant [ types | (<type> <message>) ]```");
            builder.AddField("SQL", "```.sql (table info) | (query <query>)```");
            builder.AddField("Emote (can send Nitro emotes for you)", "```.emote <search_string> | .<emote_name>```");

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("duck")]
        [Summary("Returns info about the current user, or the user parameter, if one passed.")]
        public async Task DuckDuckGo([Remainder] string searchString)
        {

            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            var userInfo = Context.Message.Author;
            //await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");

            LogManager.ProcessMessage(userInfo, BotMessageType.Search);

            var reply = await new DuckSharpClient().GetInstantAnswerAsync(searchString);

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Search for " + searchString);
            builder.WithDescription(reply.AbstractText + Environment.NewLine + reply.AbstractUrl);
            builder.WithColor(128, 128, 0);

            if (reply?.RelatedTopics != null && reply.RelatedTopics.Length > 0)
            {
                builder.Description = reply.RelatedTopics[0].Text;
            }

            if (reply.ImageUrl != null && reply.ImageUrl.Length > 0)
            {
                builder.WithThumbnailUrl($"https://duckduckgo.com{reply.ImageUrl}");
            }

            builder.WithAuthor(userInfo);
            builder.WithFooter($"{userInfo.Username}#{userInfo.Discriminator}");
            builder.WithCurrentTimestamp();

            // TODO Error handling
            foreach (var item in reply.RelatedTopics.Select(i => i).Take(4))
            {
                builder.AddField(item.Text, item.FirstUrl);
            }

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("google")]
        [Summary("Returns info about the current user, or the user parameter, if one passed.")]
        public async Task GoogleSearch([Remainder] string searchString)
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            var userInfo = Context.Message.Author;
            //await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");

            LogManager.ProcessMessage(userInfo, BotMessageType.Search);

            var reply = new Engine().Search(searchString);

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Search for " + searchString);
            builder.WithDescription(reply.Description?.Substring(0, reply.Description.Length > 100 ? 100 : reply.Description.Length));
            builder.WithColor(128, 0, 128);

            //builder.Description = reply.RelatedTopics[0].Text;
            if (reply.ImageUrl != null)
            {
                //builder.WithThumbnailUrl(reply.ImageUrl);
            }

            builder.WithAuthor(userInfo);
            builder.WithFooter($"{userInfo.Username}#{userInfo.Discriminator}");
            builder.WithCurrentTimestamp();

            foreach (var item in reply.Results.Take(3))
            {
                builder.AddField(item.title, item.description + Environment.NewLine + item.url);
            }

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        // TODO Rework
        [Command("neko")]
        public async Task Neko()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Neko);

            var req = await NekoClient.Image_v3.Neko();
            var report = GetReportInfoByImage(req.ImageUrl);
            if (report != null)
            {
                var user = DatabaseManager.GetDiscordUserById(report.ByUserId);
                Context.Channel.SendMessageAsync($"The current image has been blocked by {user.Nickname}. Try the command again to get a new image", false);
                return;
            }

            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        [Command("nekogif")]
        public async Task NekoGif()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.NekoGif);

            var req = await NekoClient.Image_v3.NekoGif();
            var report = GetReportInfoByImage(req.ImageUrl);
            if (report != null)
            {
                var user = DatabaseManager.GetDiscordUserById(report.ByUserId);
                Context.Channel.SendMessageAsync($"The current image has been blocked by {user.Nickname}. Try the command again to get a new image", false);
                return;
            }

            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }


        [Command("fox")]
        public async Task Fox()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Fox);

            var req = await NekoClient.Image_v3.Fox();
            var report = GetReportInfoByImage(req.ImageUrl);
            if (report != null)
            {
                var user = DatabaseManager.GetDiscordUserById(report.ByUserId);
                Context.Channel.SendMessageAsync($"The current image has been blocked by {user.Nickname}. Try the command again to get a new image", false);
                return;
            }

            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        [Command("waifu")]
        public async Task Waifu()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Waifu);

            var req = await NekoClient.Image_v3.Waifu();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        [Command("baka")]
        public async Task Baka()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Baka);

            var req = await NekoClient.Image.Baka();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        [Command("smug")]
        public async Task Smug()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Smug);

            var req = await NekoClient.Image.Smug();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        [Command("holo")]
        public async Task Holo()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            try
            {
                var author = Context.Message.Author;
                LogManager.ProcessMessage(author, BotMessageType.Holo);

                var req = await NekoClient.Image_v3.Holo();

                var report = GetReportInfoByImage(req.ImageUrl);
                if (report != null)
                {
                    var user = DatabaseManager.GetDiscordUserById(report.ByUserId);
                    Context.Channel.SendMessageAsync($"The current image has been blocked by {user.Nickname}. Try the command again to get a new image", false);
                    return;
                }

                Context.Channel.SendMessageAsync(req.ImageUrl, false);
            }
            catch (Exception ex)
            {

            }
        }

        [Command("avatar")]
        public async Task Avatar()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Avatar);

            var req = await NekoClient.Image_v3.Avatar();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        [Command("nekoavatar")]
        public async Task NekoAvatar()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.NekoAvatar);

            var req = await NekoClient.Image_v3.NekoAvatar();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        /*[Command("wallpaper")] // TODO INTEGRATE 2 wallpaper endpoints
        public async Task Wallpaper()
        {
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Wallpaper);

            var req = await NekoClient.Image_v3.Wallpaper();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }*/

        [Command("emote")]
        public async Task EmojiInfo(string search)
        {
            
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            var author = Context.Message.Author;

            var animatedEmotes = DatabaseManager.GetEmotesByName(search);

            // limit to 100
            animatedEmotes = animatedEmotes.Take(100).ToList();

            // TODO make it look nice
            string text = "**Available emojis to use (Usage .<name>)**" + Environment.NewLine + Environment.NewLine;

            foreach (var emoji in animatedEmotes)
            {
                text += $".{emoji.EmojiName} ";

                if(text.Length > 1800)
                {
                    await Context.Channel.SendMessageAsync(text, false);
                    text = "";
                }
            }

            await Context.Channel.SendMessageAsync(text, false);

        }

        [Command("wallpaper", RunMode = RunMode.Async)]
        [Alias("wp")]
        public async Task Wallpaper()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Wallpaper);

            var req = NekosFun.GetLink("wallpaper");
            BannedLink report = null;

            string regenString = "";

            do
            {
                try
                {
                    report = GetReportInfoByImage(req);
                    if (report != null)
                    {

                        var user = DatabaseManager.GetDiscordUserById(report.ByUserId);
                        regenString += $"An image has been blocked by {user.Nickname}. Regenerating a new image just for you :)" + Environment.NewLine;
                        req = NekosFun.GetLink("wallpaper");

                        //return;
                    }
                }
                catch (Exception ex)
                {
                    return;
                }
            } while (report != null);

            if (regenString.Length > 0)
            {
                await Context.Channel.SendMessageAsync(regenString, false);
            }

            var message = await Context.Channel.SendMessageAsync(req, false);

            // disabled for now
            if (false)
                await AddSaveReact(message);

            AddMessageToList(message);

            if (new Random().Next(0, 20) == 0)
            {
                // Send only every x messages
                Context.Channel.SendMessageAsync("wallpaper may still contain some NSFW images. To remove them type '.block link' To get the link, right click the image -> Copy Link. Do not use < > around the link", false);
            }
        }

        [Command("animalears", RunMode = RunMode.Async)]
        public async Task Animalears()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Animalears);

            var req = NekosFun.GetLink("animalears");
            var report = GetReportInfoByImage(req);
            if (report != null)
            {
                var user = DatabaseManager.GetDiscordUserById(report.ByUserId);
                Context.Channel.SendMessageAsync($"The current image has been blocked by {user.Nickname}. Try the command again to get a new image", false);
                return;
            }

            var message = await Context.Channel.SendMessageAsync(req, false);
            await AddSaveReact(message);
            AddMessageToList(message);
        }


        [Command("foxgirl", RunMode = RunMode.Async)]
        public async Task Foxgirl()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Foxgirl);

            var req = NekosFun.GetLink("foxgirl");
            var report = GetReportInfoByImage(req);
            if (report != null)
            {
                var user = DatabaseManager.GetDiscordUserById(report.ByUserId);
                Context.Channel.SendMessageAsync($"The current image has been blocked by {user.Nickname}. Try the command again to get a new image", false);
                return;
            }

            var message = await Context.Channel.SendMessageAsync(req, false);
            await AddSaveReact(message);
            AddMessageToList(message);
        }


        public async void AddMessageToList(RestUserMessage message)
        {
            if (!message.Author.IsBot)
            {
                // for now only log messages from bots
                return;
            }

            LastMessages.Add(message);
            if (LastMessages.Count() > 100)
                LastMessages.RemoveAt(0);
        }


        [Command("block")]
        public async Task Block(string image)
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                //Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                //return
            }
            var guildUser = author as SocketGuildUser;

            // Remove < > for no preview if used
            image = image.Replace("<", "").Replace("<", "");



            if (image.Contains("discordapp") || !image.StartsWith("https://"))
            {
                Context.Channel.SendMessageAsync($"You did not provide a valid link.", false);
                return;
            }

            var blockInfo = DatabaseManager.GetBannedLink(image);

            if (blockInfo != null)
            {
                Context.Message.DeleteAsync();
                var user = DatabaseManager.GetDiscordUserById(blockInfo.ByUserId);
                Context.Channel.SendMessageAsync($"Image is already in the blacklist (blocked by {user.Nickname}) You were too slow {guildUser.Nickname} <:exmatrikulator:769624058005553152>", false);
                return;
            }

            /* ReportInfo reportInfo = new ReportInfo()
             {
                 ImageUrl = image,
                 ReportedAt = DateTime.Now,
                 ReportedBy = new Stats.DiscordUser()
                 {

                     DiscordId = guildUser.Id,
                     DiscordDiscriminator = guildUser.DiscriminatorValue,
                     DiscordName = guildUser.Username,
                     ServerUserName = guildUser.Nickname ?? guildUser.Username // User Nickname -> Update
                 }

             };
            */
            DatabaseManager.CreateBannedLink(image, guildUser.Id);

            /*
            Program.BlackList.Add(reportInfo);
            Program.SaveBlacklist();*/

            foreach (var message in LastMessages)
            {
                if (message.Content == image)
                {
                    // We are removing this item
                    message.DeleteAsync();
                }
            }

            Context.Channel.SendMessageAsync($"Added the image to blacklist by {guildUser.Nickname}", false);
            Context.Message.DeleteAsync();
        }

        private async Task AddSaveReact(RestUserMessage message)
        {
            await message.AddReactionAsync(Emote.Parse("<:savethis:780179874656419880>"));
        }


        [Command("rant")]
        public async Task Rant(string type = null, [Remainder] string content = "")
        {
            // TODO perm check but for now open everwhere

            if (type == null)
            {
                // get a random rant
                RandomRant();

            }
            else if (type.ToLower() == "help")
            {
                HelpOutput();
                return;
            }
            else if (type.ToLower() == "types")
            {
                var typeList = DatabaseManager.GetAllRantTypes();
                string allTypes = "```" + string.Join(", ", typeList.Values) + "```";

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("All Rant types");

                builder.WithColor(0, 0, 255);

                builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");
                builder.WithCurrentTimestamp();
                builder.AddField("Types [Name]", allTypes);

                Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            else
            {
                int typeId = DatabaseManager.GetRantType(type);
                if (content.Length == 0)
                {
                    // requested a rant from that category
                    RandomRant(type);
                    return;
                }
                else if (content.Length < 5)
                {
                    await Context.Channel.SendMessageAsync($"Rant needs to be atleast 5 characters long", false);
                    return;
                }

                if (typeId < 0)
                {
                    await Context.Channel.SendMessageAsync($"You used a type that doesnt exist yet. But because I'm so nice im adding it for you.", false);
                    bool success = DatabaseManager.Instance().AddRantType(type);
                    await Context.Channel.SendMessageAsync($"Added {type} Success: {success}", false);

                    if (!success)
                        return;

                    typeId = DatabaseManager.GetRantType(type);
                }

                var guildChannel = (SocketGuildChannel)Context.Message.Channel;

                bool successRant = DatabaseManager.AddRant(Context.Message.Id, Context.Message.Author.Id, guildChannel.Id, typeId, content);
                Context.Channel.SendMessageAsync($"Added rant for {type} Success: {successRant}", false);
            }
        }

        private async void RandomRant(string type = null)
        {
            var rant = DatabaseManager.GetRandomRant(type);
            if (rant == null)
            {
                await Context.Channel.SendMessageAsync($"No rant could be loaded for type {type} (To see all types write: '.rant types')." +
                    $"If you are trying to add a rant type '.rant {type} <your actuall rant>'", false);
                return;
            }

            var byUser = Program.Client.GetUser(rant.DiscordUserId);
            var datePosted = SnowflakeUtils.FromSnowflake(rant.DiscordMessageId);
            var rantType = DatabaseManager.GetRantTypeNameById(rant.RantTypeId);

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle($"Rant about {rantType} on {datePosted:dd.MM.yyyy}");
            builder.Description = rant.Content;
            builder.WithColor(255, 0, 255);
            builder.WithAuthor(byUser);
            //builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");
            builder.WithCurrentTimestamp();
            builder.WithFooter($"RantId: {rant.RantMessageId} TypeId: {rant.RantTypeId}");

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }


        [Command("stats")]
        public async Task Stats()
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Other);


            Dictionary<string, CommandStatistic> dbStats = new Dictionary<string, CommandStatistic>();




            // TODO clean up this mess
            /*
            var topCommands = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalCommands).Take(5);
            var topNeko = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalNeko).Take(5);
            var topNekoGif = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalNekoGif).Take(5);
            var topHolo = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalHolo).Take(5);
            var topWaifu = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalWaifu).Take(5);
            var topBaka = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalBaka).Take(5);
            var topSmug = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalSmug).Take(5);
            var topFox = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalFox).Take(5);

            var topAvatar = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalAvatar).Take(5);
            var topNekopAvatar = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalNekoAvatar).Take(5);
            var topWallpaper = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalWallpaper).Take(5);

            var topFoxgirl = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalFoxgirl).Take(5);
            var topAnimalears = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalAnimalears).Take(5);

            var topSearch = Program.BotStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalSearch).Take(5);
            */

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("BattleRush's Helper Stats");
            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");

            builder.WithColor(0, 100, 175);

            // Profile image of top person -> to update
            //builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");

            builder.WithCurrentTimestamp();


            foreach (BotMessageType type in Enum.GetValues(typeof(BotMessageType)))
            {
                builder.AddField(type.ToString(), DatabaseManager.Instance().GetTopStatisticByType(type).DiscordUser); // TODO take top5 and their count
            }

            /*
            builder.AddField("Total Commands", GetRankingString(topCommands.Select(i => i.ServerUserName + ": " + i.Stats.TotalCommands)));
            builder.AddField("Total Search", GetRankingString(topSearch.Select(i => i.ServerUserName + ": " + i.Stats.TotalSearch)), true);
            builder.AddField("Total Neko", GetRankingString(topNeko.Select(i => i.ServerUserName + ": " + i.Stats.TotalNeko)), true);
            builder.AddField("Total Neko gifs", GetRankingString(topNekoGif.Select(i => i.ServerUserName + ": " + i.Stats.TotalNekoGif)), true);
            builder.AddField("Total Holo", GetRankingString(topHolo.Select(i => i.ServerUserName + ": " + i.Stats.TotalHolo)), true);
            builder.AddField("Total Waifu", GetRankingString(topWaifu.Select(i => i.ServerUserName + ": " + i.Stats.TotalWaifu)), true);
            builder.AddField("Total Baka", GetRankingString(topBaka.Select(i => i.ServerUserName + ": " + i.Stats.TotalBaka)), true);
            builder.AddField("Total Smug", GetRankingString(topSmug.Select(i => i.ServerUserName + ": " + i.Stats.TotalSmug)), true);
            builder.AddField("Total Fox", GetRankingString(topFox.Select(i => i.ServerUserName + ": " + i.Stats.TotalFox)), true);

            builder.AddField("Total Avatar", GetRankingString(topAvatar.Select(i => i.ServerUserName + ": " + i.Stats.TotalAvatar)), true);
            builder.AddField("Total Neko Avatar", GetRankingString(topNekopAvatar.Select(i => i.ServerUserName + ": " + i.Stats.TotalNekoAvatar)), true);
            builder.AddField("Total Wallpaper", GetRankingString(topWallpaper.Select(i => i.ServerUserName + ": " + i.Stats.TotalWallpaper)), true);

            builder.AddField("Total Foxgirl", GetRankingString(topFoxgirl.Select(i => i.ServerUserName + ": " + i.Stats.TotalFoxgirl)), true);
            builder.AddField("Total Animalears", GetRankingString(topAnimalears.Select(i => i.ServerUserName + ": " + i.Stats.TotalAnimalears)), true);
            */
            Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("say")]
        public async Task Say(string message, int amount)
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                //Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            if (amount < 1)
                amount = 1;

            Context.Message.DeleteAsync();

            for (int i = 0; i < amount; i++)
            {
                Context.Channel.SendMessageAsync(message, false);
                await Task.Delay(1250);
            }
        }

        [Command("purge")]
        public async Task Purge(int count, bool fromBot = false)
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                //Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            ulong fromUserToDelete = fromBot ? 774276700557148170 : ETHDINFKBot.Program.Owner;

            if (fromBot)
            {
                Context.Message.DeleteAsync();
            }

            var messages = await Context.Channel.GetMessagesAsync(100).FlattenAsync(); //defualt is 100
            messages = messages.Where(i => i.Author.Id == fromUserToDelete).OrderByDescending(i => i.Id).Take(count);
            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messages);
        }


        [Command("countdown2021")]
        public async Task countdown2021()
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                //Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            Task t = new Task(() => CountdownLoop(Context));
            t.Start();

        }

        private DateTime Now()
        {
            return DateTime.UtcNow.AddHours(1);
        }

        private long MilisecsToMidnight()
        {
            return (int)(new DateTime(2021, 1, 1, 0, 0, 0) - Now()).TotalMilliseconds;
        }

        private async void CountdownLoop(SocketCommandContext context)
        {
            while (Now().Year == 2020)
            {
                var ms = MilisecsToMidnight();
                if (ms < 0)
                    break;
                if (ms < 10000)
                {
                    long secs = (ms / 1000) + 1;
                    string addText = "";
                    if (secs == 10)
                        addText = "only 10 secs can you believe it";
                    else if (secs == 9)
                        addText = "";
                    else if (secs == 8)
                        addText = "uhm my time is running out";
                    else if (secs == 7)
                        addText = "";
                    else if (secs == 6)
                        addText = "uhm";
                    else if (secs == 5)
                        addText = "what do I say";
                    else if (secs == 4)
                        addText = "";
                    else if (secs == 3)
                        addText = "I could say something inspirational";
                    else if (secs == 2)
                        addText = "";
                    else if (secs == 1)
                        addText = "guess what happens next?";

                    await context.Channel.SendMessageAsync($"{secs}... {addText}");

                    Thread.Sleep((int)(ms % 1000));
                }
                else if (ms < 60000)
                {
                    await context.Channel.SendMessageAsync($"{(ms / 1000) + 1}...");
                    Thread.Sleep((int)(ms % 5000));
                }
                else
                {
                    await context.Channel.SendMessageAsync($"{(ms / 60000) + 1} min left...");
                    Thread.Sleep((int)(ms % 60000));
                }
            }

            await context.Channel.SendMessageAsync($"Is it 2021?");
            Thread.Sleep(2);
            await context.Channel.SendMessageAsync($"Checks the clock");
            Thread.Sleep(2);
            await context.Channel.SendMessageAsync($"I guess it is. **Happy 2021**");
            Thread.Sleep(5);
            await context.Channel.SendMessageAsync($"Also **WE ARE NOW 1 YEAR CLOSER TO THE BASISPRÜFUNG EXAMS**");
        }


        [Command("test")]
        public async Task Test()
        {

            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("BattleRush's Helper Stats");
            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");

            builder.WithColor(0, 100, 175);

            // Profile image of top person -> to update
            //builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");

            builder.WithCurrentTimestamp();
            builder.AddField("Top Emoji Usage", $"<:checkmark:778202017372831764>");
            builder.AddField("<:checkmark:778202017372831764>", $"test");
            Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        // TODO Duplicate (SERIOUSLY REWORK THAT SHIT YOU LAZY ASS)
        private bool ContainsForbiddenQuery(string command)
        {
            List<string> forbidden = new List<string>()
            {
                "alter",
                "analyze",
                "attach",
                "transaction",
                "comment",
                "commit",
                "create",
                "delete",
                "detach",
                "database",
                "drop",
                "insert",
                "pragma",
                "reindex",
                "release",
                "replace",
                "rollback",
                "savepoint",
                "update",
                "upsert",
                "vacuum",
                "recursive ", // idk why it breaks when i have time ill take a look
                "with "
            };

            //.sql query select * from RedditPosts,SubredditInfos,DiscordUsers,BannedLinks,CommandStatistics,CommandTypes,DiscordMessages as x,EmojiStatistics, PingStatistics,SavedMessages,RedditImages where PostTitle = EmojiName or UpvoteCount = x.MessageId

            foreach (var item in forbidden)
            {
                if (command.ToLower().Contains(item.ToLower()))
                    return true;
            }

            return false;
        }


        [Command("r")]
        public async Task Reddit(string subreddit = "")
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            if (subreddit.Contains("'") || subreddit.Contains("\""))
                return;

            if (ContainsForbiddenQuery(subreddit))
                return;

            LogManager.ProcessMessage(Context.Message.Author, BotMessageType.Reddit);

            if (subreddit.ToLower() == "all")
            {
                string allSubreddits = "**Available subreddits**" + Environment.NewLine;
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var subredditInfos = context.SubredditInfos.AsQueryable().OrderBy(i => i.SubredditName).ToList();

                    foreach (var item in subredditInfos)
                    {
                        allSubreddits += $"{item.SubredditName}, ";
                    }

                    // TODO better text
                    Context.Channel.SendMessageAsync(allSubreddits, false);
                }
            }
            else
            {
                // TODO text posts

                string link = "";
                try
                {
                    // TODO Better escaping
                    subreddit = subreddit.Replace("'", "''");
                    using (ETHBotDBContext context = new ETHBotDBContext())
                    {
                        using (var command = context.Database.GetDbConnection().CreateCommand())
                        {
                            // TODO sql input escaping
                            command.CommandText = @$"select ri.Link from SubredditInfos si
left join RedditPosts pp on si.SubredditId = pp.SubredditInfoId
left join RedditImages ri on pp.RedditPostId = ri.RedditPostId
where si.SubredditName like '%{subreddit}%' and ri.Link is not null and pp.IsNSFW = 0
ORDER BY RANDOM() LIMIT 1";// todo nsfw test
                            context.Database.OpenConnection();
                            using (var result = command.ExecuteReader())
                            {
                                while (result.Read())
                                {
                                    link = result.GetString(0);
                                    break;
                                }
                            }
                        }
                    }

                }
                catch (Exception ex)
                {

                }

                Context.Channel.SendMessageAsync(link, false);

            }
            /*
 * 
 * SELECT column FROM table 
ORDER BY RANDOM() LIMIT 1

*/
        }

        [Command("disk")]
        public void DirSizeReddit()
        {
            try
            {
                DirectoryInfo info = new DirectoryInfo("Reddit");
                long size = DirSize(info);

                Context.Channel.SendMessageAsync($"Current Reddit disk usage :{size / (decimal)1024 / 1024 / 1024} GB", false);
            }
            catch (Exception ex)
            {
                Context.Channel.SendMessageAsync(ex.ToString(), false);
            }
        }

        public static long DirSize(DirectoryInfo d)
        {
            long size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += DirSize(di);
            }
            return size;
        }

        [Command("rp")]
        public async Task RedditPost(string subreddit = "")
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            if (subreddit.Contains("'") || subreddit.Contains("\""))
                return; 

            if (ContainsForbiddenQuery(subreddit))
                return;

            LogManager.ProcessMessage(Context.Message.Author, BotMessageType.Reddit);

            if (subreddit.ToLower() == "all")
            {
                string allSubreddits = "**Available subreddits**" + Environment.NewLine;
                Context.Channel.SendMessageAsync(allSubreddits, false);
                allSubreddits = "";

                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var subredditInfos = context.SubredditInfos.AsQueryable().OrderBy(i => i.SubredditName).ToList();

                    foreach (var item in subredditInfos)
                    {
                        allSubreddits += $"{item.SubredditName}, ";

                        if (allSubreddits.Length > 1900)
                        {
                            // TODO better text
                            Context.Channel.SendMessageAsync(allSubreddits, false);
                            allSubreddits = "";
                        }
                    }


                }
            }
            else
            {

                int postId = 0;
                try
                {
                    // TODO Better escaping
                    subreddit = subreddit.Replace("'", "''");
                    using (ETHBotDBContext context = new ETHBotDBContext())
                    {
                        using (var command = context.Database.GetDbConnection().CreateCommand())
                        {
                            // TODO sql input escaping
                            command.CommandText = @$"select pp.RedditPostId from SubredditInfos si
left join RedditPosts pp on si.SubredditId = pp.SubredditInfoId
where si.SubredditName like '%{subreddit}%' and pp.IsNSFW = 0
ORDER BY RANDOM() LIMIT 1";// todo nsfw test
                            context.Database.OpenConnection();
                            using (var result = command.ExecuteReader())
                            {
                                while (result.Read())
                                {
                                    postId = result.GetInt32(0);
                                    break;
                                }
                            }
                        }


                        var redditPost = DatabaseManager.GetRedditPostById(postId);

                        var subredditInfo = DatabaseManager.GetSubreddit(redditPost.SubredditInfoId);

                        EmbedBuilder builder = new EmbedBuilder();

                        builder.WithTitle(redditPost.PostTitle);
                        builder.WithUrl("https://www.reddit.com/" + redditPost.Permalink);

                        var content = redditPost.IsText ? redditPost.Content : "";

                        if (content.Length > 2000)
                        {
                            content = content.Substring(0, 2000);
                        }

                        // TODO if subreddit name null get the subreddit 
                        builder.WithDescription(content);
                        builder.WithColor(0, 0, 255);

                        //builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");
                        builder.WithCurrentTimestamp();
                        string url = redditPost.Url;
                        if (url.Contains("v.redd.it"))
                        {
                            url += "/DASH_720.mp4";
                        }

                        builder.WithImageUrl(url);
                        builder.AddField("Infos", $"Posted by: {redditPost.Author} in /r/{subredditInfo?.SubredditName} at {redditPost.PostedAt}");
                        builder.AddField("Upvotes", redditPost.UpvoteCount, true);
                        builder.AddField("Downvotes", redditPost.DownvoteCount, true);
                        builder.AddField("NSFW", redditPost.IsNSFW, true);

                        Context.Channel.SendMessageAsync("", false, builder.Build());

                    }

                }
                catch (Exception ex)
                {

                }
            }
            /*
 * 
 * SELECT column FROM table 
ORDER BY RANDOM() LIMIT 1

*/
        }

        /*
        [Command("radmin")]
        public async Task RedditAdmin(string command = "", string value = "")
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                Context.Channel.SendMessageAsync("You aren't allowed to run this command ask BattleRush to run it for you", false);
                return;
            }

            if (command == "")
                command = "help";

            switch (command)
            {
                case "help":
                    Context.Channel.SendMessageAsync("help, status, add NAME, start NAME", false);
                    break;

                case "status":
                    CheckReddit();
                    break;

                case "add":
                    AddSubreddit(value);
                    break;

                case "start":
                    
                    break;
                default:
                    break;
            }

        }
        */







        [Command("lb")]
        public async Task Leaderboard()
        {

            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;
            try
            {

                var author = Context.Message.Author;
                LogManager.ProcessMessage(author, BotMessageType.Other);

                var statText = DatabaseManager.GetTopEmojiStatisticByText(10);
                var statTextBot = DatabaseManager.GetTopEmojiStatisticByBot(10);
                var statTextOnce = DatabaseManager.GetTopEmojiStatisticByTextOnce(10);
                var statTextReaction = DatabaseManager.GetTopEmojiStatisticByReaction(10);
                //var statEmoji = DatabaseManager.ping(10);


                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("BattleRush's Helper Stats");
                //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");

                builder.WithColor(0, 100, 175);

                // Profile image of top person -> to update
                //builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");

                builder.WithCurrentTimestamp();
                builder.AddField("Top Emoji", GetRankingString(statTextOnce.Select(i => $"<{(i.Animated ? "a:" : ":") + i.EmojiName}:{i.EmojiId}> " + i.UsedInTextOnce)), true);
                builder.AddField("Top Emoji (all)", GetRankingString(statText.Select(i => $"<{(i.Animated ? "a:" : ":") + i.EmojiName}:{i.EmojiId}> " + i.UsedInText)), true);
                //builder.AddField("Top Emoji (from Bots)", GetRankingString(statTextBot.Select(i => $"<{(i.Animated ? "a:" : ":") + i.EmojiName}:{i.EmojiId}> " + i.UsedByBots)), true);
                builder.AddField("Top Reactions", GetRankingString(statTextReaction.Select(i => $"<{(i.Animated ? "a:" : ":") + i.EmojiName}:{i.EmojiId}> " + i.UsedAsReaction)), true);
                builder.AddField("Top Pinged Users", "TODO");

                Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            catch (Exception ex)
            {
                Context.Channel.SendMessageAsync(ex.ToString(), false);
            }
        }



        private BannedLink GetReportInfoByImage(string imageUrl)
        {
            return DatabaseManager.GetBannedLink(imageUrl);
        }

        private string GetRankingString(IEnumerable<string> list)
        {
            string rankingString = "";
            int pos = 1;
            foreach (var item in list)
            {
                string boldText = pos == 1 ? " ** " : "";
                rankingString += $"{boldText}{pos}) {item}{boldText}{Environment.NewLine}";
                pos++;
            }
            return rankingString;
        }
    }
}
