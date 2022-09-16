using Discord.Commands;
using ETHDINFKBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Modules
{
    // TODO FINISH STATS FIRSTLY

    [Group("stats")]
    public class StatsModule : ModuleBase<SocketCommandContext>
    {

        //private void Me



        [Group("daily")]
        public class DailyStats : ModuleBase<SocketCommandContext>
        {
            [Command("all")]
            public async Task AllStats()
            {
                // TODO MEssage that its posting all
                await MessagesStats();
                await EmoteStats();
            }

            [Command("messages")]
            public async Task MessagesStats()
            {
                try
                {
                    using (var stream = StatsHelper.GetMessageGraph(DateTime.Now.AddDays(-1), DateTime.Now))
                        await Context.Channel.SendFileAsync(stream, "graph.png");
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.ToString().Substring(0, Math.Min(ex.ToString().Length, 1990)));
                }
            }

            [Command("emote")]
            public async Task EmoteStats()
            {
                var embed = StatsHelper.GetMostEmoteUsed(DateTime.Now.AddDays(-1), DateTime.Now, 15);
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
            // messages
            // reactions
            // emotes

            // messages per minute

            // top new emote poster



        }

        [Group("weekly")]
        public class WeeklyStats : ModuleBase<SocketCommandContext>
        {
            [Command("all")]
            public async Task AllStats()
            {
                // TODO MEssage that its posting all
                await MessagesStats();
                await EmoteStats();
            }

            [Command("messages")]
            public async Task MessagesStats()
            {
                try
                {
                    using (var stream = StatsHelper.GetMessageGraph(DateTime.Now.AddDays(-7), DateTime.Now, 60))
                        await Context.Channel.SendFileAsync(stream, "graph.png");
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.ToString().Substring(0, Math.Min(ex.ToString().Length, 1990)));
                }
            }

            [Command("emote")]
            public async Task EmoteStats()
            {
                var embed = StatsHelper.GetMostEmoteUsed(DateTime.Now.AddDays(-7), DateTime.Now, 15);
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
        }

        [Group("monthly")]
        public class MonthlyStats : ModuleBase<SocketCommandContext>
        {
            [Command("all")]
            public async Task AllStats()
            {
                // TODO MEssage that its posting all
                await MessagesStats();
                await EmoteStats();
            }

            [Command("messages")]
            public async Task MessagesStats()
            {
                try
                {
                    using (var stream = StatsHelper.GetMessageGraph(DateTime.Now.AddMonths(-1), DateTime.Now, 600))
                        await Context.Channel.SendFileAsync(stream, "graph.png");
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.ToString().Substring(0, Math.Min(ex.ToString().Length, 1990)));
                }
            }

            [Command("emote")]
            public async Task EmoteStats()
            {
                var embed = StatsHelper.GetMostEmoteUsed(DateTime.Now.AddMonths(-1), DateTime.Now, 15);
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
        }

        [Group("yearly")]
        public class YearlyStats : ModuleBase<SocketCommandContext>
        {

            [Command("all")]
            public async Task AllStats()
            {
                // TODO MEssage that its posting all
                await MessagesStats();
                await EmoteStats();
            }

            [Command("messages")]
            public async Task MessagesStats()
            {
                try
                {
                    using (var stream = StatsHelper.GetMessageGraph(DateTime.Now.AddYears(-1), DateTime.Now, 3600))
                        await Context.Channel.SendFileAsync(stream, "graph.png");
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.ToString().Substring(0, Math.Min(ex.ToString().Length, 1990)));
                }
            }

            [Command("emote")]
            public async Task EmoteStats()
            {
                var embed = StatsHelper.GetMostEmoteUsed(DateTime.Now.AddYears(-1), DateTime.Now, 15);
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
        }

        [Group("all")]
        public class AllyStats : ModuleBase<SocketCommandContext>
        {
            // all is same as years

            [Command("all")]
            public async Task AllStats()
            {
                // TODO MEssage that its posting all
                await MessagesStats();
                await EmoteStats();
            }

            [Command("messages")]
            public async Task MessagesStats()
            {
                try
                {
                    using (var stream = StatsHelper.GetMessageGraph(DateTime.Now.AddYears(-1), DateTime.Now, 3600))
                        await Context.Channel.SendFileAsync(stream, "graph.png");
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.ToString().Substring(0, Math.Min(ex.ToString().Length, 1990)));
                }
            }

            [Command("emote")]
            public async Task EmoteStats()
            {
                var embed = StatsHelper.GetMostEmoteUsed(DateTime.Now.AddYears(-1), DateTime.Now, 15);
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
        }
    }
}
