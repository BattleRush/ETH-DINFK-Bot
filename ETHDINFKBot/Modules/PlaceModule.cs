using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ETHBot.DataLayer.Data.Fun;
using ETHDINFKBot.Data;

using ETHDINFKBot.Drawing;
using ETHDINFKBot.Enums;
using ETHDINFKBot.Helpers;
using FFMediaToolkit;
using FFMediaToolkit.Encoding;
using FFMediaToolkit.Graphics;
//using ImageMagick;
using Newtonsoft.Json;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETHDINFKBot.Modules
{
    // TODO move to other files
    public class PlacePixel
    {
        public PlacePixel()
        {
            Placements = new Dictionary<ulong, ulong>();
        }

        public short XPos { get; set; }

        public short YPos { get; set; }

        // 3 bytes
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public Dictionary<ulong, ulong> Placements { get; set; }
        public int PlacementCount { get { return Placements.Count; } }

        public byte Intensity(ulong fromTime)
        {
            var lastPixelTime = SnowflakeUtils.FromSnowflake(Placements.Keys.Last()); // todo ensure this is the last one
            var currentFrameTime = SnowflakeUtils.FromSnowflake(fromTime);


            double totalSecsTracked = 10 * 60; // 10 mins

            double totalSeconds = (currentFrameTime - lastPixelTime).TotalSeconds;
            totalSeconds = totalSecsTracked - totalSeconds;

            if (totalSeconds < 0)
                return 0;

            byte intensity = (byte)((totalSeconds / totalSecsTracked) * 255);


            return intensity;
        }
    }

    public class PlaceBoard
    {
        public PlaceBoard()
        {
            Pixels = new List<PlacePixel>();
        }
        public List<PlacePixel> Pixels { get; set; }

        public void AddPixel(PlaceBoardHistory pixel)
        {
            var listPixel = Pixels.AsQueryable().SingleOrDefault(i => i.XPos == pixel.XPos && i.YPos == pixel.YPos);
            if (listPixel == null)
            {
                var newPixel = new PlacePixel()
                {
                    XPos = pixel.XPos,
                    YPos = pixel.YPos,
                    R = pixel.R,
                    G = pixel.G,
                    B = pixel.B
                };

                //newPixel.Placements.Add(pixel.SnowflakeTimePlaced, pixel.PlaceDiscordUserId);
                Pixels.Add(newPixel);
            }
            else
            {
                listPixel.R = pixel.R;
                listPixel.G = pixel.G;
                listPixel.B = pixel.B;
                //listPixel.Placements.Add(pixel.SnowflakeTimePlaced, pixel.PlaceDiscordUserId);
            }
        }
    }


    [Group("place")]
    public class PlaceModule : ModuleBase<SocketCommandContext>
    {
        //public static List<PlaceBoardPixel> PixelsCache = new List<PlaceBoardPixel>();
        public static DateTime LastRefresh = DateTime.MinValue;

        public static bool? LockedBoard = null;

        public static SKBitmap CurrentPlaceBitmap;
        public static int LastYRefreshed = 0;

        private bool IsPlaceLocked()
        {
            if (CurrentPlaceBitmap == null)
            {

                var board = DrawingHelper.GetEmptyGraphics(1000, 1000);
                CurrentPlaceBitmap = board.Bitmap;

                // load the entire image on startup
                for (int i = 0; i < 10; i++)
                {
                    RefreshBoard(100);
                    Thread.Sleep(100);
                }
            }

            if (LockedBoard == null)
            {
                PlaceDBManager dbManager = PlaceDBManager.Instance();
                LockedBoard = dbManager.GetBoardStatus();
            }

            return LockedBoard.Value;
        }

        // Bitmap -> ImageData (safe)

        // SYSTEM.DRAWING -> TODO EVEN NEEDED?
        /*
        public static ImageData ToImageData(Bitmap bitmap)
        {
            var rect = new System.Drawing.Rectangle(System.Drawing.Point.Empty, bitmap.Size);
            var bitLock = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            var bitmapData = ImageData.FromPointer(bitLock.Scan0, ImagePixelFormat.Bgr24, bitmap.Size);
            bitmap.UnlockBits(bitLock);
            return bitmapData;
        }

        public Bitmap CopyAndDrawOnBitmap(Bitmap bitmap, string message, int x, int y, System.Drawing.Size size)
        {
            int padding = 50;
            // todo dont do that per each call
            System.Drawing.Image i = System.Drawing.Image.FromFile(Path.Combine("Images", "BattleRush.png")); // This is 300x300

            System.Drawing.Image dinfk = System.Drawing.Image.FromFile(Path.Combine("Images", "DINFK.png"));

            Bitmap cloneBitmap = bitmap.Clone(new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), size), bitmap.PixelFormat);

            Graphics g = Graphics.FromImage(cloneBitmap);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawString(message, DrawingHelper.NormalTextFont, DrawingHelper.SolidBrush_White, new System.Drawing.Point(x, y));

            g.DrawString("By BattleRush", DrawingHelper.NormalTextFont, DrawingHelper.SolidBrush_White, new System.Drawing.Point(size.Width - 150, y));
            g.DrawImage(i, size.Width - padding, size.Height - padding, padding, padding);

            g.DrawString("ETH DINFK Discord Server", DrawingHelper.NormalTextFont, DrawingHelper.SolidBrush_White, new System.Drawing.Point(size.Width - 450, y));
            g.DrawImage(dinfk, size.Width - padding - 200, size.Height - padding, padding, padding);


            g.DrawString("DEV ONLY. WILL BE RESET WHEN GOING LIVE", DrawingHelper.TitleFont, new SolidBrush(System.Drawing.Color.Red), new System.Drawing.Point(size.Width - 775, y + 20));

            g.Flush();

            return cloneBitmap;
        }
        */

        private static bool LoadedLib = false; // TODO Do better

        // Redesign it from not being public static and doing it properly
        public static List<PlaceDiscordUser> PlaceDiscordUsers = new List<PlaceDiscordUser>();

        [Command("usertest")]
        public async Task UserTest()
        {
            PlaceDBManager dbManager = PlaceDBManager.Instance();

            PlaceDiscordUsers = dbManager.GetPlaceDiscordUsers();

            string text = "";
            foreach (var item in PlaceDiscordUsers)
            {
                text += $"{item.PlaceDiscordUserId}|{item.DiscordUserId}" + Environment.NewLine;

                if (text.Length > 1900)
                {
                    await Context.Channel.SendMessageAsync(text, false);
                    text = "";
                }
            }

            await Context.Channel.SendMessageAsync(text, false);
        }

        // TODO DUPLICATE Func and to be removed
        [Command("genchunk")]
        public async Task GenerateChunks()
        {
            return;
            /*
            Stopwatch watch = new Stopwatch();
            watch.Start();

            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                Context.Channel.SendMessageAsync("You aren't allowed to run this command.", false);
                return;
            }

            var chunkFolder = Path.Combine(Program.BasePath, "TimelapseChunks");

            if (!Directory.Exists(chunkFolder))
                Directory.CreateDirectory(chunkFolder);


            PlaceDBManager dbManager = PlaceDBManager.Instance();

            PlaceDiscordUsers = dbManager.GetPlaceDiscordUsers();

            int size = 100_000;

            var totalPixelsPlaced = dbManager.GetBoardHistoryCount();
            await Context.Channel.SendMessageAsync($"Total pixels to load {totalPixelsPlaced.ToString("N0")}", false);

            short chunkId = 0;

            for (int i = 0; i < totalPixelsPlaced; i += size)
            {
                chunkId++;

                // check if this chunk is fully done yet
                if (i + size > totalPixelsPlaced)
                    break;

                string file = $"Chunk_{chunkId}.dat";
                string filePath = Path.Combine(chunkFolder, file);

                if (File.Exists(filePath))
                    continue; // this chunk has been generated to disk already

                byte[] data = new byte[3 + size * 12];
                data[0] = (byte)MessageEnum.GetChunk_Response; // id of response

                // Chunk identifier
                byte[] chunkIdBytes = BitConverter.GetBytes(chunkId);
                data[1] = chunkIdBytes[0];
                data[2] = chunkIdBytes[1];

                // 1 entry 12 bytes -> chunk size = 1.2MB

                // Repeating rel pos
                // 0-3 | ID (int32)
                // 4-5 | XPos (int16)
                // 6-7 | XPos (int16)
                // 8 | R color (byte)
                // 9 | G color (byte)
                // 10 | B color (byte)
                // 11 | UserId (byte) 

                // TODO Optimizations
                // Store x/y in 10 bits each (-1.5 bytes)
                // do aux table for users and store them in 1 byte instead of 8 bytes (-7 bytes)
                // add custom timestamp (in seconds to save even more space) (+3/4 bytes)

                int counter = 3;

                var pixelHistory = dbManager.GetBoardHistory(i, size);

                foreach (var item in pixelHistory)
                {
                    byte[] idBytes = BitConverter.GetBytes(item.PlaceBoardHistoryId);

                    byte[] xBytes = BitConverter.GetBytes(item.XPos);
                    byte[] yBytes = BitConverter.GetBytes(item.YPos);

                    data[counter] = idBytes[0];
                    data[counter + 1] = idBytes[1];
                    data[counter + 2] = idBytes[2];
                    data[counter + 3] = idBytes[3];
                    counter += 4;

                    data[counter] = xBytes[0];
                    data[counter + 1] = xBytes[1];
                    data[counter + 2] = yBytes[0];
                    data[counter + 3] = yBytes[1];
                    counter += 4;

                    data[counter] = item.R;
                    data[counter + 1] = item.G;
                    data[counter + 2] = item.B;
                    counter += 3;

                    // get user id (limited currently to 255)
                    data[counter] = Convert.ToByte(item.PlaceDiscordUserId);

                    counter += 1;
                }


                await Context.Channel.SendMessageAsync($"Saved {file}", false);
                File.WriteAllBytes(filePath, data);
            }

            watch.Stop();

            await Context.Channel.SendMessageAsync($"Done. Timelapse has been updated automatically in {watch.ElapsedMilliseconds}ms", false);

            */
        }

        [Command("help")]
        public async Task Help()
        {
            EmbedBuilder builder = new EmbedBuilder();

            string prefix = Program.CurrentPrefix;


            int g = 0;
#if DEBUG
            g = 192;
#endif

            builder.WithTitle("ETH DINFK Place");
            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");
            builder.WithDescription(@"Rules: There are none. However everything that is forbidden by the serverrules, is also forbidden to 'draw' on the board.

If you violate the server rules your pixels will be removed.
**LIVE Website: http://ethplace.spclr.ch:81/**");
            builder.WithColor(g, 255, 0);

            //builder.WithThumbnailUrl("https://avatars0.githubusercontent.com/u/11750584");

            var ownerUser = Program.Client.GetUser(Program.Owner);

            // TODO move admin commands to the admin module
            builder.AddField("Admin ONLY", $"```{prefix}place lock <true|false>" + Environment.NewLine +
                $"{prefix}place remove <userId> <x> <y> <xSize> <ySize> [<minutes>|1440]```");

            builder.AddField("Pixel verify (sends a 100x100 image for pixel verification) (45 sec cooldown)", $"```{prefix}place pixelverify <x> <y>```");
            builder.AddField("Timelapse (Web View only)", "https://place.battlerush.dev/");
            builder.AddField("View full board (May contain outdated cache status)", $"```{prefix}place view [(admin only) <force_load>]```");
            builder.AddField("Pixel history of a pixel/all", $"```{prefix}place history <x> <y>" + Environment.NewLine +
                $"{prefix}place history all```");
            builder.AddField("Zoom on a section (always up to date)", $"```{prefix}place zoom <x> <y> <size>```");
            builder.AddField("Grid (help for navigation", $"```{prefix}place grid" + Environment.NewLine +
                $"{prefix}place grid <x> <y> <size>```");

            builder.AddField("Set single pixel", $"```{prefix}place setpixel <x> <y> #<hex_color>```");
            builder.AddField("Multipixel (user only) Min: 10 Max: 86'400", $"```{prefix}place multipixel {{<x> <y> #<hex_color>[|]}} " + Environment.NewLine +
                $"{prefix}place viewmultipixel```");

            builder.AddField("View place performance", $"```{prefix}place perf [<graph_mode>] [<last_records_amount>]```");

            builder.WithThumbnailUrl(ownerUser.GetAvatarUrl());
            builder.WithAuthor(ownerUser);
            builder.WithCurrentTimestamp();

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }


        [Command("lock")]
        public async Task LockPlace(bool lockPlace)
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                Context.Channel.SendMessageAsync("You aren't allowed to run this command.", false);
                return;
            }

            PlaceDBManager dbManager = PlaceDBManager.Instance();
            LockedBoard = dbManager.SetBoardStatus(lockPlace);

            if (LockedBoard.Value)
                await Program.Client.SetGameAsync($" a locked place", null, ActivityType.Watching);
            else
                await Program.Client.SetGameAsync($" an active place", null, ActivityType.Watching);

            Context.Channel.SendMessageAsync($"Set lock to {LockedBoard}", false);
        }


        [Command("remove")]
        public async Task RemovePixels(ulong discordUserId, int x, int y, int xSize, int ySize, int mins = 1440)
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                Context.Channel.SendMessageAsync("You really tought you could run this huh?", false);
                return;
            }

            PlaceDBManager dbManager = PlaceDBManager.Instance();

            var boardPixels = dbManager.RemovePixels(discordUserId, mins, x, x + xSize, y, y + ySize);

            Context.Channel.SendMessageAsync($"Found {boardPixels} by {discordUserId} in the last {mins} which have been deleted", false);
        }


        public static Dictionary<ulong, DateTimeOffset> PixelVerifyRequest = new Dictionary<ulong, DateTimeOffset>();

        [Command("pixelverify")]
        public async Task PixelVerify(int x, int y)
        {
            int size = 100;

            // check if out of bounds
            if (x < 0 || y < 0 || x + size > 1000 || y + size > 1000)
            {
                await Context.Channel.SendMessageAsync($"PIXELVERIFY {x} {y} FAILED");
                return;
            }

            var blockUntil = DateTime.UtcNow.AddSeconds(45); // 1 per minute

            ulong userId = Context.Message.Author.Id;

            if (PixelVerifyRequest.ContainsKey(userId) && PixelVerifyRequest[userId] > DateTime.UtcNow)
            {
                //if (new Random().Next(0, 25) % 25 == 0)
                Context.Channel.SendMessageAsync($"Still in timeout");

                return;
            }

            if (PixelVerifyRequest.ContainsKey(userId))
                PixelVerifyRequest[userId] = blockUntil;
            else
                PixelVerifyRequest.Add(userId, blockUntil);

            Stopwatch watch = new Stopwatch();
            watch.Start();

            PlaceDBManager dbManager = PlaceDBManager.Instance();

            var boardPixels = dbManager.GetCurrentImage(x, x + size, y, y + size);

            var board = DrawingHelper.GetEmptyGraphics(size, size);

            foreach (var pixel in boardPixels)
            {
                var color = new SKColor(pixel.R, pixel.G, pixel.B);
                board.Bitmap.SetPixel(pixel.XPos - x, pixel.YPos - y, color);
            }
            watch.Stop();

            // TODO Dispose stuff
            var stream = CommonHelper.GetStream(board.Bitmap);
            await Context.Channel.SendFileAsync(stream, $"verify_{x}_{y}.png", $"PIXELVERIFY {x} {y} SUCCESS Time: {watch.ElapsedMilliseconds}ms");
        }


        [Command("grid")]
        public async Task ViewGrid(int x = 0, int y = 0, int size = 1000)
        {
            int padding = 50;

            if (size < 50 || size > 1000 || CurrentPlaceBitmap == null)
            {
                await Context.Channel.SendMessageAsync("Size can only be between 50 and 1000 or CurrentPlaceBitmap is null");
                return;
            }

            long msDbTime = -1;

            Stopwatch watch = new Stopwatch();
            watch.Start();

            PlaceDBManager dbManager = PlaceDBManager.Instance();
            int pixelSize = 1_000 / size;

            int boardSize = pixelSize * size;

            int step = size / 10;


            //List<PlaceBoardPixel> boardPixels;

            //if (LastRefresh.Add(TimeSpan.FromMinutes(10)) > DateTime.Now)
            //{
            //    // cache is still new
            //    boardPixels = PixelsCache;
            //}
            //else
            //{
            //    Context.Channel.SendMessageAsync("Cache miss. It will take a few seconds to refresh and generate.");

            //    boardPixels = dbManager.GetCurrentImage();
            //    PixelsCache = boardPixels;
            //    LastRefresh = DateTime.Now;
            //}

            // do 25 lines
            RefreshBoard(25);

            List<PlaceBoardPixel> boardPixels = new List<PlaceBoardPixel>();
            // workaround for now to replace quicker the db load TODO Rework
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    short xNow = (short)(x + i);
                    short yNow = (short)(y + j);

                    if (xNow < 0 || yNow < 0 || xNow >= 1000 || yNow >= 1000)
                    {
                        continue;
                    }

                    var color = CurrentPlaceBitmap.GetPixel(xNow, yNow);

                    boardPixels.Add(new PlaceBoardPixel()
                    {
                        XPos = xNow,
                        YPos = yNow,
                        R = color.Red,
                        G = color.Green,
                        B = color.Blue
                    });
                }
            }


            //boardPixels = boardPixels.Where(i => i.XPos >= x && i.XPos < x + size && i.YPos >= y && i.YPos < y + size).ToList();
            //boardPixels = boardPixels.OrderBy(i => i.XPos).OrderBy(i => i.YPos).ToList();

            var board = DrawingHelper.GetEmptyGraphics(boardSize + padding * 2, boardSize + padding * 2);
            watch.Stop();

            msDbTime = watch.ElapsedMilliseconds;

            watch.Restart();

            var redColor = new SKColor(255, 0, 0);
            var whiteColor = new SKColor(255, 255, 255);

            int minXText = -1;
            int minYText = -1;

            //var pen = DrawingHelper.Pen_White;
            //var font = DrawingHelper.LargerTextFont;
            //var brush = DrawingHelper.SolidBrush_White;

            try
            {
                if (x < 0 || y < 0 || x + size >= 1000 || y + size >= 1000)
                {
                    for (int i = 0; i < boardSize; i++)
                    {
                        for (int j = 0; j < boardSize; j++)
                        {
                            if ((i + j) % 20 < 5)
                            {
                                board.Bitmap.SetPixel(padding + i, padding + j, redColor);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            foreach (var pixel in boardPixels)
            {
                var color = new SKColor(pixel.R, pixel.G, pixel.B);
                //board.Bitmap.SetPixel(pixel.XPos, pixel.YPos, color);

                for (int i = 0; i < pixelSize; i++)
                {
                    for (int j = 0; j < pixelSize; j++)
                    {
                        board.Bitmap.SetPixel(padding + (pixel.XPos - x) * pixelSize + i, padding + (pixel.YPos - y) * pixelSize + j, color);

                        int xZero = pixel.XPos - x;
                        int yZero = pixel.YPos - y;

                        if (xZero % step == 0 || pixel.XPos == x + size - 1)
                        {
                            board.Bitmap.SetPixel(padding + (pixel.XPos - x) * pixelSize + i, padding + (pixel.YPos - y) * pixelSize + j, redColor);

                            if (minXText < xZero)
                            {
                                // TODO verify if the size is right
                                board.Canvas.DrawText(pixel.XPos.ToString(), new SKPoint(padding + (pixel.XPos - x) * pixelSize + i - 5, 15), DrawingHelper.DefaultTextPaint);
                                board.Canvas.DrawText(pixel.XPos.ToString(), new SKPoint(padding + (pixel.XPos - x) * pixelSize + i - 5, padding + boardSize + 10), DrawingHelper.DefaultTextPaint);

                                minXText = xZero;
                            }
                        }

                        if (yZero % step == 0 || pixel.YPos == y + size - 1)
                        {
                            // TODO on out of bounce this doesnt show

                            board.Bitmap.SetPixel(padding + (pixel.XPos - x) * pixelSize + i, padding + (pixel.YPos - y) * pixelSize + j, redColor);

                            if (minYText < yZero)
                            {
                                board.Canvas.DrawText(pixel.YPos.ToString(), new SKPoint(5, padding + (pixel.YPos - y) * pixelSize + j - 5), DrawingHelper.DefaultTextPaint);
                                board.Canvas.DrawText(pixel.YPos.ToString(), new SKPoint(padding + boardSize + 5, padding + (pixel.YPos - y) * pixelSize + j - 5), DrawingHelper.DefaultTextPaint);

                                minYText = yZero;
                            }
                        }
                    }
                }
            }
            watch.Stop();

            // TODO Dispose stuff
            var stream = CommonHelper.GetStream(board.Bitmap);
            await Context.Channel.SendFileAsync(stream, "place.png", $"DB Time: {msDbTime}ms Draw Time: {watch.ElapsedMilliseconds}ms");
        }

        [Command("timelapse")]
        public async Task GenerateTimelapse(params SocketUser[] users)
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                Context.Channel.SendMessageAsync("You aren't allowed to run this command || Yes this is because someone likes to spam db heavy commands ||", false);
                return;
            }

            GenerateTimelapseCommans(0, 0, 1000, users.ToList());
        }

        [Command("timelapse")]
        public async Task GenerateTimelapse(int x, int y, int size, params SocketUser[] users)
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner && size > 250)
            {
                Context.Channel.SendMessageAsync("You aren't allowed to run this command for size > 250", false);
                return;
            }
            GenerateTimelapseCommans(x, y, size, users.ToList());
        }

        [Command("timelapse")]
        public async Task GenerateTimelapse(int x, int y, int size)
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner && size > 250)
            {
                Context.Channel.SendMessageAsync("You aren't allowed to run this command for size > 250", false);
                return;
            }

            GenerateTimelapseCommans(x, y, size, new List<SocketUser>());
        }

        [Command("timelapse")]
        public async Task GenerateTimelapse()
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                Context.Channel.SendMessageAsync("You aren't allowed to run this command || Yes this is because someone likes to spam db heavy commands ||", false);
                return;
            }

            GenerateTimelapseCommans(0, 0, 1000, new List<SocketUser>());
        }

        private async void GenerateTimelapseCommans(int x, int y, int size, List<SocketUser> socketUsers)
        {
            return;
            /*
            try
            {
                int textPadding = 50;

                Stopwatch watch = new Stopwatch();
                watch.Start();

                PlaceDBManager dbManager = PlaceDBManager.Instance();

                IEnumerable<PlaceBoardHistory> pixelHistory;

                List<short> userIds = new List<short>();

                // TODO
                //foreach (var item in socketUsers)
                //userIds.Add(item.Id);

                if (size < 0)
                {
                    // no zoom
                    size = 1000;
                    pixelHistory = dbManager.GetBoardHistory(userIds).OrderBy(i => i.PlaceBoardHistoryId);
                    x = 0;
                    y = 0;
                }
                else
                {
                    if (size < 10)
                        size = 10; // 10 is min size

                    // TODO
                    //pixelHistory = dbManager.GetBoardHistory(x, y, size, userIds).OrderBy(i => i.SnowflakeTimePlaced);
                }

                int secs = 30;
                int imagesPerSec = 60; // 60fps?

                int frames = secs * imagesPerSec;

                ulong step = 1;// (pixelHistory.Last().SnowflakeTimePlaced - pixelHistory.First().SnowflakeTimePlaced) / (ulong)frames;

                ulong last = 0;

                if (!Directory.Exists("TimelapseOutput"))
                    Directory.CreateDirectory("TimelapseOutput");

                //var files = Directory.GetFiles("TimelapseOutput");
                //if (files.Length > 0)
                //{
                //    foreach (var file in files)
                //    {
                //        //File.Delete(file);
                //    }
                //}
       

                int pixelSize = 1_000 / size;

                int boardSizeVal = pixelSize * size;

                int frameCounter = 0;

                var board = DrawingHelper.GetEmptyGraphics(boardSizeVal, boardSizeVal + textPadding);
                var boardSize = new System.Drawing.Size(boardSizeVal, boardSizeVal + textPadding);





                var pen = DrawingHelper.Pen_White;
                var font = DrawingHelper.TitleFont;
                var brush = DrawingHelper.SolidBrush_White;

                List<short> users = new System.Collections.Generic.List<short>();
                int pixelCount = 0;

                if (!LoadedLib)
                {
                    // TODO move this to a setting
#if !DEBUG


                    //FFmpegLoader.FFmpegPath = "/usr/local/lib"; // Rel path

                    FFmpegLoader.LogVerbosity = LogLevel.Verbose;
                    FFmpegLoader.LogCallback += FFmpegLoader_LogCallback;
#else
                    //FFmpegLoader.FFmpegPath = "ffmpeg"; // Rel path
#endif
                    // TODO dont hardcode
                }
                LoadedLib = true;

                // You can set there codec, bitrate, frame rate and many other options.
                var settings = new VideoEncoderSettings(width: boardSize.Width, height: boardSize.Height, framerate: imagesPerSec, codec: VideoCodec.H264);
                settings.EncoderPreset = EncoderPreset.Fast;

                //settings.CRF = 17;

                string filename = $"timelapse_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.mp4";
                using (var file = MediaBuilder.CreateContainer(Path.Combine(Directory.GetCurrentDirectory(), "TimelapseOutput", filename)).WithVideo(settings).Create())
                {
                    string text = $"{SnowflakeUtils.FromSnowflake(last).ToString("yyyy-MM-dd HH:mm:ss")} PixelsPlaced: {pixelCount.ToString("N0")} Users participated: {users.Count.ToString("N0")}";

                    var frame = CopyAndDrawOnBitmap(board.Bitmap, text, 10, boardSize.Height - textPadding + 5, boardSize);

                    file.Video.AddFrame(ToImageData(frame));




                    //board.Bitmap.Save(Path.Combine("Timelapse", $"{fileName}{frameCounter.ToString("D6")}.png"));
                    //frameCounter++;


                    foreach (var history in pixelHistory)
                    {

                        if (!users.Contains(history.PlaceDiscordUserId))
                            users.Add(history.PlaceDiscordUserId);

                        var color = System.Drawing.Color.FromArgb(history.R, history.G, history.B);
                        //board.Bitmap.SetPixel(history.XPos - x, history.YPos - y, color);

                        for (int i = 0; i < pixelSize; i++)
                        {
                            for (int j = 0; j < pixelSize; j++)
                            {
                                board.Bitmap.SetPixel((history.XPos - x) * pixelSize + i, (history.YPos - y) * pixelSize + j, color);
                            }
                        }

                        pixelCount++;

                        
                        //if (last + step < history.SnowflakeTimePlaced)
                        //{
                        //    // generate a new frame
                        //    //last = history.SnowflakeTimePlaced;


                        //    text = $"{SnowflakeUtils.FromSnowflake(last).ToString("yyyy-MM-dd HH:mm:ss")} PixelsPlaced: {pixelCount.ToString("N0")} Users participated: {users.Count.ToString("N0")}";

                        //    frame = CopyAndDrawOnBitmap(board.Bitmap, text, 10, boardSize.Height - textPadding + 5, boardSize);

                        //    file.Video.AddFrame(ToImageData(frame));


                        //    //board.Graphics.DrawString($"{SnowflakeUtils.FromSnowflake(last).ToString()}", font, brush, new System.Drawing.Point(40, 40));


                        //    //board.Bitmap.Save(Path.Combine("Timelapse", $"{fileName}{frameCounter.ToString("D6")}.png"));
                        //    frameCounter++;
                        //}
                    }


                    text = $"{SnowflakeUtils.FromSnowflake(last).ToString("yyyy-MM-dd HH:mm:ss")} PixelsPlaced: {pixelCount.ToString("N0")} Users participated: {users.Count.ToString("N0")}";

                    frame = CopyAndDrawOnBitmap(board.Bitmap, text, 10, boardSize.Height - textPadding + 5, boardSize);

                    // still image for 2 sec 
                    for (int i = 0; i < imagesPerSec * 2; i++)
                    {
                        file.Video.AddFrame(ToImageData(frame));
                    }



                    //board.Graphics.DrawString($"{SnowflakeUtils.FromSnowflake(last).ToString()}", font, brush, new System.Drawing.Point(40, 40));


                    // just the final image
                    //board.Bitmap.Save(Path.Combine("Timelapse", $"{fileName}{frameCounter.ToString("D6")}.png"));
                    frameCounter++;
                }


                await Context.Channel.SendFileAsync(Path.Combine("TimelapseOutput", filename), $"Time: {watch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                //await Context.Channel.SendMessageAsync(ex.Message);
                string exString = ex.ToString();
                await Context.Channel.SendMessageAsync(exString.Substring(0, Math.Min(2000, exString.Length)));
            }
            */
        }

        private void FFmpegLoader_LogCallback(string message)
        {
            Console.WriteLine(message);
        }



        private static bool Refreshing = false;



        private void RefreshBoard(int ySize)
        {
            // use lock and stuff but lazy to do it properly haha xD and the db is dying atm (just for future myself)
            if (Refreshing)
                return;

            if (IsPlaceLocked())
                return; // no db stuff when place locked and also totaly not to make sure the bitmap is empty hehe :/

            PlaceDBManager dbManager = PlaceDBManager.Instance();
            try
            {
                Refreshing = true;

                int from = LastYRefreshed;
                int until = LastYRefreshed + ySize;

                var pixels = dbManager.GetImageByYLines(from, until);

                foreach (var pixel in pixels)
                    CurrentPlaceBitmap.SetPixel(pixel.XPos, pixel.YPos, new SKColor(pixel.R, pixel.G, pixel.B));

                LastYRefreshed = until;
                if (LastYRefreshed > 999)
                    LastYRefreshed = 0;
            }
            catch (Exception ex)
            {

            }
            finally
            {
                Refreshing = false;
            }
        }



        public static Dictionary<ulong, DateTimeOffset> ViewTimeout = new Dictionary<ulong, DateTimeOffset>();

        [Command("view")]
        public async Task ViewBoard(bool forceReload = false)
        {
            try
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner && forceReload)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command || Yes this is because someone likes to spam db heavy commands ||", false);
                    return;
                }

                ulong userId = Context.Message.Author.Id;

                // Verify if the current user is locked
                if (ViewTimeout.ContainsKey(userId) && ViewTimeout[userId] > DateTime.UtcNow)
                {
                    //if (new Random().Next(0, 2) % 2 == 0)
                    Context.Channel.SendMessageAsync($"Still in timeout for {Math.Round((ViewTimeout[userId] - DateTime.UtcNow).TotalSeconds, 3)} seconds");

                    return;
                }

                var blockUntil = DateTime.UtcNow.AddSeconds(15);

                if (ViewTimeout.ContainsKey(userId))
                    ViewTimeout[userId] = blockUntil;
                else
                    ViewTimeout.Add(userId, blockUntil);


                long msDbTime = -1;
                Stopwatch watch = new Stopwatch();
                watch.Start();

                //PlaceDBManager dbManager = PlaceDBManager.Instance();

                List<PlaceBoardPixel> boardPixels;

                RefreshBoard(50);

                // old system overload the db often too much
                /*if (LastRefresh.Add(TimeSpan.FromMinutes(10)) > DateTime.Now && !forceReload)
                {
                    // cache is still new
                    boardPixels = PixelsCache;
                }
                else
                {
                    Refreshing = true;
                    LastRefresh = DateTime.Now;
                    Context.Channel.SendMessageAsync("Cache miss. It will take a few seconds to refresh and generate.");
                    // reset old cache
                    PixelsCache = new List<PlaceBoardPixel>();

                    boardPixels = dbManager.GetCurrentImage();
                    PixelsCache = boardPixels; 
                    Refreshing = false;
                }*/

                watch.Stop();

                msDbTime = watch.ElapsedMilliseconds;
                /*
                watch.Restart();

                var board = DrawingHelper.GetEmptyGraphics(1000, 1000);

                for (int i = 0; i < boardPixels.Count; i++)
                {
                    var pixel = boardPixels[i];
                    var color = System.Drawing.Color.FromArgb(pixel.R, pixel.G, pixel.B);
                    board.Bitmap.SetPixel(pixel.XPos, pixel.YPos, color);
                }

                watch.Stop();*/

                // TODO Dispose stuff

        
                var stream = CommonHelper.GetStream(CurrentPlaceBitmap);
                //await Context.Channel.SendFileAsync(stream, "place.png", $"DB Time: {msDbTime}ms Draw Time: {watch.ElapsedMilliseconds}ms");
                await Context.Channel.SendFileAsync(stream, "place.png", $"DB Time: {msDbTime}ms Web Viewer: https://place.battlerush.dev/");
            }
            catch (Exception ex)
            {
                string exString = ex.ToString();
                await Context.Channel.SendMessageAsync(exString.Substring(0, Math.Min(2000, exString.Length)));
            }
        }

        [Command("history")]
        public async Task PixelHistory(int x, int y)
        {
            PixelHistoryTask(x, y, null); // TODO by person if not all?
        }

        [Command("history")]
        public async Task PixelHistory(string all)
        {
            PixelHistoryTask(-1, -1, all); // TODO by person if not all?
        }

        public async Task PixelHistoryTask(int x = -1, int y = -1, string all = null)
        {
            if ((x < 0 || x >= 1000 || y < 0 || y >= 1000) && all.ToLower() != "all")
            {
                await Context.Channel.SendMessageAsync($"Pixel not found");
                return;
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();

            PlaceDBManager dbManager = PlaceDBManager.Instance();

            int amount = 25;

            string title = "";

            List<PlaceBoardHistory> pixelHistory;

            // TODO rework all request

            if (all?.ToLower() != "all")
            {
                pixelHistory = dbManager.GetPixelHistory(x, y, amount);
                title = $"Last {pixelHistory.Count} placements for {x}/{y}";
            }
            else
            {
                pixelHistory = dbManager.GetLastPixelHistory(amount);
                title = $"Last {pixelHistory.Count} placements for all";
            }

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle(title);


            string messageText = "";
            foreach (var pixel in pixelHistory)
            {
                var color = new SKColor(pixel.R, pixel.G, pixel.B);
                var stringHex = "#TODO set color hex";

                ulong discordUserId = PlaceDiscordUsers.FirstOrDefault(i => i.PlaceDiscordUserId == pixel.PlaceDiscordUserId)?.DiscordUserId ?? 0;
                if (all?.ToLower() == "all")
                    messageText += $"<@{discordUserId}> placed ({stringHex}) at {pixel.XPos}/{pixel.YPos} {pixel.PlacedDateTime.ToString("MM.dd HH:mm:ss")} {Environment.NewLine}"; // todo check for everyone or here
                else
                    messageText += $"<@{discordUserId}> placed ({stringHex}) at {pixel.PlacedDateTime.ToString("MM.dd HH:mm")} {Environment.NewLine}"; // todo check for everyone or here

            }

            messageText += Environment.NewLine;

            builder.WithDescription(messageText);
            builder.WithColor(192, 168, 0);

            builder.WithAuthor(Context.Message.Author);
            builder.WithCurrentTimestamp();

            watch.Stop();

            await Context.Message.Channel.SendMessageAsync($"DB Time: {watch.ElapsedMilliseconds}ms", false, builder.Build());
        }

        public static Dictionary<ulong, DateTimeOffset> ZoomTimeout = new Dictionary<ulong, DateTimeOffset>();


        // TODO move maybe to bitmap image

        [Command("zoom")]
        public async Task ZoomIntoTheBoard(int x, int y, int size = 100)
        {
            if (size > 350 || size < 10)
            {
                await Context.Channel.SendMessageAsync("Size can only be between 10 and 350");
                return;
            }
            ulong userId = Context.Message.Author.Id;

            // Verify if the current user is locked
            if (ZoomTimeout.ContainsKey(userId) && ZoomTimeout[userId] > DateTime.UtcNow)
            {
                //if (new Random().Next(0, 2) % 2 == 0)
                Context.Channel.SendMessageAsync($"Still in timeout for {Math.Round((ZoomTimeout[userId] - DateTime.UtcNow).TotalSeconds, 3)} seconds");

                return;
            }

            var blockUntil = DateTime.UtcNow.AddSeconds(size / 25); // 1 per second

            if (ZoomTimeout.ContainsKey(userId))
                ZoomTimeout[userId] = blockUntil;
            else
                ZoomTimeout.Add(userId, blockUntil);

            long msDbTime = -1;

            Stopwatch watch = new Stopwatch();
            watch.Start();

            PlaceDBManager dbManager = PlaceDBManager.Instance();
            int pixelSize = 1_000 / size;

            int boardSize = pixelSize * size;


            var boardPixels = dbManager.GetCurrentImage(x, x + size, y, y + size);

            var board = DrawingHelper.GetEmptyGraphics(boardSize, boardSize);
            watch.Stop();

            msDbTime = watch.ElapsedMilliseconds;

            watch.Restart();


            var redColor = new SKColor(255, 0, 0);

            try
            {
                if (x < 0 || y < 0 || x + size >= 1000 || y + size >= 1000)
                    for (int i = 0; i < boardSize; i++)
                        for (int j = 0; j < boardSize; j++)
                            if ((i + j) % 20 < 5)
                                board.Bitmap.SetPixel(i, j, redColor);
            }
            catch (Exception ex)
            {

            }

            foreach (var pixel in boardPixels)
            {
                var color = new SKColor(pixel.R, pixel.G, pixel.B);
                //board.Bitmap.SetPixel(pixel.XPos, pixel.YPos, color);

                for (int i = 0; i < pixelSize; i++)
                {
                    for (int j = 0; j < pixelSize; j++)
                    {
                        board.Bitmap.SetPixel((pixel.XPos - x) * pixelSize + i, (pixel.YPos - y) * pixelSize + j, color);
                    }
                }
            }

            // for any out of bounds pixels for the sneaky people

            watch.Stop();

            // TODO Dispose stuff
            var stream = CommonHelper.GetStream(board.Bitmap);
            await Context.Channel.SendFileAsync(stream, "place.png", $"DB Time: {msDbTime}ms Draw Time: {watch.ElapsedMilliseconds}ms");


            // size can be between 10 and 1000
            // accept hex or int as cord
        }

        [Command("perf")]
        public async Task PlacePerf(int lastSize = 1440)
        {
            PlacePerf(true, lastSize);
        }

        [Command("perf")]
        public async Task PlacePerf(bool graphMode = true, int lastSize = 1440)
        {
            PlaceDBManager dbManager = PlaceDBManager.Instance();

            var list = dbManager.GetPlacePerformanceInfo(lastSize);

            if(list.Count == 0) 
                return;

            var startTime = list.First().DateTime;
            var endTime = list.Last().DateTime;

            var listStartUpTimes = dbManager.GetBotStartUpTimes(startTime, endTime);

            if (graphMode)
            {
                try
                {
                    // TODO optimize some lines + move to draw helper
                    var dataPointsAvg = list.ToDictionary(i => i.DateTime, i => i.AvgTimeInMs);
                    var dataPointsCount = list.ToDictionary(i => i.DateTime, i => i.SuccessCount);
                    var dataPointsFailed = list.ToDictionary(i => i.DateTime, i => i.FailedCount);

                    int maxCountSuccess = list.OrderByDescending(i => i.SuccessCount).First().SuccessCount;
                    int maxCountFailed = list.OrderByDescending(i => i.FailedCount).First().FailedCount;

                    int maxVal = Math.Max(maxCountFailed, maxCountSuccess);

                    var drawInfo = DrawingHelper.GetEmptyGraphics();
                    var padding = DrawingHelper.DefaultPadding;
                    var labels = DrawingHelper.GetLabels(dataPointsAvg, 6, 10, true, startTime, endTime, " ms");
                    var labelsCount = DrawingHelper.GetLabels(dataPointsCount, 6, 10, true, startTime, endTime);
                    var gridSize = new GridSize(drawInfo.Bitmap, padding);
                    var dataPointListAvg = DrawingHelper.GetPoints(dataPointsAvg, gridSize, true, startTime, endTime);
                    var dataPointListCount = DrawingHelper.GetPoints(dataPointsCount, gridSize, true, startTime, endTime, false, maxVal);
                    var dataPointListFailed = DrawingHelper.GetPoints(dataPointsFailed, gridSize, true, startTime, endTime, false, maxVal);

                    DrawingHelper.DrawGrid(drawInfo.Canvas, gridSize, padding, labels.XAxisLables, labels.YAxisLabels, $"Place Perf {list.Count} mins", labelsCount.YAxisLabels);
                    // todo add 2. y Axis on the right

                    DrawingHelper.DrawLine(drawInfo.Canvas, drawInfo.Bitmap, dataPointListAvg, 6, new SKPaint() { Color = new SKColor(255, 0, 0) }, "Avg in ms / min", 0, true); //new Pen(System.Drawing.Color.LightGreen)
                    DrawingHelper.DrawLine(drawInfo.Canvas, drawInfo.Bitmap, dataPointListCount, 6, new SKPaint() { Color = new SKColor(0, 255, 0) }, "Count / min", 1, true); // new Pen(System.Drawing.Color.Yellow)
                    DrawingHelper.DrawLine(drawInfo.Canvas, drawInfo.Bitmap, dataPointListFailed, 6, new SKPaint() { Color = new SKColor(0, 0, 255) }, "Failed Count / min", 2, true); // new Pen(System.Drawing.Color.DarkOrange)

                    // TODO add methods to the drawing lib
                    if (listStartUpTimes.Count > 0)
                    {
                        int totalMins = (int)(endTime - startTime).TotalMinutes;
                        foreach (var startUpTimes in listStartUpTimes)
                        {
                            double percent = (startUpTimes.StartUpTime - startTime).TotalMinutes / totalMins;
                            int x = (int)(gridSize.XMin + gridSize.XSize * percent);
                            drawInfo.Canvas.DrawLine(new SKPoint(x, gridSize.YMin), new SKPoint(x, gridSize.YMax), new SKPaint() { Color = new SKColor(255, 0, 0)});
                        }
                    }

                    var stream = CommonHelper.GetStream(drawInfo.Bitmap);

                    drawInfo.Bitmap.Dispose();
                    drawInfo.Canvas.Dispose();

                    await Context.Channel.SendFileAsync(stream, "place_perf.png", $"");
                }
                catch (Exception ex)
                {
                    // TODO add Logger
                    Console.WriteLine(ex.ToString());
                }
            }
            else
            {
                list = list.Take(250).ToList(); // max 250

                string output = "```";
                foreach (var item in list)
                {
                    output += $"{item.DateTime.ToString("dd.MM HH:mm")} Success: {item.SuccessCount.ToString("N0")} Failed: {item.FailedCount.ToString("N0")} Avg: {item.AvgTimeInMs}{Environment.NewLine}";

                    if (output.Length > 1950)
                    {
                        output += "```";
                        await Context.Channel.SendMessageAsync(output);
                        output = "```";
                    }
                }

                if (output.Length > 0)
                {
                    output += "```";
                    await Context.Channel.SendMessageAsync(output);
                }
            }
        }

        public static List<long> PixelPlacementTimeLastMinute = new List<long>();
        public static int FailedPixelPlacements = 0;

        public static readonly object PlaceAggregateObj = new object();
        //static long OldPixelCountMod = -1;

        // TODO Move chunk logic into a seperate file
        private bool IsNewChunkAvailable(PlaceDBManager dbManager)
        {
            var lastPixelIdChunked = GetLastPixelIdChunked(); // Get info about the last pixel id that was saved in a chunk
            long unchunkedPixelCount = dbManager.GetUnchunkedPixels(lastPixelIdChunked);

            return unchunkedPixelCount >= 100_000;
        }

        private long GetLastPixelIdChunked()
        {
            return DatabaseManager.Instance().GetBotSettings()?.PlacePixelIdLastChunked ?? -1;
        }

        private long GetTotalChunkedPixels()
        {
            int size = 100_000; // hardcoded chunk size
            int totalChunkedPixels = (DatabaseManager.Instance().GetBotSettings()?.PlaceLastChunkId ?? 0) * size;

            return totalChunkedPixels;
        }

        [MethodImpl(MethodImplOptions.Synchronized)] // TODO check if even needed
        private void AggregatePlace(PlaceDBManager dbManager)
        {
            double avgPixelTime = PixelPlacementTimeLastMinute.Average();

            dbManager.AddPlacePerfRecord(new PlacePerformanceInfo()
            {
                DateTime = DateTime.UtcNow,
                AvgTimeInMs = (int)avgPixelTime,
                SuccessCount = PixelPlacementTimeLastMinute.Count,
                FailedCount = FailedPixelPlacements
            });

            FailedPixelPlacements = 0;
            PixelPlacementTimeLastMinute = new List<long>();

            LastStatusRefresh = DateTime.Now;

            var lastPixelIdChunked = GetLastPixelIdChunked();
            var totalPixelsChunked = GetTotalChunkedPixels();

            var totalPixelsPlaced = dbManager.GetBoardHistoryCount(lastPixelIdChunked, totalPixelsChunked);

            if (IsNewChunkAvailable(dbManager))
            {
                // we can generate a new chunk
                AutomaticGenChunk();
            }

            //OldPixelCountMod = totalPixelsPlaced % 100_000;

            Program.Client.SetGameAsync($"{totalPixelsPlaced:N0} pixels", null, ActivityType.Watching);
            RefreshBoard(10);

            if (DateTime.Now.Minute % 60 == 0 || PlaceDiscordUsers.Count == 0)
            {
                // refresh the db users incase any new
                PlaceDiscordUsers = dbManager.GetPlaceDiscordUsers();
            }
        }

        // DUPLICATE CODE WITH genchunk (which could be removed if this automatic code works)
        private async void AutomaticGenChunk(bool sendMessage = true)
        {
            // TODO Create PlaceChunkTable to save info about each chunk

            Stopwatch watch = new Stopwatch();
            watch.Start();

            // todo config
            ulong guildId = Program.BaseGuild;
            ulong spamChannel = 768600365602963496;

            var guild = Program.Client.GetGuild(guildId);
            var textChannel = guild.GetTextChannel(spamChannel);

            var chunkFolder = Path.Combine(Program.BasePath, "TimelapseChunks");

            if (!Directory.Exists(chunkFolder))
                Directory.CreateDirectory(chunkFolder);

            PlaceDBManager dbManager = PlaceDBManager.Instance();

            PlaceDiscordUsers = dbManager.GetPlaceDiscordUsers();

            int size = 100_000;
            var lastPixelIdChunked = GetLastPixelIdChunked();
            var totalPixelsChunked = GetTotalChunkedPixels();

            var totalPixelsPlaced = dbManager.GetBoardHistoryCount(lastPixelIdChunked, totalPixelsChunked);

            if(sendMessage)
                await textChannel.SendMessageAsync($"Total pixels to load {totalPixelsPlaced.ToString("N0")}", false);

            //short chunkId = 0;

            var botSettings = DatabaseManager.Instance().GetBotSettings();

            short nextChunkId = (short)(botSettings.PlaceLastChunkId + 1);

            string file = $"Chunk_{nextChunkId}.dat";
            string filePath = Path.Combine(chunkFolder, file);


            byte[] data = new byte[3 + size * 12];
            data[0] = (byte)MessageEnum.GetChunk_Response; // id of response

            // Chunk identifier
            byte[] chunkIdBytes = BitConverter.GetBytes(nextChunkId);
            data[1] = chunkIdBytes[0];
            data[2] = chunkIdBytes[1];



            // 1 entry 12 bytes -> chunk size = 1.2MB

            // Repeating rel pos
            // 0-3 | ID (int32)
            // 4-5 | XPos (int16)
            // 6-7 | XPos (int16)
            // 8 | R color (byte)
            // 9 | G color (byte)
            // 10 | B color (byte)
            // 11 | UserId (byte) 

            // TODO Optimizations
            // Store x/y in 10 bits each (-1.5 bytes)
            // do aux table for users and store them in 1 byte instead of 8 bytes (-7 bytes)
            // add custom timestamp (in seconds to save even more space) (+3/4 bytes)

            int counter = 3;

            var pixelHistory = dbManager.GetBoardHistory(botSettings.PlacePixelIdLastChunked, size);

            if(pixelHistory.Count != size)
            {
                // history is not complete
                // TODO log the error
                return;
            }

            foreach (var item in pixelHistory)
            {
                byte[] idBytes = BitConverter.GetBytes(item.PlaceBoardHistoryId);

                byte[] xBytes = BitConverter.GetBytes(item.XPos);
                byte[] yBytes = BitConverter.GetBytes(item.YPos);

                data[counter] = idBytes[0];
                data[counter + 1] = idBytes[1];
                data[counter + 2] = idBytes[2];
                data[counter + 3] = idBytes[3];
                counter += 4;

                data[counter] = xBytes[0];
                data[counter + 1] = xBytes[1];
                data[counter + 2] = yBytes[0];
                data[counter + 3] = yBytes[1];
                counter += 4;

                data[counter] = item.R;
                data[counter + 1] = item.G;
                data[counter + 2] = item.B;
                counter += 3;

                // get user id (limited currently to 255)
                data[counter] = Convert.ToByte(item.PlaceDiscordUserId);

                counter += 1;
            }

            if(sendMessage)
                await textChannel.SendMessageAsync($"Saved {file}", false);

            File.WriteAllBytes(filePath, data);


            watch.Stop();


            // save the last saved chunk

            botSettings.PlaceLastChunkId = nextChunkId;
            botSettings.PlacePixelIdLastChunked = pixelHistory.OrderBy(i => i.PlaceBoardHistoryId).Last().PlaceBoardHistoryId;

            DatabaseManager.Instance().SetBotSettings(botSettings);

            if(sendMessage)
                await textChannel.SendMessageAsync($"Done. Timelapse has been updated automatically in {watch.ElapsedMilliseconds}ms", false);
        }

        [Command("setpixel")]
        public async Task PlaceColor(short x, short y, string colorString, [Remainder] string comment = "")
        {
            if (IsPlaceLocked())
            {
                // 1 in 5 send a message
                if (!Context.Message.Author.IsBot && new Random().Next(0, 5) % 5 == 0)
                {
                    await Context.Channel.SendMessageAsync("Board is locked");
                }

                return;
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();

            var isBot = Context.Message.Author.IsBot;
            ulong userId = Context.Message.Author.Id;

            // prevent people placing multipixels from placing single pixels
            if (!isBot)
            {
                // Verify if the current user is locked
                if (MultiPlacement.ContainsKey(userId) && MultiPlacement[userId] > DateTime.UtcNow)
                {
                    if (new Random().Next(0, 10) % 10 == 0)
                        Context.Channel.SendMessageAsync($"You think you are being clever huh?");

                    return;
                }
            }

            if (colorString.IndexOf('#') == -1)
                colorString = "#" + colorString;

            if (!SKColor.TryParse(colorString, out SKColor color))
                return; // invalid color

            PlaceDBManager dbManager = PlaceDBManager.Instance();

            var successfull = dbManager.PlacePixel(x, y, color, userId);

            watch.Stop();

            if (successfull)
            {
                PixelPlacementTimeLastMinute.Add(watch.ElapsedMilliseconds);
            }
            else
            {
                lock (PlaceAggregateObj)
                {
                    FailedPixelPlacements++;
                }
            }


            lock (PlaceAggregateObj)
            {
                if (LastStatusRefresh.Add(TimeSpan.FromSeconds(60)) < DateTime.Now)
                    AggregatePlace(dbManager);
            }

            if (!isBot && successfull)
            {
                Context.Channel.SendMessageAsync($"Placed {color.Red}.{color.Green}.{color.Blue} on X: {x} Y: {y}");
            }
        }

        public static Dictionary<ulong, DateTimeOffset> MultiPlacement = new Dictionary<ulong, DateTimeOffset>();
        public static DateTime LastStatusRefresh = DateTime.Now; // set to now to prevent recordw in perf with count = 1

        [Command("multipixel")]
        public async Task PlaceMultipleColor([Remainder] string input = "")
        {
            if (IsPlaceLocked() || Context.Message.Author.IsBot)
            {
                // 1 in 5 send a message
                if (!Context.Message.Author.IsBot && new Random().Next(0, 5) % 5 == 0)
                    await Context.Channel.SendMessageAsync("Board is locked");

                return;
            }

            // TODO make it possible to restart a canceled job

            PlaceDBManager placeDBManager = PlaceDBManager.Instance();
            DatabaseManager databaseManager = DatabaseManager.Instance();

            try
            {
                ulong userId = Context.Message.Author.Id;
                var discordUser = databaseManager.GetDiscordUserById(userId);
                var placeDiscordUser = placeDBManager.GetPlaceDiscordUserByDiscordUserId(userId);

                // if the user never placer a pixel before create a record for them
                if (placeDiscordUser == null)
                {
                    if (placeDBManager.AddPlaceDiscordUser(userId))
                    {
                        placeDiscordUser = placeDBManager.GetPlaceDiscordUserByDiscordUserId(userId);
                    }
                    else
                    {
                        Context.Channel.SendMessageAsync($"REJECTED UNABLE_TO_CREATE_USER <@{userId}>");
                        return;
                    }
                }

                if (!discordUser.AllowedPlaceMultipixel)
                {
                    Context.Channel.SendMessageAsync($"REJECTED NOT_VERIFIED <@{userId}>");
                    return;
                }

                var activeJobs = placeDBManager.GetMultipixelJobs(placeDiscordUser.PlaceDiscordUserId);

                // Verify if the current user is locked
                if (activeJobs.Count > 0)
                {
                    // User is still in lock mode -> cancel
                    Context.Channel.SendMessageAsync($"REJECTED ACTIVE_JOB_AVAILABLE <@{userId}>");
                    return;
                }

                var attachments = Context.Message.Attachments;

                if (attachments.Count == 1)
                {
                    var firstAttachment = attachments.First();

                    using (var client = new WebClient())
                    {
                        byte[] buffer = client.DownloadData(firstAttachment.Url);

                        string download = Encoding.UTF8.GetString(buffer);

                        if (download.Contains('|'))
                        {
                            int lines = download.Split('|').Length;

                            // only if attachment has min 10 lines then use it
                            if (lines > 10)
                                input = download;
                        }
                    }
                }


                input = input.Trim();

                List<string> instructions = input.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();

                if (instructions.Count < 10)
                {
                    // reject if less than 10 instructions queued
                    Context.Channel.SendMessageAsync($"REJECTED TOO_FEW <@{userId}>");
                    return;
                }

                if (instructions.Count > 86_400)
                {
                    Context.Channel.SendMessageAsync($"REJECTED TOO_MANY <@{userId}>");
                    return;
                }


                List<string> validInstructions = new List<string>();

                foreach (var item in instructions)
                {
                    // Ignore empty records
                    if (item.Length == 0)
                        continue;

                    Stopwatch watch = new Stopwatch();
                    watch.Start();

                    var commands = item.Split(' ');

                    short x = short.Parse(commands[0]);
                    short y = short.Parse(commands[1]);

                    if (x < 0 || y < 0 || x >= 1000 || y >= 1000)
                        continue; // invalid cords

                    if (commands[2].Length > 7)
                        continue; // illegal hex string (TODO check if the string is rly a hex color)


                    string singleInstruction = commands[0] + "|" + commands[1] + "|" + commands[2];

                    validInstructions.Add(singleInstruction);
                }

                Context.Channel.SendMessageAsync($"VERIFIED_{validInstructions.Count}/{instructions.Count}_INSTRUCTIONS <@{userId}>");

                if (validInstructions.Count == 0)
                {
                    Context.Channel.SendMessageAsync($"CANCELED_NO_VALID_INSTRUCTIONS {instructions.Count} <@{userId}>");
                    return;
                }

                var newJob = placeDBManager.CreatePlaceMultipixelJob(placeDiscordUser.PlaceDiscordUserId, validInstructions.Count);
                Context.Channel.SendMessageAsync($"CREATED_JOB_{newJob.PlaceMultipixelJobId} <@{userId}>");

                placeDBManager.UpdatePlaceMultipixelJobStatus(newJob.PlaceMultipixelJobId, MultipixelJobStatus.Importing);

                int packetSize = 100;
                int packetCount = 0;
                for (int i = 0; i < validInstructions.Count; i += packetSize)
                {
                    var currentInstructions = validInstructions.Skip(i).Take(packetSize);

                    string packetInstruction = string.Join(";", currentInstructions);

                    placeDBManager.CreateMultipixelJobPacket(newJob.PlaceMultipixelJobId, packetInstruction, currentInstructions.Count());
                    packetCount++;
                }

                placeDBManager.UpdatePlaceMultipixelJobStatus(newJob.PlaceMultipixelJobId, MultipixelJobStatus.Ready);

                Context.Channel.SendMessageAsync($"JOB_{newJob.PlaceMultipixelJobId}_SETREADY_{packetCount}_PACKETS <@{userId}>");
                Context.Channel.SendMessageAsync($"It may take up to 100 seconds for your job to go to ACTIVE status. Check with {Program.CurrentPrefix}place viewmultipixel your job status.");
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message + $" <@{Context.Message.Author.Id}>");
            }
        }

        [Command("viewmultipixel")]
        public async Task ViewMultiPixel()
        {
            PlaceDBManager placeDBManager = PlaceDBManager.Instance();
            var placeDiscordUser = placeDBManager.GetPlaceDiscordUserByDiscordUserId(Context.Message.Author.Id);
            if (placeDiscordUser == null)
            {
                Context.Channel.SendMessageAsync($"User has no jobs created.");
                return;
            }

            var jobs = placeDBManager.GetMultipixelJobs(placeDiscordUser.PlaceDiscordUserId, false);

            EmbedBuilder builder = new EmbedBuilder();

            string prefix = Program.CurrentPrefix;


            int g = 0;
#if DEBUG
            g = 192;
#endif

            builder.WithTitle("ETH DINFK Place Multipixel Jobs");
            builder.WithDescription($@"View your last 10 Multipixel Jobs and their status
{prefix}place multipixel <file> (To start a new job)
{prefix}place cancelmultipixel <id> (To cancel an active job)");

            builder.WithColor(g, 255, 0);

            // only last 10
            foreach (var job in jobs.Skip(Math.Max(0, jobs.Count() - 10)))
            {
                int pixelPainted = 0;
                if (job.Status == (int)MultipixelJobStatus.Done)
                    pixelPainted = job.TotalPixels;
                else
                    pixelPainted = 100 * placeDBManager.GetFinishedMultipixelJobPacketCount(job.PlaceMultipixelJobId);

                builder.AddField($"JOB {job.PlaceMultipixelJobId}", $"```Created {job.CreatedAt} Total: {job.TotalPixels} Done: {pixelPainted} Status: {(MultipixelJobStatus)job.Status}```");
            }

            builder.WithThumbnailUrl(Context.Message.Author.GetAvatarUrl());
            builder.WithAuthor(Context.Message.Author);
            builder.WithCurrentTimestamp();

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }


        [Command("cancelmultipixel")]
        public async Task ViewMultiPixel(int multiPixelJobId)
        {
            PlaceDBManager placeDBManager = PlaceDBManager.Instance();

            var placeDiscordUser = placeDBManager.GetPlaceDiscordUserByDiscordUserId(Context.Message.Author.Id);

            var job = placeDBManager.GetPlaceMultipixelJob(multiPixelJobId);

            if (job.PlaceDiscordUserId != placeDiscordUser.PlaceDiscordUserId || job.Status > (int)MultipixelJobStatus.Active)
            {
                Context.Channel.SendMessageAsync($"You can only cancel your own jobs and if they are in the active state.");
                return;
            }

            var success = placeDBManager.UpdatePlaceMultipixelJobStatus(job.PlaceMultipixelJobId, MultipixelJobStatus.Canceled);
            if (success)
                Context.Channel.SendMessageAsync($"Canceled the job. It may still continue the current job for up to 100 seconds.");
            else
                Context.Channel.SendMessageAsync($"Error while canceling job.");
        }
    }
    public class PlacePixelPerfEntry
    {
        public DateTimeOffset DateTime { get; set; }
        public int Count { get; set; }
        public double Avg { get; set; }
    }
}
