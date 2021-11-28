using Discord;
using Discord.Commands;
using ETHBot.DataLayer;
using ETHDINFKBot.Drawing;
using ETHDINFKBot.Helpers;
using FFMpegCore;
using FFMpegCore.Enums;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ETHDINFKBot.Modules
{

    public class MessageInfo
    {
        public int Count { get; set; }
        public ulong ChannelId { get; set; }
        public DateTimeOffset DateTime { get; set; }
    }

    [Group("test")]
    public class TestModule : ModuleBase<SocketCommandContext>
    {
        [Command("movie", RunMode = RunMode.Async)]
        public async Task CreateMovie()
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            // Get users that havent pinged the role in the last 72h
            var sqlQuery = @"
SELECT 
    DiscordMessageId,
    DiscordChannelId
FROM DiscordMessages
WHERE DiscordChannelId = 768600365602963496";

            Stopwatch watch = new Stopwatch();

            watch.Start();

            List<DateTimeOffset> messageTimes = new List<DateTimeOffset>();
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                messageTimes = context.DiscordMessages.AsQueryable().Where(i => i.DiscordChannelId == 768600365602963496).Select(i => SnowflakeUtils.FromSnowflake(i.DiscordMessageId)).ToList();
            }
            watch.Stop();

            Context.Channel.SendMessageAsync($"Retreived data in {watch.ElapsedMilliseconds}ms");

            //var queryResult = await SQLHelper.GetQueryResults(null, sqlQuery, true, 10_000_000, true, true);

            List<MessageInfo> messageInfos = new List<MessageInfo>();

            //foreach (var item in queryResult.Data)
            //{
            //    messageInfos.Add(new MessageInfo()
            //    {
            //        ChannelId = Convert.ToUInt64(item[1]),
            //        DateTime = SnowflakeUtils.FromSnowflake(Convert.ToUInt64(item[1]))
            //    });
            //}

            var firstDateTime = messageTimes.Min();
            var lastDateTime = messageTimes.Max();

            double maxFrames = 600;

            int bound = (int)((lastDateTime - firstDateTime).TotalMinutes / maxFrames);

            Context.Channel.SendMessageAsync($"Group by {bound} minutes, Total mins {(lastDateTime - firstDateTime).TotalMinutes}");

            /*
            var groups = messageTimes.GroupBy(x =>
            {
                var stamp = x;
                stamp = stamp.AddMinutes(-(stamp.DateTime.Minute % bound));
                stamp = stamp.AddMilliseconds(-stamp.DateTime.Millisecond - 1000 * stamp.DateTime.Second);
                return stamp;
            }).Select(g => new { TimeStamp = g.Key, Value = g.Count() }).ToList();
            */

            // https://stackoverflow.com/questions/47763874/how-to-linq-query-group-by-2-hours-interval
            var groups = messageTimes.GroupBy(x =>
            {
                var stamp = x;
                stamp = stamp.AddHours(-(stamp.Hour % 12));
                stamp = stamp.AddMinutes(-(stamp.Minute));
                stamp = stamp.AddMilliseconds(-stamp.Millisecond - 1000 * stamp.Second);
                return stamp;
            }).Select(g => new { TimeStamp = g.Key, Value = g.Count() }).ToList();

            Context.Channel.SendMessageAsync($"Total frames {groups.Count}");

            Dictionary<DateTime, int> dataPointsSpam = new();

            int val = 0;
            foreach (var group in groups)
            {
                val += group.Value;
                dataPointsSpam.Add(group.TimeStamp.DateTime, val);
            }

            string basePath = Path.Combine(Program.BasePath, "MovieFrames");
            string baseOutputPath = Path.Combine(Program.BasePath, "MovieOutput");

            // Clean up any remaining files
            if (Directory.Exists(basePath))
                Directory.Delete(basePath, true);
            if (Directory.Exists(baseOutputPath))
                Directory.Delete(baseOutputPath, true);


            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);
            if (!Directory.Exists(baseOutputPath))
                Directory.CreateDirectory(baseOutputPath);

            for (int i = 2; i <= dataPointsSpam.Count; i++)
            {
                if (i % 250 == 0)
                    Context.Channel.SendMessageAsync($"Frame gen {i} out of {dataPointsSpam.Count}");

                // TODO optimize some lines + move to draw helper
                var dataPoints = dataPointsSpam.Take(i).ToDictionary(i => i.Key, i => i.Value); ;

                var startTime = dataPoints.First().Key;
                var endTime = dataPoints.Last().Key;


                var drawInfo = DrawingHelper.GetEmptyGraphics();
                var padding = DrawingHelper.DefaultPadding;
                padding.Left = 200; // large numbers
                var labels = DrawingHelper.GetLabels(dataPoints, 6, 10, true, startTime, endTime, " msg");
                var gridSize = new GridSize(drawInfo.Bitmap, padding);
                var dataPointList = DrawingHelper.GetPoints(dataPoints, gridSize, true, startTime, endTime);

                DrawingHelper.DrawGrid(drawInfo.Canvas, gridSize, padding, labels.XAxisLables, labels.YAxisLabels, $"Messages in #spam");
                // todo add 2. y Axis on the right

                DrawingHelper.DrawLine(drawInfo.Canvas, drawInfo.Bitmap, dataPointList, 6, new SKPaint() { Color = new SKColor(255, 0, 0) }, "msg in #spam", 0, true); //new Pen(System.Drawing.Color.LightGreen)


                using (var data = drawInfo.Bitmap.Encode(SKEncodedImageFormat.Png, 80))
                {
                    // save the data to a stream
                    using (var stream = File.OpenWrite(Path.Combine(basePath, $"{i.ToString("D8")}.png")))
                    {
                        data.SaveTo(stream);
                    }
                }
            }

            try
            {
                int random = new Random().Next(1_000_000_000);
                GlobalFFOptions.Configure(options => options.BinaryFolder = Program.Settings.FFMpegPath);

                var imageInfos = Directory.GetFiles(Path.Combine(basePath)).ToList().OrderBy(i => i).Select(i => ImageInfo.FromPath(i)).ToArray();
                string fileName = Path.Combine(baseOutputPath, $"movie_{random}.mp4");
                FFMpeg.JoinImageSequence(fileName, frameRate: 30, imageInfos);

                Context.Channel.SendFileAsync(fileName);
            }
            catch (Exception ex)
            {
                Context.Channel.SendMessageAsync(ex.ToString());
            }
        }
    }
}
