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
using ETHBot.DataLayer.Data.Discord;
using ImageMagick;

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

        private (string BasePath, string BaseOutputPath) CleanAndCreateFolders()
        {
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

            return (basePath, baseOutputPath);
        }

        private void StackResults(List<ParsedMessageInfo> parsedMessageInfos)
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

        private int GetMaxValue(List<ParsedMessageInfo> parsedMessageInfos, DateTimeOffset until)
        {
            int maxY = 0;

            // TODO calculate the rolling info

            foreach (var item in parsedMessageInfos)
            {
                foreach (var info in item.Info)
                {
                    // only consider until this key
                    if (info.Key > until)
                        continue;

                    if (maxY < info.Value)
                        maxY = info.Value;
                }
            }

            return maxY;
        }

        private async Task<string> RunFFMpeg(string basePath, string baseOutputPath, int fps)
        {
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

            return fileName;
        }

        // TODO add channel filtering
        private List<MessageInfo> GetMessageInfos(ulong? fromSnowflakeId = null)
        {
            List<MessageInfo> messageTimes = new List<MessageInfo>();
            using (ETHBotDBContext context = new ETHBotDBContext())
                messageTimes = context.DiscordMessages.AsQueryable().Where(i => fromSnowflakeId == null || (fromSnowflakeId != null && i.DiscordMessageId > fromSnowflakeId)).Select(i => new MessageInfo() { ChannelId = i.DiscordChannelId, DateTime = SnowflakeUtils.FromSnowflake(i.DiscordMessageId) }).ToList();

            return messageTimes;
        }

        private async Task<bool> GenerateFrames(IEnumerable<DateTimeOffset> keys, List<ParsedMessageInfo> parsedMessageInfos, string basePath, bool drawDots = true)
        {
            List<Task> tasks = new List<Task>();

            for (int i = 2; i <= keys.Count(); i++)
            {
                if (i % 250 == 0)
                    await Context.Channel.SendMessageAsync($"Frame gen {i} out of {keys.Count()}");

                var startTime = keys.Take(i).Min();
                var endTime = keys.Take(i).Max();

                int maxY = GetMaxValue(parsedMessageInfos, endTime);

                var drawInfo = DrawingHelper.GetEmptyGraphics();
                var padding = DrawingHelper.DefaultPadding;

                padding.Left = 100; // large numbers
                padding.Bottom = 150; // possible for many labels
                padding.Right = 150; // add labels for max height

                var labels = DrawingHelper.GetLabels(startTime.DateTime, endTime.DateTime, 0, maxY, 6, 10, " msg");

                var gridSize = new GridSize(drawInfo.Bitmap, padding);

                DrawingHelper.DrawGrid(drawInfo.Canvas, gridSize, padding, labels.XAxisLables, labels.YAxisLabels, $"Messages count");

                int xOffset = 0;
                int rowIndex = -4; // workaround to use as many label space as possible

                foreach (var item in parsedMessageInfos)
                {
                    // TODO optimize some lines + move to draw helper
                    var dataPoints = item.Info.Where(j => j.Key <= endTime).OrderBy(i => i.Key).ToDictionary(j => j.Key.DateTime, j => j.Value);

                    // todo add 2. y Axis on the right
                    var dataPointList = DrawingHelper.GetPoints(dataPoints, gridSize, true, startTime.DateTime, endTime.DateTime, false, maxY);

                    var highestPoint = -1f;

                    if (dataPoints.Count > 0)
                        highestPoint = dataPointList.Min(i => i.Y);

                    var drawLineInfo = DrawingHelper.DrawLine(drawInfo.Canvas, drawInfo.Bitmap, dataPointList, new SKPaint() { Color = item.Color }, 6, "#" + item.ChannelName, rowIndex, xOffset, drawDots, highestPoint); //new Pen(System.Drawing.Color.LightGreen)

                    if (drawLineInfo.newRow)
                    {
                        rowIndex++;
                        xOffset = drawLineInfo.usedWidth;
                    }
                    else
                    {
                        xOffset += drawLineInfo.usedWidth;
                    }

                }

                tasks.Add(SaveToDisk(basePath, i, drawInfo.Bitmap, drawInfo.Canvas));
            }


            Task.WaitAll(tasks.ToArray());

            return true;
        }


        private List<ParsedMessageInfo> GetParsedMessageInfos(IEnumerable<IGrouping<DateTimeOffset, MessageInfo>> groups, List<DiscordChannel> channels, params ulong[] channelIds)
        {
            List<ParsedMessageInfo> parsedMessageInfos = new List<ParsedMessageInfo>();

            foreach (var item in groups)
            {
                foreach (var value in item)
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


            return parsedMessageInfos;
        }

        [Command("movie", RunMode = RunMode.Async)]
        public async Task CreateMovie(bool stacked, int groupByHours, int fps, bool drawDots, params ulong[] channelIds)
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }


            Stopwatch watch = new Stopwatch();
            watch.Start();

            var messageInfos = GetMessageInfos();

            watch.Stop();

            await Context.Channel.SendMessageAsync($"Retreived data in {watch.ElapsedMilliseconds}ms");

            // https://stackoverflow.com/questions/47763874/how-to-linq-query-group-by-2-hours-interval

            var groups = GroupMessageInfoBy(groupByHours, -1, messageInfos);
            var keys = groups.Select(x => x.Key);

            var channels = DatabaseManager.Instance().GetDiscordAllChannels(Context.Guild.Id);

            await Context.Channel.SendMessageAsync($"Total frames {groups.Count()}");

            var parsedMessageInfos = GetParsedMessageInfos(groups, channels, channelIds);

            (string basePath, string baseOutputPath) = CleanAndCreateFolders();

            if (stacked)
                StackResults(parsedMessageInfos);

            try
            {
                await GenerateFrames(keys, parsedMessageInfos, basePath, drawDots);

                await Context.Channel.SendMessageAsync("Saving finished. Starting FFMpeg");


                string fileName = await RunFFMpeg(basePath, baseOutputPath, fps);
                await Context.Channel.SendFileAsync(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating movie");
                await Context.Channel.SendMessageAsync(ex.ToString());
            }
        }

        private IEnumerable<IGrouping<DateTimeOffset, MessageInfo>> GroupMessageInfoBy(int groupByHours, int groupByMins, List<MessageInfo> messageInfos)
        {
            if (groupByHours > 0)
            {
                // Group by hours
                return messageInfos.GroupBy(x =>
                {
                    var stamp = x;
                    stamp.DateTime = stamp.DateTime.AddHours(-(stamp.DateTime.Hour % groupByHours));
                    stamp.DateTime = stamp.DateTime.AddMinutes(-(stamp.DateTime.Minute));
                    stamp.DateTime = stamp.DateTime.AddMilliseconds(-stamp.DateTime.Millisecond - 1000 * stamp.DateTime.Second);
                    return stamp.DateTime;
                });
            }
            else if (groupByMins > 0)
            {
                // Group by mins
                return messageInfos.GroupBy(x =>
                {
                    var stamp = x;
                    stamp.DateTime = stamp.DateTime.AddMinutes(-(stamp.DateTime.Minute % groupByMins));
                    stamp.DateTime = stamp.DateTime.AddMilliseconds(-stamp.DateTime.Millisecond - 1000 * stamp.DateTime.Second);
                    return stamp.DateTime;
                });
            }

            return null; // Invalid input
        }

        [Command("movietoday", RunMode = RunMode.Async)]
        public async Task CreateMovieToday()
        {
            bool stacked = true;
            bool drawDots = true;
            int fps = 30;

            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }


            Stopwatch watch = new Stopwatch();
            watch.Start();

            // only today
            var messageInfos = GetMessageInfos(SnowflakeUtils.ToSnowflake(new DateTimeOffset(DateTime.Now.Date)));

            watch.Stop();

            await Context.Channel.SendMessageAsync($"Retreived data in {watch.ElapsedMilliseconds}ms");

            // https://stackoverflow.com/questions/47763874/how-to-linq-query-group-by-2-hours-interval
            // Group by 2 mins


            var groups = GroupMessageInfoBy(-1, 2, messageInfos);
            var keys = groups.Select(x => x.Key);


            var channels = DatabaseManager.Instance().GetDiscordAllChannels(Context.Guild.Id);

            await Context.Channel.SendMessageAsync($"Total frames {groups.Count()}");

            var parsedMessageInfos = GetParsedMessageInfos(groups, channels);

            (string basePath, string baseOutputPath) = CleanAndCreateFolders();

            if (stacked)
                StackResults(parsedMessageInfos);

            try
            {
                await GenerateFrames(keys, parsedMessageInfos, basePath, drawDots);

                await Context.Channel.SendMessageAsync("Saving finished. Starting FFMpeg");


                string fileName = await RunFFMpeg(basePath, baseOutputPath, fps);
                await Context.Channel.SendFileAsync(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating movie");
                await Context.Channel.SendMessageAsync(ex.ToString());
            }
        }


        private async Task SaveToDisk(string basePath, int i, SKBitmap bitmap, SKCanvas canvas)
        {
            using (var data = bitmap.Encode(SKEncodedImageFormat.Png, 80))
            {
                // save the data to a stream
                using (var stream = File.OpenWrite(Path.Combine(basePath, $"{i.ToString("D8")}.png")))
                    data.SaveTo(stream);
            }

            // Release
            bitmap.Dispose();
            canvas.Dispose();
        }


        [Command("movieplace", RunMode = RunMode.Async)]
        public async Task CreateMoviePlace(bool stacked, int groupByHour, int fps, bool drawDots)
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
        HOUR(PlacedDateTime), 
        DATE(PlacedDateTime), 
        COUNT(*)
    FROM 
        PlaceBoardHistory 
    GROUP BY 
        HOUR(PlacedDateTime),
        DATE(PlacedDateTime)
    ORDER BY
        DATE(PlacedDateTime),
        HOUR(PlacedDateTime)";

            Stopwatch watch = new Stopwatch();

            watch.Start();


            List<MessageInfo> messageTimes = new List<MessageInfo>();



            var queryResult = await SQLHelper.GetQueryResults(null, sqlQuery, true, 10_000_000, true, true);


            Context.Channel.SendMessageAsync($"Retreived data in {watch.ElapsedMilliseconds}ms");


            var parsedInfo = new ParsedMessageInfo()
            {
                ChannelId = 1,
                Info = new Dictionary<DateTimeOffset, int>(),
                ChannelName = "eth-place-bots",
                Color = new SKColor(255, 0, 0)
            };

            var firstDateTime = DateTimeOffset.MaxValue;
            var lastDateTime = DateTimeOffset.MinValue;


            foreach (var item in queryResult.Data)
            {
                int hours = Convert.ToInt32(item[0]);
                DateTime dateTime = Convert.ToDateTime(item[1]);
                int count = Convert.ToInt32(item[2]);

                var key = new DateTimeOffset(dateTime).AddHours(hours);
                parsedInfo.Info.Add(key, count);

                if (firstDateTime > key)
                    firstDateTime = key;
                if (lastDateTime < key)
                    lastDateTime = key;
            }


            double maxFrames = 600;

            int bound = (int)((lastDateTime - firstDateTime).TotalMinutes / maxFrames);

            Context.Channel.SendMessageAsync($"Group by {bound} minutes, Total mins {(lastDateTime - firstDateTime).TotalMinutes}");




            Context.Channel.SendMessageAsync($"Total frames {parsedInfo.Info.Count()}");

            //int val = 0;
            //foreach (var group in groups)
            //{
            //    val += group.Value;
            //    dataPointsSpam.Add(group.TimeStamp.DateTime, val);
            //}

            (string basePath, string baseOutputPath) = CleanAndCreateFolders();


            if (stacked)
            {
                var values = parsedInfo.Info.OrderBy(i => i.Key);
                int val = 0;

                foreach (var info in values)
                {
                    val += info.Value;
                    parsedInfo.Info[info.Key] = val;
                }
            }

            try
            {
                List<Task> tasks = new List<Task>();

                for (int i = 2; i <= parsedInfo.Info.Count; i++)
                {
                    if (i % 250 == 0)
                        Context.Channel.SendMessageAsync($"Frame gen {i} out of {parsedInfo.Info.Count}");

                    var startTime = parsedInfo.Info.Keys.Take(i).Min();
                    var endTime = parsedInfo.Info.Keys.Take(i).Max();

                    int maxY = 0;

                    // TODO calculate the rolling info


                    foreach (var info in parsedInfo.Info)
                    {
                        // only consider until this key
                        if (info.Key > endTime)
                            continue;

                        if (maxY < info.Value)
                            maxY = info.Value;
                    }


                    var drawInfo = DrawingHelper.GetEmptyGraphics();
                    var padding = DrawingHelper.DefaultPadding;

                    padding.Left = 150; // large numbers

                    var labels = DrawingHelper.GetLabels(startTime.DateTime, endTime.DateTime, 0, maxY, 6, 10, " msg");

                    var gridSize = new GridSize(drawInfo.Bitmap, padding);

                    DrawingHelper.DrawGrid(drawInfo.Canvas, gridSize, padding, labels.XAxisLables, labels.YAxisLabels, $"Messages count");

                    // TODO optimize some lines + move to draw helper
                    var dataPoints = parsedInfo.Info.Where(j => j.Key <= endTime).OrderBy(i => i.Key).ToDictionary(j => j.Key.DateTime, j => j.Value);

                    // todo add 2. y Axis on the right
                    var dataPointList = DrawingHelper.GetPoints(dataPoints, gridSize, true, startTime.DateTime, endTime.DateTime, false, maxY);
                    
                    var highestPoint = -1f;

                    if (dataPoints.Count > 0)
                        highestPoint = dataPointList.Min(i => i.Y);

                    DrawingHelper.DrawLine(drawInfo.Canvas, drawInfo.Bitmap, dataPointList, new SKPaint() { Color = parsedInfo.Color }, 6, "#" + parsedInfo.ChannelName, 0, 0, drawDots, highestPoint); //new Pen(System.Drawing.Color.LightGreen)

                    tasks.Add(SaveToDisk(basePath, i, drawInfo.Bitmap, drawInfo.Canvas));
                }

                await Context.Channel.SendMessageAsync("Finished processing, waiting for SaveToDisk");

                Task.WaitAll(tasks.ToArray());

                await Context.Channel.SendMessageAsync("Saving finished. Starting FFMpeg");

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
                conversion.OnProgress += Conversion_OnProgress;

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


        [Command("test", RunMode = RunMode.Async)]
        public async Task Progress()
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            (string basePath, string baseOutputPath) = CleanAndCreateFolders();

            SKRect rect = new SKRect(100, 100, 200, 200);

            var arcPaint = new SKPaint()
            {
                Color = new SKColor(255, 0, 0)
            };

            List<Task> tasks = new List<Task>();

            // draw empty rectabgle
            int frame = 0;

            // Spawn the border
            for (int i = 0; i < 10; i++)
            {
                var drawInfo = DrawingHelper.GetEmptyGraphics(505, 200);


                drawInfo.Canvas.DrawRect(70, 25, 365, 100, new SKPaint() { Color = new SKColor(255, 0, 0, (byte)(20 * i + 30)), IsStroke = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4 });

                tasks.Add(SaveToDisk(basePath, frame, drawInfo.Bitmap, drawInfo.Canvas));
                frame++;
            }

            // Standstill
            for (int i = 0; i < 5; i++)
            {
                var drawInfo = DrawingHelper.GetEmptyGraphics(505, 200);

                drawInfo.Canvas.DrawRect(70, 25, 365, 100, new SKPaint() { Color = new SKColor(255, 0, 0, 255), IsStroke = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4 });

                tasks.Add(SaveToDisk(basePath, frame, drawInfo.Bitmap, drawInfo.Canvas));
                frame++;
            }

            int dayOfTheYear = 60; Math.Min(DateTime.Now.DayOfYear, 365); // dont handle leap years for now TODO
            for (int i = 0; i < dayOfTheYear + 10; i++)
            {
                var drawInfo = DrawingHelper.GetEmptyGraphics(505, 200);

                for (int j = 0; j < Math.Min(i, 10); j++)
                {
                    drawInfo.Canvas.DrawRect(70, 23, Math.Min(i - j, dayOfTheYear), 100, new SKPaint() { Color = new SKColor(0, 255, 0, 25), Style = SKPaintStyle.Fill });

                    if (j == i)
                    {
                        break;
                    }
                }

                // border
                drawInfo.Canvas.DrawRect(70, 25, 365, 100, new SKPaint() { Color = new SKColor(255, 0, 0, 255), IsStroke = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4 });

                tasks.Add(SaveToDisk(basePath, frame, drawInfo.Bitmap, drawInfo.Canvas));
                frame++;
            }


            /*            for (int i = 0; i < 180; i++)
                {
                    var drawInfo = DrawingHelper.GetEmptyGraphics(500, 500);

                    using (SKPath path = new SKPath())
                    {
                        path.AddArc(rect, 0, i);
                        drawInfo.Canvas.DrawPath(path, arcPaint);

                        tasks.Add(SaveToDisk(basePath, i, drawInfo.Bitmap, drawInfo.Canvas));
                    }
                }*/

            int random = new Random().Next(1_000_000_000);

            var files = Directory.GetFiles(Path.Combine(basePath)).ToList().OrderBy(i => i);
            string fileName = Path.Combine(baseOutputPath, $"movie_{random}.gif");

            try
            {
                using (var collection = new MagickImageCollection())
                {
                    int i = 0;
                    foreach (var file in files)
                    {
                        collection.Add(file);
                        collection[i].AnimationDelay = 1; // in this example delay is 1000ms/1sec

                        if (i == 0)
                            collection[i].AnimationIterations = 0;

                        //collection[i].Flip();
                        i++;
                    }

                    // Optionally reduce colors
                    var settings = new QuantizeSettings();

                    //settings.Colors = 256;
                    //collection.Quantize(settings);

                    // Optionally optimize the images (images should have the same size).
                    //collection.Optimize();

                    // Save gif
                    collection.Write(fileName, MagickFormat.Gif);
                }
            }
            catch (Exception ex)
            {

            }

            await Context.Channel.SendFileAsync(fileName);
        }
        private void Conversion_OnProgress(object sender, Xabe.FFmpeg.Events.ConversionProgressEventArgs args)
        {
            if (args.Percent % 20 == 0)
                Context?.Channel?.SendMessageAsync(args.Percent + "% done.");
        }
    }
}
