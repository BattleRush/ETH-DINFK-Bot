using Discord;
using ETHBot.DataLayer.Data.Discord;

// SYSTEM.DRAWING
// using ETHDINFKBot.Drawing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Helpers
{
    public static class StatsHelper
    {

        public static Stream GetMessageGraph(DateTime from, DateTime to, int groupByMins = 5)
        {
            // SYSTEM.DRAWING
            return new MemoryStream();
            /*
            var dataPoints = DatabaseManager.Instance().GetMessageCountGrouped(from, to, groupByMins);
            var drawInfo = DrawingHelper.GetEmptyGraphics();
            var padding = DrawingHelper.DefaultPadding;
            var labels = DrawingHelper.GetLabels(dataPoints, 6, 10, true, from, to);
            var gridSize = new GridSize(drawInfo.Bitmap, padding);
            var dataPointList = DrawingHelper.GetPoints(dataPoints, gridSize, true, from, to);

            DrawingHelper.DrawGrid(drawInfo.Graphics, gridSize, padding, labels.XAxisLables, labels.YAxisLabels, "Stats table");
            DrawingHelper.DrawPoints(drawInfo.Graphics, drawInfo.Bitmap, dataPointList, 6, null, "Message count", 0);

            var stream = CommonHelper.GetStream(drawInfo.Bitmap);

            drawInfo.Bitmap.Dispose();
            drawInfo.Graphics.Dispose();
            return stream;*/
        }


        public static EmbedBuilder GetMostEmoteUsed(DateTime from, DateTime to, int top = 10)
        {
            var dbManager = DatabaseManager.Instance();

            var emoteHistoryList = dbManager.GetEmoteHistoryUsage(from, to);

            var reactions = emoteHistoryList.Where(i => i.IsReaction);
            var textEmotes = emoteHistoryList.Where(i => !i.IsReaction);

            var groupedReactions = reactions.GroupBy(i => i.DiscordEmoteId).ToDictionary(g => g.Key, g => g.Select(i => i.Count).Sum()).OrderByDescending(i => i.Value).Take(top); // sum to also get reaction removed
            var groupedTextEmotes = textEmotes.GroupBy(i => i.DiscordEmoteId).ToDictionary(g => g.Key, g => g.Count()).OrderByDescending(i => i.Value).Take(top);


            Dictionary<DiscordEmote, int> topReactions = new Dictionary<DiscordEmote, int>();
            Dictionary<DiscordEmote, int> topEmote = new Dictionary<DiscordEmote, int>();


            foreach (var item in groupedReactions)
            {
                var emote = dbManager.GetDiscordEmoteById(item.Key);
                topReactions.Add(emote, item.Value);
            }

            foreach (var item in groupedTextEmotes)
            {
                var emote = dbManager.GetDiscordEmoteById(item.Key);
                topEmote.Add(emote, item.Value);
            }



            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("BattleRush's Helper Emote Stats");
            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");

            builder.WithColor(0, 100, 175);

            // Profile image of top person -> to update
            //builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");

            builder.WithCurrentTimestamp();
            builder.AddField("Top Emote", GetRankingString(topEmote), true);
            builder.AddField("Top Reactions", GetRankingString(topReactions), true);


            //Context.Channel.SendMessageAsync("", false, builder.Build());



            //return groups.ToDictionary(g => g.TimeStamp, g => g.Value);

            return builder;

        }

        private static string GetRankingString(Dictionary<DiscordEmote, int> emotes)
        {
            string rankingString = "";
            int pos = 1;
            foreach (var emote in emotes)
            {
                string boldText = pos == 1 ? " ** " : "";
                rankingString += $"{boldText}{pos}) <{ (emote.Key.Animated ? "a:" : ":") + emote.Key.EmoteName}:{emote.Key.DiscordEmoteId}> {emote.Value}{boldText}{Environment.NewLine}";
                pos++;
            }
            return rankingString;
        }
    }
}
