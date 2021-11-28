using Discord;
using Discord.Commands;
using ETHBot.DataLayer;
using ETHDINFKBot.Drawing;
using ETHDINFKBot.Helpers;
//using FFMpegCore;
//using FFMpegCore.Enums; // removed because of System.Drawing issues
using Xabe.FFmpeg;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Discord.WebSocket;

namespace ETHDINFKBot.Modules
{

    public class MessageInfo
    {
        public ulong ChannelId { get; set; }
        public DateTimeOffset DateTime { get; set; }
    }

    public class ParsedMessageInfo
    {
        public ulong ChannelId { get; set; }
        public string ChannelName { get; set; }
        public Dictionary<DateTimeOffset, int> Info { get; set; }

        public SKColor Color { get; set; }
    }

    [Group("test")]
    public class TestModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger _logger = new Logger<TestModule>(Program.Logger);

        [Command("movie", RunMode = RunMode.Async)]
        public async Task CreateMovie(bool stacked, int groupByHour, int fps, bool drawDots, params ulong[] channelIds)
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

            List<MessageInfo> messageTimes = new List<MessageInfo>();
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                messageTimes = context.DiscordMessages.AsQueryable().Select(i => new MessageInfo() { ChannelId = i.DiscordChannelId, DateTime = SnowflakeUtils.FromSnowflake(i.DiscordMessageId) }).ToList();
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

            var firstDateTime = messageTimes.Min(i => i.DateTime);
            var lastDateTime = messageTimes.Max(i => i.DateTime);

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
                stamp.DateTime = stamp.DateTime.AddHours(-(stamp.DateTime.Hour % groupByHour));
                stamp.DateTime = stamp.DateTime.AddMinutes(-(stamp.DateTime.Minute));
                stamp.DateTime = stamp.DateTime.AddMilliseconds(-stamp.DateTime.Millisecond - 1000 * stamp.DateTime.Second);
                return stamp.DateTime;
            }).Select(g => new { Key = g.Key, Value = g.ToList() }).ToList();

            var keys = groups.Select(x => x.Key).ToList();

            List<ParsedMessageInfo> parsedMessageInfos = new List<ParsedMessageInfo>();

            var channels = DatabaseManager.Instance().GetDiscordAllChannels(Context.Guild.Id);

            try
            {
                foreach (var item in groups)
                {
                    foreach (var value in item.Value)
                    {
                        if (!parsedMessageInfos.Any(i => i.ChannelId == value.ChannelId))
                        {
                            var channelDB = channels.SingleOrDefault(i => i.DiscordChannelId == value.ChannelId);
                            if (channelDB == null)
                                continue;

                            // ingore this channel
                            if (channelIds.Length > 0 && !channelIds.Contains(value.ChannelId))
                                continue;

                            parsedMessageInfos.Add(new ParsedMessageInfo()
                            {
                                ChannelId = value.ChannelId,
                                Info = new Dictionary<DateTimeOffset, int>(),
                                ChannelName = channelDB.ChannelName,
                                Color = new SKColor((byte)new Random().Next(0, 255), (byte)new Random().Next(0, 255), (byte)new Random().Next(0, 255))
                            });
                        }

                        var channelInfo = parsedMessageInfos.Single(i => i.ChannelId == value.ChannelId);
                        if (channelInfo.Info.ContainsKey(value.DateTime))
                            channelInfo.Info[value.DateTime] += 1;
                        else
                            channelInfo.Info.Add(value.DateTime, 1);
                    }
                }
            }
            catch (Exception ex)
            {

            }

            Context.Channel.SendMessageAsync($"Total frames {groups.Count()}");

            //int val = 0;
            //foreach (var group in groups)
            //{
            //    val += group.Value;
            //    dataPointsSpam.Add(group.TimeStamp.DateTime, val);
            //}

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


            if (stacked)
            {
                foreach (var info in parsedMessageInfos)
                {
                    var values = info.Info.OrderBy(i => i.Key);
                    int val = 0;
                    foreach (var value in values)
                    {
                        val += value.Value;
                        info.Info[value.Key] = val;
                    }
                }
            }

            try
            {

                for (int i = 2; i <= keys.Count; i++)
                {
                    if (i % 250 == 0)
                        Context.Channel.SendMessageAsync($"Frame gen {i} out of {keys.Count}");

                    var startTime = keys.Take(i).Min();
                    var endTime = keys.Take(i).Max();

                    int maxY = 0;

                    // TODO calculate the rolling info

                    foreach (var item in parsedMessageInfos)
                    {
                        foreach (var info in item.Info)
                        {
                            // only consider until this key
                            if (info.Key > endTime)
                                continue;

                            if (maxY < info.Value)
                                maxY = info.Value;
                        }
                    }

                    var drawInfo = DrawingHelper.GetEmptyGraphics();
                    var padding = DrawingHelper.DefaultPadding;

                    padding.Left = 200; // large numbers

                    var labels = DrawingHelper.GetLabels(startTime.DateTime, endTime.DateTime, 0, maxY, 6, 10, " msg");

                    var gridSize = new GridSize(drawInfo.Bitmap, padding);

                    DrawingHelper.DrawGrid(drawInfo.Canvas, gridSize, padding, labels.XAxisLables, labels.YAxisLabels, $"Messages count");
                    int lineIndex = 0;
                    foreach (var item in parsedMessageInfos)
                    {
                        // TODO optimize some lines + move to draw helper
                        var dataPoints = item.Info.Where(j => j.Key <= endTime).OrderBy(i => i.Key).ToDictionary(j => j.Key.DateTime, j => j.Value);

                        // todo add 2. y Axis on the right
                        var dataPointList = DrawingHelper.GetPoints(dataPoints, gridSize, true, startTime.DateTime, endTime.DateTime, false, maxY);

                        DrawingHelper.DrawLine(drawInfo.Canvas, drawInfo.Bitmap, dataPointList, 6, new SKPaint() { Color = item.Color }, "msg in #" + item.ChannelName, lineIndex, drawDots); //new Pen(System.Drawing.Color.LightGreen)
                        lineIndex++;
                    }

                    using (var data = drawInfo.Bitmap.Encode(SKEncodedImageFormat.Png, 80))
                    {
                        // save the data to a stream
                        using (var stream = File.OpenWrite(Path.Combine(basePath, $"{i.ToString("D8")}.png")))
                        {
                            data.SaveTo(stream);
                        }
                    }
                }


                int random = new Random().Next(1_000_000_000);
                //GlobalFFOptions.Configure(options => options.BinaryFolder = Program.Settings.FFMpegPath);
                Xabe.FFmpeg.FFmpeg.SetExecutablesPath(Program.Settings.FFMpegPath);

                var files = Directory.GetFiles(Path.Combine(basePath)).ToList().OrderBy(i => i);
                string fileName = Path.Combine(baseOutputPath, $"movie_{random}.mp4");

                var conversion = new Conversion();
                conversion.SetInputFrameRate(fps);
                conversion.BuildVideoFromImages(files);
                conversion.SetFrameRate(fps);
                conversion.SetPixelFormat(PixelFormat.rgb24);
                conversion.SetOutput(fileName);

                await conversion.Start();


                //var imageInfos = Directory.GetFiles(Path.Combine(basePath)).ToList().OrderBy(i => i).Select(i => ImageInfo.FromPath(i)).ToArray();
                //FFMpeg.JoinImageSequence(fileName, frameRate: 30, imageInfos);

                await Context.Channel.SendFileAsync(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating movie");
                await Context.Channel.SendMessageAsync(ex.ToString());
            }
        }
    }
}
