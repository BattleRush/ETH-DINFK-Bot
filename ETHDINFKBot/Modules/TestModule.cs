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
using ETHDINFKBot.Classes;

namespace ETHDINFKBot.Modules
{
    [Group("test")]
    public class TestModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger _logger = new Logger<TestModule>(Program.Logger);

        [Command("movie", RunMode = RunMode.Async)]
        public async Task CreateMovie(bool stacked, int groupByHours, int fps, bool drawDots, params ulong[] channelIds)
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            try
            {
                string fileName = await MovieHelper.GenerateMovieForMessages(Context.Guild.Id, -1, fps, groupByHours, -1, stacked, drawDots, "", channelIds);
                await Context.Channel.SendFileAsync(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating movie");
                await Context.Channel.SendMessageAsync(ex.ToString());
            }
        }

        [Command("movietoday", RunMode = RunMode.Async)]
        public async Task CreateMovieToday()
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            try
            {
                string fileName = await MovieHelper.GenerateMovieForMessages(Context.Guild.Id, 24, 30, -1, 2, true, true);
                await Context.Channel.SendFileAsync(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating movie");
                await Context.Channel.SendMessageAsync(ex.ToString());
            }
        }

        [Command("movieweek", RunMode = RunMode.Async)]
        public async Task CreateMovieLastWeek()
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                await Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            try
            {
                string fileName = await MovieHelper.GenerateMovieForMessages(Context.Guild.Id, 24 * 7, 30, -1, 15, true, true);
                await Context.Channel.SendFileAsync(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating movie");
                await Context.Channel.SendMessageAsync(ex.ToString());
            }
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

            (string basePath, string baseOutputPath) = MovieHelper.CleanAndCreateMovieFolders();


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

                    tasks.Add(MovieHelper.SaveToDisk(basePath, i, drawInfo.Bitmap, drawInfo.Canvas));
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

            (string basePath, string baseOutputPath) = MovieHelper.CleanAndCreateMovieFolders();

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

                tasks.Add(MovieHelper.SaveToDisk(basePath, frame, drawInfo.Bitmap, drawInfo.Canvas));
                frame++;
            }

            // Standstill
            for (int i = 0; i < 5; i++)
            {
                var drawInfo = DrawingHelper.GetEmptyGraphics(505, 200);

                drawInfo.Canvas.DrawRect(70, 25, 365, 100, new SKPaint() { Color = new SKColor(255, 0, 0, 255), IsStroke = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4 });

                tasks.Add(MovieHelper.SaveToDisk(basePath, frame, drawInfo.Bitmap, drawInfo.Canvas));
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

                tasks.Add(MovieHelper.SaveToDisk(basePath, frame, drawInfo.Bitmap, drawInfo.Canvas));
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
