using ETHDINFKBot.Helpers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Drawing
{
    public class DrawTable
    {
        //private Font DefaultFont = DrawingHelper.LargerTextFont;
        //private Brush DefaultBrush = DrawingHelper.SolidBrush_White;
        //private Pen DefaultPen = DrawingHelper.Pen_White;

        private List<string> Header;
        private List<List<string>> Data;
        private string AdditionalString;
        public DrawTable(List<string> header, List<List<string>> data, string additionalString)
        {
            Header = header;
            Data = data;
            AdditionalString = additionalString;
        }

        public async Task<Stream> GetImage()
        {
            return await GetQueryResultImage();
        }

        // start drawing TODO move to drawing lib
        private int DrawRow(SKCanvas canvas, List<string> row, int padding, int currentHeight, List<int> widths)
        {
            float highestSize = 0;
            int currentWidthStart = padding;
            for (int i = 0; i < row.Count; i++)
            {
                int offsetX = currentWidthStart;
                int cellWidth = widths.ElementAt(i);

                string text = row.ElementAt(i);

                SKRect headerDestRect = new SKRect(offsetX, currentHeight, cellWidth, 500);

                var size = new SKSize();
                try
                {
                    // TODO Correct measurement
                    size = new SKSize(); // SKPaint.MeasureText(text);
                    size.Height = 25;
                    size.Width = 300;
                }
                catch (Exception ex)
                {
                    //Context.Channel.SendMessageAsync("debug: " + text);
                    // todo log the text for future bugfix
                    text = Encoding.ASCII.GetString(Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback(string.Empty), new DecoderExceptionFallback()), Encoding.UTF8.GetBytes(text)));

                    //Context.Channel.SendMessageAsync("debug2: " + text);
                    // recalculate the size again
                    // TODO Correct measurement
                    //size = g.MeasureString(text, DefaultFont, new SizeF(cellWidth, 500), null);
                }

                if (size.Height > highestSize)
                    highestSize = size.Height;

                //g.DrawRectangle(Pens.Red, headerDestRect);
              
                // TODO likely wrong recalculate
                canvas.DrawText(text, offsetX, cellWidth, DrawingHelper.DefaultTextPaint);
                
                currentWidthStart += cellWidth;
            }

            //currentHeight += (int)highestSize + padding / 5;

            currentWidthStart = padding;
            for (int i = 0; i < row.Count; i++)
            {
                int offsetX = currentWidthStart;
                int cellWidth = widths.ElementAt(i);
                SKRect headerDestRect = new SKRect(offsetX, padding, cellWidth, (int)highestSize + 1);
                //g.DrawRectangle(Pens.Red, headerDestRect);

                currentWidthStart += cellWidth;
            }

            return (int)highestSize;
        }

        private List<int> DefineTableCellWidths(int normalCellWidth)
        {
            var bitmap = new SKBitmap(2000, 2000); // TODO insert into constructor
            SKCanvas canvas = new SKCanvas(bitmap);

            canvas.Clear(DrawingHelper.DiscordBackgroundColor);

            // a cell can be max 1000 pixels wide
            var maxSize = new SKSize(1000, 1000);

            int[] maxWidthNeeded = new int[Header.Count];

            // the minimum size is the header text size
            for (int i = 0; i < Header.Count; i++)
            {
                var size = new SKSizeI(250, 20);// TODO g.MeasureString(Header.ElementAt(i), DefaultFont, maxSize, null);
                maxWidthNeeded[i] = (int)size.Width + 10;
            }

            // find the max column size in the content
            for (int i = 0; i < maxWidthNeeded.Length; i++)
            {
                foreach (var row in Data)
                {
                    var size = new SKSizeI(250, 20);// TODO g.MeasureString(row.ElementAt(i), DefaultFont, maxSize, null);

                    int currentWidth = (int)size.Width + 10;

                    if (maxWidthNeeded[i] < currentWidth)
                        maxWidthNeeded[i] = currentWidth;
                }
            }
            // find columns that need the flex property
            List<int> flexColumns = new List<int>();
            int freeRoom = 0;
            int flexContent = 0;
            for (int i = 0; i < maxWidthNeeded.Length; i++)
            {
                if (maxWidthNeeded[i] > normalCellWidth)
                {
                    flexColumns.Add(i);
                    flexContent += maxWidthNeeded[i] - normalCellWidth; // only the oversize
                }
                else
                {
                    freeRoom += normalCellWidth - maxWidthNeeded[i];
                }
            }


            if (flexColumns.Count == 0)
            {
                // no columns need flex so we distribute all even
                for (int i = 0; i < maxWidthNeeded.Length; i++)
                    maxWidthNeeded[i] = normalCellWidth;
            }
            else
            {
                // we need to distribute the free room over the flexContent by %
                foreach (var column in flexColumns)
                {
                    float percentNeeded = (maxWidthNeeded[column] - normalCellWidth) / flexContent;
                    float gettingFreeSpace = freeRoom * percentNeeded;
                    maxWidthNeeded[column] = normalCellWidth + (int)gettingFreeSpace;
                }
            }

            canvas.Dispose();
            bitmap.Dispose();

            return maxWidthNeeded.ToList();
        }

        private async Task<Stream> GetQueryResultImage()
        {
            Stopwatch watchDraw = new Stopwatch();
            watchDraw.Start();


            // todo make dynamic 
            int width = 1920;
            int height = 10000;

            SKBitmap bitmap = new SKBitmap(width, height); // TODO insert into constructor
            SKCanvas canvas = new SKCanvas(bitmap);

            canvas.Clear(DrawingHelper.DiscordBackgroundColor);

            int padding = 20;

            int xSize = width - padding * 2;

            if (Header.Count == 0)
            {
                // TODO draw no results on image
                //await Context.Channel.SendMessageAsync("No results", false, null, null, null, new Discord.MessageReference(Context.Message.Id));
                return null;
            }

            int cellWidth = xSize / Header.Count;

            int currentHeight = padding;

            List<int> widths = DefineTableCellWidths(cellWidth);

            string cellWithInfo = "normal" + cellWidth + " " + string.Join(", ", widths);

            //await Context.Channel.SendMessageAsync(cellWithInfo, false, null, null, null, new Discord.MessageReference(Context.Message.Id));
            currentHeight += DrawRow(canvas, Header, padding, currentHeight, widths);

            int failedDrawLineCount = 0;
            foreach (var row in Data)
            {
                try
                {
                    canvas.DrawLine(padding, currentHeight, Math.Max(width - padding, 0), currentHeight, DrawingHelper.DefaultDrawing);
                }
                catch (Exception ex)
                {
                    failedDrawLineCount++;
                }
                try
                {
                    currentHeight += DrawRow(canvas, row, padding, currentHeight, widths);
                }
                catch (Exception ex)
                {
                    // TODO send exception
                    //Context.Channel.SendMessageAsync(ex.ToString());
                    break;
                }
            }

            try
            {
                canvas.DrawLine(padding, currentHeight, Math.Max(width - padding, 0), currentHeight, DrawingHelper.DefaultDrawing);
            }
            catch (Exception ex)
            {
                failedDrawLineCount++;
            }

            if (failedDrawLineCount > 0)
            {
                //Context.Channel.SendMessageAsync($"Failed to draw {failedDrawLineCount} lines, widths: {string.Join(",", widths)}");
            }

            watchDraw.Stop();
            //
            canvas.DrawText($"{AdditionalString} DrawTime: {watchDraw.ElapsedMilliseconds.ToString("N0")}ms",
                new SKPoint(padding, currentHeight + padding),
                DrawingHelper.TitleTextPaint); // TODO Different color for text

            //List<int> rowHeight = new List<int>();
            //Rectangle DestinationRectangle = new Rectangle(10, 10, cellWidth, 500);

            //var size = Graphics.MeasureCharacterRanges("", drawFont2, DestinationRectangle, null);
            //Graphics.DrawString($"{(int)((maxValue / yNum) * i)}", drawFont2, b, new Point(40, 10 + ySize - (ySize / yNum) * i));

            bitmap = DrawingHelper.CropImage(bitmap, new SKRect(0, 0, 1920, currentHeight + padding * 3));

            var stream = CommonHelper.GetStream(bitmap);
            bitmap.Dispose();
            canvas.Dispose();
            return stream;
        }

        // end drawing
    }
}
