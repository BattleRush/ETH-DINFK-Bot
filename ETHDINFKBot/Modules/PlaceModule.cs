using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ETHBot.DataLayer.Data.Fun;
using ETHDINFKBot.Data;
using ETHDINFKBot.Drawing;
using ETHDINFKBot.Helpers;
using FFMediaToolkit;
using FFMediaToolkit.Encoding;
using FFMediaToolkit.Graphics;
using ImageMagick;
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
using System.Text;
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
        public static List<PlaceBoardPixel> PixelsCache = new List<PlaceBoardPixel>();
        public static DateTime LastRefresh = DateTime.MinValue;

        public static bool? LockedBoard = null;

        private bool IsPlaceLocked()
        {
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


        // FOR HEATMAP BUT ITS WAY TO SLOW

        //http://dylanvester.com/2015/10/creating-heat-maps-with-net-20-c-sharp/
        private Bitmap CreateIntensityMask(Bitmap bSurface, List<PlacePixel> aHeatPoints, ulong time)
        {
            // Create new graphics surface from memory bitmap
            Graphics DrawSurface = Graphics.FromImage(bSurface);
            // Set background color to white so that pixels can be correctly colorized
            DrawSurface.Clear(System.Drawing.Color.White);
            // Traverse heat point data and draw masks for each heat point
            foreach (PlacePixel DataPoint in aHeatPoints)
            {
                // Render current heat point on draw surface
                DrawHeatPoint(DrawSurface, DataPoint, 2, time);
            }
            return bSurface;
        }
        private void DrawHeatPoint(Graphics Canvas, PlacePixel HeatPoint, int Radius, ulong time)
        {
            // Create points generic list of points to hold circumference points
            List<System.Drawing.Point> CircumferencePointsList = new List<System.Drawing.Point>();
            // Create an empty point to predefine the point struct used in the circumference loop
            System.Drawing.Point CircumferencePoint;
            // Create an empty array that will be populated with points from the generic list
            System.Drawing.Point[] CircumferencePointsArray;
            // Calculate ratio to scale byte intensity range from 0-255 to 0-1
            float fRatio = 1F / Byte.MaxValue;
            // Precalulate half of byte max value
            byte bHalf = Byte.MaxValue / 2;
            // Flip intensity on it's center value from low-high to high-low
            int iIntensity = (byte)(HeatPoint.Intensity(time) - ((HeatPoint.Intensity(time) - bHalf) * 2));
            // Store scaled and flipped intensity value for use with gradient center location
            float fIntensity = iIntensity * fRatio;
            // Loop through all angles of a circle
            // Define loop variable as a double to prevent casting in each iteration
            // Iterate through loop on 10 degree deltas, this can change to improve performance
            for (double i = 0; i <= 360; i += 10)
            {
                // Replace last iteration point with new empty point struct
                CircumferencePoint = new System.Drawing.Point();
                // Plot new point on the circumference of a circle of the defined radius
                // Using the point coordinates, radius, and angle
                // Calculate the position of this iterations point on the circle
                CircumferencePoint.X = Convert.ToInt32(HeatPoint.XPos + Radius * Math.Cos(ConvertDegreesToRadians(i)));
                CircumferencePoint.Y = Convert.ToInt32(HeatPoint.YPos + Radius * Math.Sin(ConvertDegreesToRadians(i)));
                // Add newly plotted circumference point to generic point list
                CircumferencePointsList.Add(CircumferencePoint);
            }
            // Populate empty points system array from generic points array list
            // Do this to satisfy the datatype of the PathGradientBrush and FillPolygon methods
            CircumferencePointsArray = CircumferencePointsList.ToArray();
            // Create new PathGradientBrush to create a radial gradient using the circumference points
            PathGradientBrush GradientShaper = new PathGradientBrush(CircumferencePointsArray);
            // Create new color blend to tell the PathGradientBrush what colors to use and where to put them
            ColorBlend GradientSpecifications = new ColorBlend(3);
            // Define positions of gradient colors, use intesity to adjust the middle color to
            // show more mask or less mask
            GradientSpecifications.Positions = new float[3] { 0, fIntensity, 1 };
            // Define gradient colors and their alpha values, adjust alpha of gradient colors to match intensity
            GradientSpecifications.Colors = new System.Drawing.Color[3]
            {
                System.Drawing.Color.FromArgb(0, System.Drawing.Color.White),
                System.Drawing.Color.FromArgb(HeatPoint.Intensity(time), System.Drawing.Color.Black),
                System.Drawing.Color.FromArgb(HeatPoint.Intensity(time), System.Drawing.Color.Black)
            };
            // Pass off color blend to PathGradientBrush to instruct it how to generate the gradient
            GradientShaper.InterpolationColors = GradientSpecifications;
            // Draw polygon (circle) using our point array and gradient brush
            Canvas.FillPolygon(GradientShaper, CircumferencePointsArray);
        }
        private static ColorMap[] CreatePaletteIndex(byte Alpha)
        {
            ColorMap[] OutputMap = new ColorMap[256];
            // Change this path to wherever you saved the palette image.
            Bitmap Palette = (Bitmap)Bitmap.FromFile(@"C:\Github\BattleRush\ETH-DINFK-Bot\ETHDINFKBot\bin\Debug\net5.0\Images\intensity-mask.jpg");
            // Loop through each pixel and create a new color mapping
            for (int X = 0; X <= 255; X++)
            {
                OutputMap[X] = new ColorMap();
                OutputMap[X].OldColor = System.Drawing.Color.FromArgb(X, X, X);
                OutputMap[X].NewColor = System.Drawing.Color.FromArgb(Alpha, Palette.GetPixel(X, 0));
            }
            return OutputMap;
        }
        public static Bitmap Colorize(Bitmap Mask, byte Alpha)
        {
            // Create new bitmap to act as a work surface for the colorization process
            Bitmap Output = new Bitmap(Mask.Width, Mask.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            // Create a graphics object from our memory bitmap so we can draw on it and clear it's drawing surface
            Graphics Surface = Graphics.FromImage(Output);
            Surface.Clear(System.Drawing.Color.Transparent);
            // Build an array of color mappings to remap our greyscale mask to full color
            // Accept an alpha byte to specify the transparancy of the output image
            ColorMap[] Colors = CreatePaletteIndex(Alpha);
            // Create new image attributes class to handle the color remappings
            // Inject our color map array to instruct the image attributes class how to do the colorization
            ImageAttributes Remapper = new ImageAttributes();
            Remapper.SetRemapTable(Colors);
            // Draw our mask onto our memory bitmap work surface using the new color mapping scheme
            Surface.DrawImage(Mask, new System.Drawing.Rectangle(0, 0, Mask.Width, Mask.Height), 0, 0, Mask.Width, Mask.Height, GraphicsUnit.Pixel, Remapper);
            // Send back newly colorized memory bitmap
            return Output;
        }
        private double ConvertDegreesToRadians(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }

        [Command("test")]
        public async Task test()
        {
            return;
            PlaceBoard board = new PlaceBoard();

            PlaceDBManager dbManager = PlaceDBManager.Instance();
            var pixelHistory = dbManager.GetBoardHistory(null).OrderBy(i => i.SnowflakeTimePlaced);

            int secs = 80;
            int imagesPerSec = 60; // 60fps?

            int frames = secs * imagesPerSec;

            ulong step = (pixelHistory.Last().SnowflakeTimePlaced - pixelHistory.First().SnowflakeTimePlaced) / (ulong)frames;

            ulong last = 0;


            int frameCounter = 0;


            if (!LoadedLib)
            {
                // TODO dont hardcode
                FFmpegLoader.FFmpegPath = @"C:\Github\BattleRush\ETH-DINFK-Bot\ETHDINFKBot\bin\Debug\net5.0\ffmpeg\x86_64";
            }
            LoadedLib = true;

            // You can set there codec, bitrate, frame rate and many other options.
            var settings = new VideoEncoderSettings(width: 1000, height: 1000, framerate: imagesPerSec, codec: VideoCodec.H264);
            settings.EncoderPreset = EncoderPreset.Fast;

            settings.CRF = 17;
            using (var file = MediaBuilder.CreateContainer(Path.Combine(Directory.GetCurrentDirectory(), "TimelapseOutput", "heatmap.mp4")).WithVideo(settings).Create())
            {
                try
                {
                    // Create new memory bitmap the same size as the picture box
                    Bitmap bMap = new Bitmap(1000, 1000, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                    var frame = CreateIntensityMask(bMap, board.Pixels, last);
                    frame = Colorize(frame, 255);
                    //var frame2 = CopyAndDrawOnBitmap(frame, "", 0, 0, new System.Drawing.Size(1000, 1000));

                    file.Video.AddFrame(ToImageData(frame));




                    //board.Bitmap.Save(Path.Combine("Timelapse", $"{fileName}{frameCounter.ToString("D6")}.png"));
                    //frameCounter++;


                    foreach (var history in pixelHistory)
                    {


                        var color = System.Drawing.Color.FromArgb(history.R, history.G, history.B);
                        //board.Bitmap.SetPixel(history.XPos - x, history.YPos - y, color);

                        board.AddPixel(history);


                        if (last + step < history.SnowflakeTimePlaced)
                        {
                            // generate a new frame
                            last = history.SnowflakeTimePlaced;

                            bMap = new Bitmap(1000, 1000, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                            frame = CreateIntensityMask(bMap, board.Pixels, last);
                            frame = Colorize(frame, 255);
                            //var frame2 = CopyAndDrawOnBitmap(frame, "", 0, 0, new System.Drawing.Size(1000, 1000));

                            file.Video.AddFrame(ToImageData(frame));




                            //board.Graphics.DrawString($"{SnowflakeUtils.FromSnowflake(last).ToString()}", font, brush, new System.Drawing.Point(40, 40));


                            //board.Bitmap.Save(Path.Combine("Timelapse", $"{fileName}{frameCounter.ToString("D6")}.png"));
                            frameCounter++;
                        }
                    }


                    //text = $"{SnowflakeUtils.FromSnowflake(last).ToString("yyyy-MM-dd HH:mm:ss")} PixelsPlaced: {pixelCount.ToString("N0")} Users participated: {users.Count.ToString("N0")}";

                    //frame = CopyAndDrawOnBitmap(board.Bitmap, text, 10, boardSize.Height - textPadding + 5, boardSize);

                    // still image for 2 sec 
                    //for (int i = 0; i < imagesPerSec * 2; i++)
                    //{
                    //    file.Video.AddFrame(ToImageData(frame));
                    //}



                    //board.Graphics.DrawString($"{SnowflakeUtils.FromSnowflake(last).ToString()}", font, brush, new System.Drawing.Point(40, 40));


                    // just the final image
                    //board.Bitmap.Save(Path.Combine("Timelapse", $"{fileName}{frameCounter.ToString("D6")}.png"));
                    frameCounter++;

                }
                catch (Exception ex)
                {

                }

            }

        }

        // END HEATMAP BUT ITS WAY TO SLOW


        [Command("help")]
        public async Task Help()
        {
            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("ETH DINFK Place");
            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");
            builder.WithDescription(@"Rules: There are none. However everything that is forbidden by the serverrules, is also forbidden to 'draw' on the board.

If you violate the server rules your pixels will be removed.");
            builder.WithColor(0, 255, 0);

            //builder.WithThumbnailUrl("https://avatars0.githubusercontent.com/u/11750584");

            var ownerUser = Program.Client.GetUser(Program.Owner);

            builder.AddField("Admin ONLY", "```.place lock <true|false>" + Environment.NewLine +
                ".place remove <userId> <x> <y> <xSize> <ySize> [<minutes>|1440]```");

            builder.AddField("Pixel verify (sends a 100x100 image for pixel verification) (45 sec cooldown)", "```.place pixelverify <x> <y>```");
            builder.AddField("Timelapse (admin only for size > 250) (DISABLED UNTIL FFMPEG WORKS xD)", "```.place timelapse (admin only)" + Environment.NewLine +
                ".place timelapse {<@user>} (admin only)" + Environment.NewLine +
                ".place timelapse <x> <y> <size>" + Environment.NewLine +
                ".place timelapse <x> <y> <size> {<@user>} ```");
            builder.AddField("View full board (May contain outdated cache status)", "```.place view [(admin only) <force_load>]```");
            builder.AddField("Pixel history of a pixel/all", "```.place history <x> <y>" + Environment.NewLine +
                ".place history all```");
            builder.AddField("Zoom on a section (always up to date)", "```.place zoom <x> <y> <size>```");
            builder.AddField("Grid (help for navigation", "```.place grid" + Environment.NewLine +
                ".place grid <x> <y> <size>```");

            builder.AddField("Set single pixel", "```.place setpixel <x> <y> #<hex_color>```");
            builder.AddField("Set multiple pixel (user only) Min. 10", "```.place setmultiplepixels {<x> <y> #<hex_color>[|]}```");

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

            if (x + size > 1000 || y + size > 1000 || x < 0 || y < 0 || size < 50 || size > 1000)
            {
                await Context.Channel.SendMessageAsync("Size can only be between 50 and 1000 or out of bounds.");
                return;
            }

            long msDbTime = -1;

            Stopwatch watch = new Stopwatch();
            watch.Start();

            PlaceDBManager dbManager = PlaceDBManager.Instance();
            int pixelSize = 1_000 / size;

            int boardSize = pixelSize * size;

            int step = size / 10;


            List<PlaceBoardPixel> boardPixels;

            if (LastRefresh.Add(TimeSpan.FromMinutes(10)) > DateTime.Now)
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
            }

            boardPixels = boardPixels.Where(i => i.XPos >= x && i.XPos < x + size && i.YPos >= y && i.YPos < y + size).ToList();
            boardPixels = boardPixels.OrderBy(i => i.XPos).OrderBy(i => i.YPos).ToList();

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


                long msDbTime = -1;
                Stopwatch watch = new Stopwatch();
                watch.Start();

                PlaceDBManager dbManager = PlaceDBManager.Instance();

                List<PlaceBoardPixel> boardPixels;

                if (LastRefresh.Add(TimeSpan.FromMinutes(10)) > DateTime.Now && !forceReload)
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
                }

                watch.Stop();

                msDbTime = watch.ElapsedMilliseconds;

                watch.Restart();

                watch.Restart();
                var board = DrawingHelper.GetEmptyGraphics(1000, 1000);

                int size = boardPixels.Count;
                var array = new PlaceBoardPixel[size];

                boardPixels.CopyTo(0, array, 0, size);

                foreach (var pixel in array)
                {
                    var color = System.Drawing.Color.FromArgb(pixel.R, pixel.G, pixel.B);
                    board.Bitmap.SetPixel(pixel.XPos, pixel.YPos, color);
                }

                watch.Stop();

                // TODO Dispose stuff
                var stream = CommonHelper.GetStream(board.Bitmap);
                await Context.Channel.SendFileAsync(stream, "place.png", $"DB Time: {msDbTime}ms Draw Time: {watch.ElapsedMilliseconds}ms");
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

        [Command("zoom")]
        public async Task ZoomIntoTheBoard(int x, int y, int size = 1000)
        {
            if (size > 500 || size < 10)
            {
                await Context.Channel.SendMessageAsync("Size can only be between 10 and 500");
                return;
            }

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

            // prevent people placing multipixels from placing single pixels
            if (!Context.Message.Author.IsBot)
            {
                ulong userId = Context.Message.Author.Id;

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

            if (LastStatusRefresh.Add(TimeSpan.FromSeconds(30)) < DateTime.Now)
            {
                LastStatusRefresh = DateTime.Now;
                var totalPixelsPlaced = dbManager.GetBoardHistoryCount();
                await Program.Client.SetGameAsync($"{totalPixelsPlaced:N0} pixels", null, ActivityType.Watching);
            }

            var successfull = dbManager.PlacePixel(x, y, color.R, color.G, color.B, Context.Message.Author.Id);

            if (!Context.Message.Author.IsBot && successfull)
            {
                Context.Channel.SendMessageAsync($"Placed {color.R}.{color.G}.{color.B} on X: {x} Y: {y}");
            }
        }

        public static Dictionary<ulong, DateTimeOffset> MultiPlacement = new Dictionary<ulong, DateTimeOffset>();

        public static DateTime LastStatusRefresh = DateTime.MinValue;

        [Command("setmultiplepixels")]
        public async Task PlaceMultipleColor([Remainder] string input)
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

                input = input.Trim();

                List<string> instructions = input.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();

                if (instructions.Count < 10)
                {
                    // reject if less than 10 instructions queued
                    //Context.Channel.SendMessageAsync($"REJECTED TOO_FEW {userId}");
                    return;
                }

                var blockUntil = DateTime.UtcNow.AddSeconds(instructions.Count); // 1 per second

                if (MultiPlacement.ContainsKey(userId))
                    MultiPlacement[userId] = blockUntil;
                else
                    MultiPlacement.Add(userId, blockUntil);

                Context.Channel.SendMessageAsync($"ACCEPTED <@{userId}>");
                foreach (var item in instructions)
                {
                    var delay = Task.Delay(1000);
                    var commands = item.Split(' ');

                    short x = short.Parse(commands[0]);
                    short y = short.Parse(commands[1]);

                    System.Drawing.Color color = ColorTranslator.FromHtml(commands[2]);

                    dbManager.PlacePixel(x, y, color.R, color.G, color.B, Context.Message.Author.Id);

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
