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
using ImageMagick;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
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

                newPixel.Placements.Add(pixel.SnowflakeTimePlaced, pixel.DiscordUserId);
                Pixels.Add(newPixel);
            }
            else
            {
                listPixel.R = pixel.R;
                listPixel.G = pixel.G;
                listPixel.B = pixel.B;
                listPixel.Placements.Add(pixel.SnowflakeTimePlaced, pixel.DiscordUserId);
            }
        }
    }


    [Group("place")]
    public class PlaceModule : ModuleBase<SocketCommandContext>
    {
        //public static List<PlaceBoardPixel> PixelsCache = new List<PlaceBoardPixel>();
        public static DateTime LastRefresh = DateTime.MinValue;

        public static bool? LockedBoard = null;

        public static Bitmap CurrentPlaceBitmap;
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
                    Thread.Sleep(50);
                }
            }

            PlaceDBManager dbManager = PlaceDBManager.Instance();
            if (LockedBoard == null)
                LockedBoard = dbManager.GetBoardStatus();

            return LockedBoard.Value;
        }

        // Bitmap -> ImageData (safe)
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


        private static bool LoadedLib = false; // TODO Do better


        public static Dictionary<ulong, byte> UserIdInfos = new Dictionary<ulong, byte>();

        [Command("usertest")]
        public async Task UserTest()
        {
            PlaceDBManager dbManager = PlaceDBManager.Instance();

            UserIdInfos = dbManager.GetPlaceUserIds();

            string text = "";
            foreach (var item in UserIdInfos)
            {
                text += $"{item.Key}|{item.Value}" + Environment.NewLine;

                if (text.Length > 1900)
                {
                    await Context.Channel.SendMessageAsync(text, false);
                    text = "";
                }
            }

            await Context.Channel.SendMessageAsync(text, false);
        }

        [Command("genchunk")]
        public async Task GenerateChunks()
        {
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

            UserIdInfos = dbManager.GetPlaceUserIds();

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
                    data[counter] = UserIdInfos[item.DiscordUserId];

                    counter += 1;
                }


                await Context.Channel.SendMessageAsync($"Saved {file}", false);
                File.WriteAllBytes(filePath, data);
            }

            await Context.Channel.SendMessageAsync($"Done", false);
        }

        [Command("help")]
        public async Task Help()
        {
            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("ETH DINFK Place");
            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");
            builder.WithDescription(@"Rules: There are none. However everything that is forbidden by the serverrules, is also forbidden to 'draw' on the board.

If you violate the server rules your pixels will be removed.
**LIVE Website: http://ethplace.spclr.ch:81/**");
            builder.WithColor(0, 255, 0);

            //builder.WithThumbnailUrl("https://avatars0.githubusercontent.com/u/11750584");

            var ownerUser = Program.Client.GetUser(Program.Owner);

            builder.AddField("Admin ONLY", "```.place lock <true|false>" + Environment.NewLine +
                ".place remove <userId> <x> <y> <xSize> <ySize> [<minutes>|1440]```");

            builder.AddField("Pixel verify (sends a 100x100 image for pixel verification) (45 sec cooldown)", "```.place pixelverify <x> <y>```");
            builder.AddField("Timelapse (Web View only)", "http://ethplace.spclr.ch:81/");
            builder.AddField("View full board (May contain outdated cache status)", "```.place view [(admin only) <force_load>]```");
            builder.AddField("Pixel history of a pixel/all", "```.place history <x> <y>" + Environment.NewLine +
                ".place history all```");
            builder.AddField("Zoom on a section (always up to date)", "```.place zoom <x> <y> <size>```");
            builder.AddField("Grid (help for navigation", "```.place grid" + Environment.NewLine +
                ".place grid <x> <y> <size>```");

            builder.AddField("Set single pixel", "```.place setpixel <x> <y> #<hex_color>```");
            builder.AddField("Set multiple pixel (user only) Min: 10 Max: 3'600", "```.place setmultiplepixels {<x> <y> #<hex_color>[|]}```");

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
                var color = System.Drawing.Color.FromArgb(pixel.R, pixel.G, pixel.B);
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

            /*if (LastRefresh.Add(TimeSpan.FromMinutes(10)) > DateTime.Now)
            {
                // cache is still new
                boardPixels = PixelsCache;
            }
            else
            {
                Context.Channel.SendMessageAsync("Cache miss. It will take a few seconds to refresh and generate.");

                boardPixels = dbManager.GetCurrentImage();
                PixelsCache = boardPixels;
                LastRefresh = DateTime.Now;
            }*/

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
                        R = color.R,
                        G = color.G,
                        B = color.B
                    });
                }
            }


            //boardPixels = boardPixels.Where(i => i.XPos >= x && i.XPos < x + size && i.YPos >= y && i.YPos < y + size).ToList();
            //boardPixels = boardPixels.OrderBy(i => i.XPos).OrderBy(i => i.YPos).ToList();

            var board = DrawingHelper.GetEmptyGraphics(boardSize + padding * 2, boardSize + padding * 2);
            watch.Stop();

            msDbTime = watch.ElapsedMilliseconds;

            watch.Restart();

            var redColor = System.Drawing.Color.FromArgb(255, 0, 0);
            var whiteColor = System.Drawing.Color.FromArgb(255, 255, 255);

            int minXText = -1;
            int minYText = -1;

            var pen = DrawingHelper.Pen_White;
            var font = DrawingHelper.LargerTextFont;
            var brush = DrawingHelper.SolidBrush_White;

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
                var color = System.Drawing.Color.FromArgb(pixel.R, pixel.G, pixel.B);
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
                                board.Graphics.DrawString(pixel.XPos.ToString(), font, brush, new System.Drawing.Point(padding + (pixel.XPos - x) * pixelSize + i - 5, 15));
                                board.Graphics.DrawString(pixel.XPos.ToString(), font, brush, new System.Drawing.Point(padding + (pixel.XPos - x) * pixelSize + i - 5, padding + boardSize + 10));

                                minXText = xZero;
                            }
                        }

                        if (yZero % step == 0 || pixel.YPos == y + size - 1)
                        {
                            // TODO on out of bounce this doesnt show

                            board.Bitmap.SetPixel(padding + (pixel.XPos - x) * pixelSize + i, padding + (pixel.YPos - y) * pixelSize + j, redColor);

                            if (minYText < yZero)
                            {
                                board.Graphics.DrawString(pixel.YPos.ToString(), font, brush, new System.Drawing.Point(5, padding + (pixel.YPos - y) * pixelSize + j - 5));
                                board.Graphics.DrawString(pixel.YPos.ToString(), font, brush, new System.Drawing.Point(padding + boardSize + 5, padding + (pixel.YPos - y) * pixelSize + j - 5));

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
            //return;
            try
            {
                int textPadding = 50;

                Stopwatch watch = new Stopwatch();
                watch.Start();

                PlaceDBManager dbManager = PlaceDBManager.Instance();

                IEnumerable<PlaceBoardHistory> pixelHistory;

                List<ulong> userIds = new List<ulong>();

                foreach (var item in socketUsers)
                    userIds.Add(item.Id);

                if (size < 0)
                {
                    // no zoom
                    size = 1000;
                    pixelHistory = dbManager.GetBoardHistory(userIds).OrderBy(i => i.SnowflakeTimePlaced);
                    x = 0;
                    y = 0;
                }
                else
                {
                    if (size < 10)
                        size = 10; // 10 is min size

                    pixelHistory = dbManager.GetBoardHistory(x, y, size, userIds).OrderBy(i => i.SnowflakeTimePlaced);
                }


                int secs = 30;
                int imagesPerSec = 60; // 60fps?

                int frames = secs * imagesPerSec;

                ulong step = (pixelHistory.Last().SnowflakeTimePlaced - pixelHistory.First().SnowflakeTimePlaced) / (ulong)frames;

                ulong last = 0;

                if (!Directory.Exists("TimelapseOutput"))
                    Directory.CreateDirectory("TimelapseOutput");

                /*
                var files = Directory.GetFiles("TimelapseOutput");
                if (files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        //File.Delete(file);
                    }
                }*/

                int pixelSize = 1_000 / size;

                int boardSizeVal = pixelSize * size;

                int frameCounter = 0;

                var board = DrawingHelper.GetEmptyGraphics(boardSizeVal, boardSizeVal + textPadding);
                var boardSize = new System.Drawing.Size(boardSizeVal, boardSizeVal + textPadding);





                var pen = DrawingHelper.Pen_White;
                var font = DrawingHelper.TitleFont;
                var brush = DrawingHelper.SolidBrush_White;

                List<ulong> users = new System.Collections.Generic.List<ulong>();
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
                        if (!users.Contains(history.DiscordUserId))
                            users.Add(history.DiscordUserId);

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


                        if (last + step < history.SnowflakeTimePlaced)
                        {
                            // generate a new frame
                            last = history.SnowflakeTimePlaced;


                            text = $"{SnowflakeUtils.FromSnowflake(last).ToString("yyyy-MM-dd HH:mm:ss")} PixelsPlaced: {pixelCount.ToString("N0")} Users participated: {users.Count.ToString("N0")}";

                            frame = CopyAndDrawOnBitmap(board.Bitmap, text, 10, boardSize.Height - textPadding + 5, boardSize);

                            file.Video.AddFrame(ToImageData(frame));


                            //board.Graphics.DrawString($"{SnowflakeUtils.FromSnowflake(last).ToString()}", font, brush, new System.Drawing.Point(40, 40));


                            //board.Bitmap.Save(Path.Combine("Timelapse", $"{fileName}{frameCounter.ToString("D6")}.png"));
                            frameCounter++;
                        }
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
                    CurrentPlaceBitmap.SetPixel(pixel.XPos, pixel.YPos, System.Drawing.Color.FromArgb(pixel.R, pixel.G, pixel.B));

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
                await Context.Channel.SendFileAsync(stream, "place.png", $"DB Time: {msDbTime}ms Web Viewer: http://ethplace.spclr.ch:81/ (dev)");
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
                var color = System.Drawing.Color.FromArgb(pixel.R, pixel.G, pixel.B);
                var stringHex = ColorTranslator.ToHtml(color);

                if (all?.ToLower() == "all")
                    messageText += $"<@{pixel.DiscordUserId}> placed ({stringHex}) at {pixel.XPos}/{pixel.YPos} {SnowflakeUtils.FromSnowflake(pixel.SnowflakeTimePlaced).ToString("MM.dd HH:mm:ss")} {Environment.NewLine}"; // todo check for everyone or here
                else
                    messageText += $"<@{pixel.DiscordUserId}> placed ({stringHex}) at {SnowflakeUtils.FromSnowflake(pixel.SnowflakeTimePlaced).ToString("MM.dd HH:mm")} {Environment.NewLine}"; // todo check for everyone or here

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
            if (size > 250 || size < 10)
            {
                await Context.Channel.SendMessageAsync("Size can only be between 10 and 250");
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


            var redColor = System.Drawing.Color.FromArgb(255, 0, 0);
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
                                board.Bitmap.SetPixel(i, j, redColor);
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
                var color = System.Drawing.Color.FromArgb(pixel.R, pixel.G, pixel.B);
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

            var isBot = Context.Message.Author.IsBot;
            ulong userId = Context.Message.Author.Id;

            // prevent people placing multipixels from placing single pixels
            if (!isBot)
            {
                // Verify if the current user is locked
                if (MultiPlacement.ContainsKey(userId) && MultiPlacement[userId] > DateTime.UtcNow)
                {
                    if (new Random().Next(0, 25) % 25 == 0)
                        Context.Channel.SendMessageAsync($"You think you are being clever huh?");

                    return;
                }
            }

            if (colorString.IndexOf('#') == -1)
                colorString = "#" + colorString;

            System.Drawing.Color color = ColorTranslator.FromHtml(colorString);

            PlaceDBManager dbManager = PlaceDBManager.Instance();

            if (LastStatusRefresh.Add(TimeSpan.FromSeconds(60)) < DateTime.Now)
            {
                LastStatusRefresh = DateTime.Now;
                var totalPixelsPlaced = dbManager.GetBoardHistoryCount();
                await Program.Client.SetGameAsync($"{totalPixelsPlaced:N0} pixels", null, ActivityType.Watching);
                RefreshBoard(10);

                //if (DateTime.Now.Minute == 0)
                //{
                // refresh the db users incase any new
                UserIdInfos = dbManager.GetPlaceUserIds();
                //}
            }

            var successfull = dbManager.PlacePixel(x, y, color, userId);

            if (!isBot && successfull)
            {
                Context.Channel.SendMessageAsync($"Placed {color.R}.{color.G}.{color.B} on X: {x} Y: {y}");
            }
        }

        public static Dictionary<ulong, DateTimeOffset> MultiPlacement = new Dictionary<ulong, DateTimeOffset>();

        public static DateTime LastStatusRefresh = DateTime.MinValue;

        [Command("setmultiplepixels")]
        public async Task PlaceMultipleColor([Remainder] string input = "")
        {
            if (IsPlaceLocked() || Context.Message.Author.IsBot)
            {
                // 1 in 5 send a message
                if (!Context.Message.Author.IsBot && new Random().Next(0, 5) % 5 == 0)
                {
                    await Context.Channel.SendMessageAsync("Board is locked");
                }

                return;
            }

            PlaceDBManager dbManager = PlaceDBManager.Instance();
            if (LastStatusRefresh.Add(TimeSpan.FromSeconds(30)) < DateTime.Now)
            {
                LastStatusRefresh = DateTime.Now;
                var totalPixelsPlaced = dbManager.GetBoardHistoryCount();
                await Program.Client.SetGameAsync($"{totalPixelsPlaced:N0} pixels", null, ActivityType.Watching);
            }

            try
            {
                ulong userId = Context.Message.Author.Id;

                // Verify if the current user is locked
                if (MultiPlacement.ContainsKey(userId) && MultiPlacement[userId] > DateTime.UtcNow)
                {
                    // User is still in lock mode -> cancel
                    //Context.Channel.SendMessageAsync($"Rejected");
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
                            int lines = download.Split('|').Count();

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
                    Context.Channel.SendMessageAsync($"REJECTED TOO_FEW {userId}");
                    return;
                }

                if (instructions.Count > 3_600)
                {
                    Context.Channel.SendMessageAsync($"REJECTED TOO_MANY {userId}");
                    return;
                }

                var blockUntil = DateTime.UtcNow.AddSeconds(instructions.Count); // 1 per second

                if (MultiPlacement.ContainsKey(userId))
                    MultiPlacement[userId] = blockUntil;
                else
                    MultiPlacement.Add(userId, blockUntil);

                Context.Channel.SendMessageAsync($"ACCEPTED {instructions.Count} <@{userId}>");
                foreach (var item in instructions)
                {
                    var delay = Task.Delay(1000);
                    var commands = item.Split(' ');

                    short x = short.Parse(commands[0]);
                    short y = short.Parse(commands[1]);

                    System.Drawing.Color color = ColorTranslator.FromHtml(commands[2]);

                    dbManager.PlacePixel(x, y, color, Context.Message.Author.Id);

                    await delay; // ensure 1 placement / sec
                }

                //if (!Context.Message.Author.IsBot)
                //{
                Context.Channel.SendMessageAsync($"DONE <@{userId}>");
                //}
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message);
            }
        }
    }
}
