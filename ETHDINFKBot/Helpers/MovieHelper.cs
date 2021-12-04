using Discord;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data.Discord;
using ETHDINFKBot.Classes;
using ETHDINFKBot.Drawing;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace ETHDINFKBot.Helpers
{
    public static class MovieHelper
    {
        public static (string BasePath, string BaseOutputPath) CleanAndCreateMovieFolders()
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

        public static async Task SaveToDisk(string basePath, int i, SKBitmap bitmap, SKCanvas canvas)
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

        private static IEnumerable<IGrouping<DateTimeOffset, GraphEntryInfo>> GroupGraphEntryInfoBy(int groupByHours, int groupByMins, List<GraphEntryInfo> messageInfos)
        {
            // https://stackoverflow.com/questions/47763874/how-to-linq-query-group-by-2-hours-interval
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

        public static async Task<string> GenerateMovieForEmotes(ulong guildId)
        {
            var emotes = Program.Client.GetGuild(guildId).Emotes;

            var guildEmoteIds = emotes.Select(x => x.Id);


            var dbManager = DatabaseManager.Instance();

            var emoteHistoryList = dbManager.GetEmoteHistoryUsage(DateTime.Now.AddDays(-7), DateTime.Now);

            var reactions = emoteHistoryList.Where(i => i.IsReaction);
            var textEmotes = emoteHistoryList.Where(i => !i.IsReaction);

            var groupedReactions = reactions.GroupBy(i => i.DiscordEmoteId).ToDictionary(g => g.Key, g => g.Select(i => i.Count).Sum()).OrderByDescending(i => i.Value); // sum to also get reaction removed
            //var groupedTextEmotes = textEmotes.GroupBy(i => i.DiscordEmoteId).ToDictionary(g => g.Key, g => g.Count()).OrderByDescending(i => i.Value);

            List<GraphEntryInfo> graphEntryInfos = new List<GraphEntryInfo>();

            var reactions2 = emoteHistoryList.Where(i => i.IsReaction && guildEmoteIds.Contains(i.DiscordEmoteId)).Select(i => new GraphEntryInfo() { KeyId = i.DiscordEmoteId, DateTime = new DateTimeOffset(i.DateTimePosted) }).ToList();

            /*foreach (var reactionEmote in groupedReactions)
            {
                // Ignore non guild emotes
                if (!guildEmoteIds.Contains(reactionEmote.Key))
                    continue;
            }*/

            var groups = GroupGraphEntryInfoBy(1, -1, reactions2);
            var keys = groups.Select(x => x.Key);

            var parsedMessageInfos = GetEmoteParsedMessageInfos(groups, emotes);

            (string basePath, string baseOutputPath) = CleanAndCreateMovieFolders();

            if (true)
                StackResults(parsedMessageInfos);

            try
            {
                await GenerateFrames(keys, parsedMessageInfos, basePath, true);
                string fileName = await RunFFMpeg(basePath, baseOutputPath, 30);

                return fileName;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error while creating movie");
                return null;
            }
        }

        public static async Task<string> GenerateMovieForMessages(ulong guildId, int hoursAmount = -1, int fps = 30, int groupByHours = 24, int groupByMinutes = -1, bool stacked = true, bool drawDots = true, string filePrefix = "", params ulong[] channelIds)
        {
            Stopwatch watch = new Stopwatch();

            watch.Start();
            var messageInfos = GetMessageInfos(hoursAmount < 0 ? null : SnowflakeUtils.ToSnowflake(DateTimeOffset.Now.AddHours(hoursAmount * (-1))));
            watch.Stop();

            //await Context.Channel.SendMessageAsync($"Retreived data in {watch.ElapsedMilliseconds}ms");

            var groups = GroupGraphEntryInfoBy(groupByHours, groupByMinutes, messageInfos);
            var keys = groups.Select(x => x.Key);

            var channels = DatabaseManager.Instance().GetDiscordAllChannels(guildId);

            //await Context.Channel.SendMessageAsync($"Total frames {groups.Count()}");

            var parsedMessageInfos = GetChannelParsedMessageInfos(groups, channels, channelIds);

            (string basePath, string baseOutputPath) = CleanAndCreateMovieFolders();

            if (stacked)
                StackResults(parsedMessageInfos);

            try
            {
                await GenerateFrames(keys, parsedMessageInfos, basePath, drawDots);
                string fileName = await RunFFMpeg(basePath, baseOutputPath, fps, filePrefix);

                return fileName;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error while creating movie");
                return null;
            }
        }

        private static void StackResults(List<ParsedGraphInfo> parsedMessageInfos)
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

        private static int GetMaxValue(List<ParsedGraphInfo> parsedMessageInfos, DateTimeOffset until)
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

        private static async Task<string> RunFFMpeg(string basePath, string baseOutputPath, int fps, string filePrefix = "")
        {
            int random = new Random().Next(1_000);
            //GlobalFFOptions.Configure(options => options.BinaryFolder = Program.Settings.FFMpegPath);
            Xabe.FFmpeg.FFmpeg.SetExecutablesPath(Program.Settings.FFMpegPath);

            var files = Directory.GetFiles(Path.Combine(basePath)).ToList().OrderBy(i => i);
            string fileName = Path.Combine(baseOutputPath, $"movie_{random}.mp4");

            if (!string.IsNullOrWhiteSpace(filePrefix))
                fileName = Path.Combine(baseOutputPath, $"{filePrefix}_{random}.mp4");

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
        private static List<GraphEntryInfo> GetMessageInfos(ulong? fromSnowflakeId = null)
        {
            List<GraphEntryInfo> messageTimes = new List<GraphEntryInfo>();
            using (ETHBotDBContext context = new ETHBotDBContext())
                messageTimes = context.DiscordMessages.AsQueryable()
                    .Where(i => fromSnowflakeId == null || (fromSnowflakeId != null && i.DiscordMessageId > fromSnowflakeId))
                    .Select(i => new GraphEntryInfo() { KeyId = i.DiscordChannelId, DateTime = SnowflakeUtils.FromSnowflake(i.DiscordMessageId) })
                    .ToList();

            return messageTimes;
        }

        private static async Task<bool> GenerateFrames(IEnumerable<DateTimeOffset> keys, List<ParsedGraphInfo> parsedMessageInfos, string basePath, bool drawDots = true)
        {
            List<Task> tasks = new List<Task>();

            for (int i = 2; i <= keys.Count(); i++)
            {
                //if (i % 250 == 0)
                //    await Context.Channel.SendMessageAsync($"Frame gen {i} out of {keys.Count()}");

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

                    if (dataPointList.Count > 0)
                        highestPoint = dataPointList.Min(i => i.Y);

                    // TODO Do better label name
                    var drawLineInfo = DrawingHelper.DrawLine(drawInfo.Canvas, drawInfo.Bitmap, dataPointList, new SKPaint() { Color = item.Color }, 6, "#" + item.GetName(), rowIndex, xOffset, drawDots, highestPoint, item.Image); //new Pen(System.Drawing.Color.LightGreen)

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

        private static List<ParsedGraphInfo> GetChannelParsedMessageInfos(IEnumerable<IGrouping<DateTimeOffset, GraphEntryInfo>> groups, List<DiscordChannel> channels, params ulong[] channelIds)
        {
            List<ParsedGraphInfo> parsedMessageInfos = new List<ParsedGraphInfo>();

            foreach (var item in groups)
            {
                foreach (var value in item)
                {
                    if (!parsedMessageInfos.Any(i => i.ChannelId == value.KeyId))
                    {
                        var channelDB = channels.SingleOrDefault(i => i.DiscordChannelId == value.KeyId);
                        if (channelDB == null)
                            continue;

                        // ingore this channel
                        if (channelIds.Length > 0 && !channelIds.Contains(value.KeyId))
                            continue;

                        parsedMessageInfos.Add(new ParsedGraphInfo()
                        {
                            ChannelId = value.KeyId,
                            Info = new Dictionary<DateTimeOffset, int>(),
                            ChannelName = channelDB.ChannelName,
                            Color = new SKColor((byte)new Random().Next(0, 255), (byte)new Random().Next(0, 255), (byte)new Random().Next(0, 255))
                        });
                    }

                    var channelInfo = parsedMessageInfos.Single(i => i.ChannelId == value.KeyId);
                    if (channelInfo.Info.ContainsKey(value.DateTime))
                        channelInfo.Info[value.DateTime] += 1;
                    else
                        channelInfo.Info.Add(value.DateTime, 1);
                }
            }

            return parsedMessageInfos;
        }

        private static List<ParsedGraphInfo> GetEmoteParsedMessageInfos(IEnumerable<IGrouping<DateTimeOffset, GraphEntryInfo>> groups, IReadOnlyCollection<GuildEmote> emotes)
        {
            List<ParsedGraphInfo> parsedMessageInfos = new List<ParsedGraphInfo>();

            foreach (var item in groups)
            {
                foreach (var value in item)
                {
                    if (!parsedMessageInfos.Any(i => i.DiscordEmoteId == value.KeyId))
                    {
                        var guildEmote = emotes.SingleOrDefault(i => i.Id == value.KeyId);
                        if (guildEmote == null)
                            continue;

                        parsedMessageInfos.Add(new ParsedGraphInfo()
                        {
                            DiscordEmoteId = value.KeyId,
                            Info = new Dictionary<DateTimeOffset, int>(),
                            DiscordEmoteName = guildEmote.Name,
                            Color = new SKColor((byte)new Random().Next(0, 255), (byte)new Random().Next(0, 255), (byte)new Random().Next(0, 255)),
                            Image = DownloadBitmap(guildEmote.Url)
                        });
                    }

                    var emoteInfo = parsedMessageInfos.Single(i => i.DiscordEmoteId == value.KeyId);
                    if (emoteInfo.Info.ContainsKey(value.DateTime))
                        emoteInfo.Info[value.DateTime] += 1;
                    else
                        emoteInfo.Info.Add(value.DateTime, 1);
                }
            }

            return parsedMessageInfos;
        }

        private static SKBitmap DownloadBitmap(string url)
        {
            HttpWebResponse response = null;
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "HEAD";
            request.Timeout = 2000; // miliseconds

            try
            {
                response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK) //Make sure the URL is not empty and the image is there
                {
                    // download the bytes
                    byte[] stream = null;
                    using (var webClient = new WebClient())
                    {
                        stream = webClient.DownloadData(url);
                    }

                    // decode the bitmap stream
                    return SKBitmap.Decode(stream).Resize(new SKSizeI(24, 24), SKFilterQuality.High); ;
                    /*
                    if (resourceBitmap != null)
                    {
                        var resizedBitmap = resourceBitmap.Resize(info, SKFilterQuality.High); //Resize to the canvas
                        canvas.DrawBitmap(resizedBitmap, 0, 0);
                    }*/
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                // Don't forget to close your response.
                if (response != null)
                {
                    response.Close();
                }
            }

            return null;
        }
    }
}
