using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using DuckSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot
{
    public class DiscordModule : ModuleBase<SocketCommandContext>
    {
        [Command("code")]
        public async Task SourceCode()
        {
            Context.Channel.SendMessageAsync("", false);
        }

        [Command("duck")]
        [Summary("Returns info about the current user, or the user parameter, if one passed.")]
        public async Task DuckDuckGo(string searchString,
        [Summary("The (optional) user to get info from")]
        SocketUser user = null)
        {
            var userInfo = user ?? Context.Message.Author;
            //await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");

            var reply = await new DuckSharpClient().GetInstantAnswerAsync(searchString);



            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Search for " + searchString);
            builder.WithDescription(reply.AbstractText + Environment.NewLine + reply.AbstractUrl);
            builder.WithColor(255, 0, 0);

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

            var reply = new Engine().Search(searchString);



            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Search for " + searchString);
            builder.WithDescription(reply.Description?.Substring(0, reply.Description.Length > 100 ? 100 : reply.Description.Length));
            builder.WithColor(255, 0, 0);

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



    }
}
