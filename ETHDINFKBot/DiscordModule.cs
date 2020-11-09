using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using DuckSharp;
using ETHDINFKBot.Log;
using NekosSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot
{
    public class DiscordModule : ModuleBase<SocketCommandContext>
    {
        public static NekoClient NekoClient = new NekoClient("BattleRush's Helper");

        // TODO Remove alot of the redundant code for loggining and stats

        [Command("code")]
        [Alias("source")]
        public async Task SourceCode()
        {
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Other);

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Sourcecode for BattleRush's Helper");
            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");
            builder.WithDescription(@"TODO Create some meaningfull text here to go with such an awesome bot.
**Source code: **
**https://github.com/BattleRush/ETH-DINFK-Bot**");
            builder.WithColor(0, 255, 0);

            builder.WithThumbnailUrl("https://avatars0.githubusercontent.com/u/11750584");
            builder.WithFooter($"https://github.com/BattleRush");
            builder.WithCurrentTimestamp();
            builder.AddField("Github profile", "https://github.com/BattleRush");
            builder.AddField("The Roslyn .NET compiler (contributor)", "https://github.com/BattleRush/roslyn");
            builder.AddField("C# Console example for Steam Trading", "https://github.com/BattleRush/SteamTradeExample-Console");
            //builder.AddField("http://battlerush.github.io/", "https://github.com/BattleRush/battlerush.github.io");
            builder.AddField("PokemonGoFlairSelection", "https://github.com/BattleRush/PokemonGoFlairSelection");
            builder.AddField("Simple lightweight website to calculate your grades", "https://github.com/BattleRush/CalculateGrades");

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("help")]
        [Alias("about")]
        public async Task HelpOutput()
        {
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Other);

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("BattleRush's Helper Help");
            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");
            builder.WithDescription(@"Prefix for all comands is "".""
Version: 0.0.0.I didn't implement this yet");
            builder.WithColor(0, 0, 255);

            builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");
            builder.WithFooter($"If you can read this then ping Mert | TroNiiXx | [13]");
            builder.WithCurrentTimestamp();
            builder.AddField("help", "Returns this message");
            builder.AddField("code or source", "Return the sourcecode for this bot");
            builder.AddField("google", "Search on Google");
            builder.AddField("duck", "Search on DuckDuckGo");

            builder.AddField("stats", "Returns stats", true);
            builder.AddField("neko", "Neko Image", true);
            builder.AddField("nekogif", "Neko Gif", true);
            builder.AddField("fox", "Fox image", true);
            builder.AddField("waifu", "Waifu image", true);
            builder.AddField("baka", "Baka image", true);
            builder.AddField("smug", "Smug image", true);
            builder.AddField("holo", "Holo image", true);

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("duck")]
        [Summary("Returns info about the current user, or the user parameter, if one passed.")]
        public async Task DuckDuckGo(string searchString,
        [Summary("The (optional) user to get info from")]
        SocketUser user = null)
        {
            var userInfo = user ?? Context.Message.Author;
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
        public async Task GoogleSearch(string searchString,
        [Summary("The (optional) user to get info from")]
        SocketUser user = null)
        {
            var userInfo = user ?? Context.Message.Author;
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
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Neko);

            var req = await NekoClient.Image.Neko();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        [Command("nekogif")]
        public async Task NekoGif()
        {
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Neko);

            var req = await NekoClient.Image.NekoGif();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }


        [Command("fox")]
        public async Task Fox()
        {
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Other);

            var req = await NekoClient.Image.Fox();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        [Command("waifu")]
        public async Task Waifu()
        {
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Other);

            var req = await NekoClient.Image.Waifu();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        [Command("baka")]
        public async Task Baka()
        {
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Other);

            var req = await NekoClient.Image.Baka();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        [Command("smug")]
        public async Task Smug()
        {
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Other);

            var req = await NekoClient.Image.Smug();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }

        [Command("holo")]
        public async Task Holo()
        {
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Other);

            var req = await NekoClient.Image.Holo();
            Context.Channel.SendMessageAsync(req.ImageUrl, false);
        }



        [Command("stats")]
        public async Task Stats()
        {
            var author = Context.Message.Author;
            LogManager.ProcessMessage(author, BotMessageType.Other);


            var top3Commands = Program.GlobalStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalCommands).Take(5);
            var top3Neko = Program.GlobalStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalNeko).Take(5);
            var top3Search = Program.GlobalStats.DiscordUsers.OrderByDescending(i => i.Stats.TotalSearch).Take(5);


            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("BattleRush's Helper Stats");
            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");

            builder.WithColor(0, 100, 175);

            // Profile image of top person -> to update
            //builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");

            builder.WithCurrentTimestamp();
            builder.AddField("Total Commands", string.Join(Environment.NewLine, top3Commands.ToList().Select(i => i.DiscordName + ": " + i.Stats.TotalCommands).ToList()));
            builder.AddField("Total Nekos", string.Join(Environment.NewLine, top3Neko.ToList().Select(i => i.DiscordName + ": " + i.Stats.TotalNeko).ToList()), true);
            builder.AddField("Total Searches", string.Join(Environment.NewLine, top3Search.ToList().Select(i => i.DiscordName + ": " + i.Stats.TotalSearch).ToList()), true);

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }
    }
}
