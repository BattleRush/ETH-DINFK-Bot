using ETHDINFKBot.Helpers;
using ETHDINFKBot.Struct;
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
        private List<TableRowInfo> TableRowInfo;
        private int Width;
        public DrawTable(List<string> header, List<List<string>> data, string additionalString, List<TableRowInfo> tableRowInfo, int width = 1920)
        {
            Header = header;
            Data = data;
            AdditionalString = additionalString;
            TableRowInfo = tableRowInfo;
            Width = width;
        }

        public async Task<Stream> GetImage()
        {
            return await GetQueryResultImage();
        }

        // https://github.com/mono/SkiaSharp.Extended/issues/12
        private float DrawTextArea(SKCanvas canvas, SKPaint paint, float x, float y, float maxWidth, float lineHeight, string text)
        {
            var spaceWidth = paint.MeasureText(" ");
            var lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            lines = lines.SelectMany(l => SplitLine(paint, maxWidth, l, spaceWidth)).ToArray();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                canvas.DrawText(line, x, y, paint);
                y += lineHeight;
            }

            return y;
        }

        private string[] SplitLine(SKPaint paint, float maxWidth, string text, float spaceWidth)
        {
            var result = new List<string>();

            var words = text.Split(new[] { " " }, StringSplitOptions.None);

            var line = new StringBuilder();
            float width = 0;
            foreach (var word in words)
            {
                var wordWidth = paint.MeasureText(word);
                var wordWithSpaceWidth = wordWidth + spaceWidth;
                var wordWithSpace = word + " ";

                if (width + wordWidth > maxWidth)
                {
                    result.Add(line.ToString());
                    line = new StringBuilder(wordWithSpace);
                    width = wordWithSpaceWidth;
                }
                else
                {
                    line.Append(wordWithSpace);
                    width += wordWithSpaceWidth;
                }
            }

            result.Add(line.ToString());

            return result.ToArray();
        }


        // start drawing TODO move to drawing lib
        private int DrawRow(SKCanvas canvas, List<string> row, int rowId, int padding, int currentHeight, List<int> widths, bool isTitle = false)
        {
            float highestSize = 0;
            int currentWidthStart = padding;
            for (int i = 0; i < row.Count; i++)
            {
                int cellWidth = widths.ElementAt(i);

                string text = row.ElementAt(i);

                var paint = isTitle ? DrawingHelper.TitleTextPaint : DrawingHelper.DefaultTextPaint;

                var tableInfo = TableRowInfo?.SingleOrDefault(i => i.RowId == rowId);
                if (tableInfo?.Cells != null)
                {
                    if (tableInfo.Value.Cells?.Any(c => c.ColumnId == i) ?? false)
                    {
                        var cellInfo = tableInfo.Value.Cells.Single(c => c.ColumnId == i);
                        paint.Color = cellInfo.FontColor;
                    }
                }

                int usedHeight = (int)DrawTextArea(canvas, paint, currentWidthStart + 5, currentHeight, widths[i], paint.TextSize, text);

                if (usedHeight > highestSize)
                    highestSize = usedHeight;

                currentWidthStart += cellWidth;
            }

            //currentHeight = (int)highestSize + padding / 5;

            currentWidthStart = padding;

            for (int i = 0; i < row.Count; i++)
                currentWidthStart += widths.ElementAt(i);


            return (int)highestSize;
        }

        private List<int> DefineTableCellWidths(int normalCellWidth, SKPaint headerPaint, SKPaint normalPaint)
        {
            var bitmap = new SKBitmap(2000, 2000); // TODO insert into constructor
            SKCanvas canvas = new SKCanvas(bitmap);

            canvas.Clear(DrawingHelper.DiscordBackgroundColor);

            int[] maxWidthNeeded = new int[Header.Count];

            // the minimum size is the header text size
            for (int i = 0; i < Header.Count; i++)
            {
                var width = headerPaint.MeasureText(Header[i]);
                maxWidthNeeded[i] = (int)width + 10;
            }

            // find the max column size in the content
            for (int i = 0; i < maxWidthNeeded.Length; i++)
            {
                foreach (var row in Data)
                {
                    var width = normalPaint.MeasureText(row[i]);
                    int currentWidth = (int)width + 10;

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
                    float percentNeeded = (maxWidthNeeded[column] - normalCellWidth) / (float)flexContent;
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
            int width = Width;
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

            List<int> widths = DefineTableCellWidths(cellWidth, DrawingHelper.TitleTextPaint, DrawingHelper.DefaultTextPaint);

            string cellWithInfo = "normal" + cellWidth + " " + string.Join(", ", widths);

            //await Context.Channel.SendMessageAsync(cellWithInfo, false, null, null, null, new Discord.MessageReference(Context.Message.Id));
            currentHeight = DrawRow(canvas, Header, -1, padding, currentHeight + 10, widths, true);


            int failedDrawLineCount = 0;
            int rowId = 0;
            foreach (var row in Data)
            {
                try
                {
                    canvas.DrawLine(padding, currentHeight - 13, Math.Max(width - padding, 0), currentHeight - 13, DrawingHelper.DefaultDrawing);
                }
                catch (Exception ex)
                {
                    failedDrawLineCount++;
                }
                try
                {
                    currentHeight = DrawRow(canvas, row, rowId, padding, currentHeight, widths);
                    currentHeight += 5;
                }
                catch (Exception ex)
                {
                    // TODO send exception
                    //Context.Channel.SendMessageAsync(ex.ToString());
                    break;
                }

                rowId++;
            }

            try
            {
                canvas.DrawLine(padding, currentHeight - 13, Math.Max(width - padding, 0), currentHeight - 13, DrawingHelper.DefaultDrawing);
            }
            catch (Exception ex)
            {
                failedDrawLineCount++;
            }

            if (failedDrawLineCount > 0)
            {
                //Context.Channel.SendMessageAsync($"Failed to draw {failedDrawLineCount} lines, widths: {string.Join(",", widths)}");
            }

            int currentWidth = padding;
            foreach (var curWidth in widths)
            {
                canvas.DrawLine(currentWidth, padding - 5, currentWidth, currentHeight - 13, DrawingHelper.DefaultDrawing);
                currentWidth += curWidth;
            }

            canvas.DrawLine(currentWidth, padding - 5, currentWidth, currentHeight - 13, DrawingHelper.DefaultDrawing);

            watchDraw.Stop();
            //
            canvas.DrawText($"{AdditionalString} DrawTime: {watchDraw.ElapsedMilliseconds.ToString("N0")}ms",
                new SKPoint(padding, currentHeight + padding),
                DrawingHelper.TitleTextPaint); // TODO Different color for text

            //List<int> rowHeight = new List<int>();
            //Rectangle DestinationRectangle = new Rectangle(10, 10, cellWidth, 500);

            //var size = Graphics.MeasureCharacterRanges("", drawFont2, DestinationRectangle, null);
            //Graphics.DrawString($"{(int)((maxValue / yNum) * i)}", drawFont2, b, new Point(40, 10 + ySize - (ySize / yNum) * i));

            bitmap = DrawingHelper.CropImage(bitmap, new SKRect(0, 0, Width, currentHeight + padding * 3));

            var stream = CommonHelper.GetStream(bitmap);
            bitmap.Dispose();
            canvas.Dispose();
            return stream;
        }

        // end drawing
    }
}
