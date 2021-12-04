using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETHDINFKBot.Drawing
{
    public class GridSize
    {
        public int XMin { get; set; }
        public int XMax { get; set; }
        public int XSize { get; set; }

        public int YMin { get; set; }
        public int YMax { get; set; }
        public int YSize { get; set; }
        public GridSize()
        {

        }

        public GridSize(SKBitmap bitmap, Padding padding)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;

            XMin = padding.Left;
            XMax = width - padding.Right;
            XSize = XMax - XMin;

            YMin = height - padding.Bottom;
            YMax = padding.Top;
            YSize = YMin - YMax;
        }
    }

    public class Padding
    {
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
        public int Left { get; set; }
        public Padding()
        {

        }

        public Padding(int padding)
        {
            Top = padding;
            Right = padding;
            Bottom = padding;
            Left = padding;
        }

        public Padding(int top, int right, int bottom, int left)
        {
            Top = top;
            Right = right;
            Bottom = bottom;
            Left = left;
        }
    }

    // TODO move some points to point / line graph
    // TODO Draw rect consider uneven ints for rounding issues
    public static class DrawingHelper
    {
        public static (SKCanvas Canvas, SKBitmap Bitmap) GetEmptyGraphics(int width = 1920, int height = 1080)
        {
            var bitmap = new SKBitmap(width, height); // TODO see if needed format
            var graphics = new SKCanvas(bitmap);
            graphics.Clear(DrawingHelper.DiscordBackgroundColor);

            // TODO Verify if this is better

            //https://docs.microsoft.com/en-us/dotnet/api/skiasharp.skpaint?view=skiasharp-2.80.2
            //var info = new SKImageInfo(width, height);
            //using (var surface = SKSurface.Create(info))
            //{
            //    SKCanvas canvas = surface.Canvas;
            //}

            return (graphics, bitmap);
        }


        // TODO duplicate params similar to GetPoints
        // TODO call other get labels
        public static (List<string> XAxisLables, List<string> YAxisLabels) GetLabels(Dictionary<DateTime, int> data, int columns, int rows, bool yZeroIndexed = true, DateTime? minDate = null, DateTime? maxDate = null, string suffix = "")
        {
            if (columns < 1)
                columns = 1;

            if (rows < 1)
                rows = 1;

            // dupe code from GetPoints
            DateTime firstDateTime = minDate ?? data.First().Key;
            DateTime lastDateTime = maxDate ?? data.Last().Key;

            long totalSec = (long)(lastDateTime - firstDateTime).TotalSeconds; // sec granularity
            int minVal = int.MaxValue;
            int maxVal = int.MinValue;
            foreach (var item in data)
            {
                if (item.Value < minVal)
                    minVal = item.Value;
                if (item.Value > maxVal)
                    maxVal = item.Value;
            }

            if (yZeroIndexed)
                minVal = 0;
            // dupe code end -> add generic class TODO

            // X AXIS
            long intervalColumn = totalSec / columns;

            List<string> xAxisLabels = new List<string>();
            DateTimeOffset currentDate = firstDateTime;
            xAxisLabels.Add(currentDate.ToString("dd.MM.yy HH:mm"));

            for (int i = 0; i < columns; i++)
            {
                currentDate = currentDate.AddSeconds(intervalColumn);
                xAxisLabels.Add(currentDate.ToString("dd.MM.yy HH:mm"));
            }


            // Y AXIS
            int intervalRow = (maxVal - minVal) / rows;

            int currentValue = minVal;

            List<string> yAxisLabels = new List<string>();
            yAxisLabels.Add(currentValue.ToString());

            for (int i = 0; i < rows; i++)
            {
                currentValue += intervalRow;
                yAxisLabels.Add(currentValue.ToString() + suffix);
            }

            return (xAxisLabels, yAxisLabels);
        }

        public static (List<string> XAxisLables, List<string> YAxisLabels) GetLabels(DateTime minX, DateTime maxX, int minY, int maxY, int columns, int rows, string suffix = "")
        {
            if (columns < 1)
                columns = 1;

            if (rows < 1)
                rows = 1;

            // dupe code from GetPoints
            DateTime firstDateTime = minX;
            DateTime lastDateTime = maxX;

            long totalSec = (long)(lastDateTime - firstDateTime).TotalSeconds; // sec granularity
            int minVal = minY;
            int maxVal = maxY;

            // dupe code end -> add generic class TODO

            // X AXIS
            long intervalColumn = totalSec / columns;

            List<string> xAxisLabels = new List<string>();
            DateTimeOffset currentDate = firstDateTime;
            xAxisLabels.Add(currentDate.ToString("dd.MM.yy HH:mm"));

            for (int i = 0; i < columns; i++)
            {
                currentDate = currentDate.AddSeconds(intervalColumn);
                xAxisLabels.Add(currentDate.ToString("dd.MM.yy HH:mm"));
            }


            // Y AXIS
            int intervalRow = (maxVal - minVal) / rows;

            int currentValue = minVal;

            List<string> yAxisLabels = new List<string>();
            yAxisLabels.Add(currentValue.ToString("N0"));

            for (int i = 0; i < rows; i++)
            {
                currentValue += intervalRow;
                yAxisLabels.Add(currentValue.ToString("N0") + suffix);
            }

            return (xAxisLabels, yAxisLabels);
        }

        public static List<SKPoint> GetPoints(Dictionary<DateTime, int> data, GridSize gridSize, bool yZeroIndexed = true, DateTime? minDate = null, DateTime? maxDate = null, bool overlapDays = false, int maxValue = -1)
        {
            // TODO implement overlap days

            List<SKPoint> dataPoints = new List<SKPoint>();



            // assume the dictionary is ordered
            // todo ensure that just in case

            DateTime firstDateTime = minDate ?? data.First().Key;
            DateTime lastDateTime = maxDate ?? data.Last().Key;

            long totalSec = (long)(lastDateTime - firstDateTime).TotalSeconds; // sec granularity

            if (totalSec == 0)
                return dataPoints;

            int minVal = int.MaxValue;
            int maxVal = int.MinValue;

            foreach (var item in data)
            {
                if (item.Value < minVal)
                    minVal = item.Value;
                if (item.Value > maxVal)
                    maxVal = item.Value;
            }

            if (yZeroIndexed)
                minVal = 0;

            // set max value incase its specified
            if (maxValue > 0)
                maxVal = maxValue;

            foreach (var item in data)
            {
                if (maxVal == 0)
                    break;

                long currentTotalSec = (long)(item.Key - firstDateTime).TotalSeconds; // sec granularity

                decimal xPercentage = currentTotalSec / (decimal)totalSec;
                decimal yPercentage = (item.Value - minVal) / (decimal)(maxVal - minVal);

                int xpos = (int)(gridSize.XSize * xPercentage) + gridSize.XMin;
                int ypos = gridSize.YSize - (int)(gridSize.YSize * yPercentage) + gridSize.YMax;

                dataPoints.Add(new SKPoint(xpos, ypos));
            }

            return dataPoints;
        }

        public static void DrawPoints(SKCanvas canvas, SKBitmap bitmap, List<SKPoint> points, int size = 6, SKPaint paint = null, string text = "", int index = 0)
        {
            if (paint == null)
            {
                paint = new SKPaint()
                {
                    Color = WhiteColor
                };
            }

            foreach (var point in points)
                canvas.DrawRect(new SKRect(point.X - size / 2, point.Y - size / 2, size, size), paint);

            // draw Legend

            int heigth = bitmap.Height;
            int labelWidth = 250;

            int yOffset = 50;
            int xOffset = 110;

            int x = xOffset - 5 + labelWidth * index;
            int y = heigth - yOffset + 10;

            canvas.DrawRect(new SKRect(x - size / 2, y - size / 2, x + size / 2, y + size / 2), paint);
            canvas.DrawText(text, new SKPoint(xOffset + labelWidth * index, heigth - yOffset), DefaultTextPaint); // TODO Correct paint?
        }

        public static (bool newRow, int usedWidth) DrawLine(SKCanvas canvas, SKBitmap bitmap, List<SKPoint> points, SKPaint paint, int size = 6, string text = "", int labelRow = 0, int labelXOffset = 0, bool drawPoint = false, float labelYHeight = -1, SKBitmap bitmapIcon = null)
        {
            bool newRow = false;

            if (paint == null)
                paint = new SKPaint() { Color = WhiteColor };

            // Dont fill the rectangles
            paint.Style = SKPaintStyle.Stroke;

            int xOffset = 120; // TODO maybe trough padding

            float usedLabelWidth = paint.MeasureText(text) + 70 /* buffer */;

            if (xOffset + labelXOffset + usedLabelWidth > bitmap.Width)
            {
                labelRow++;
                labelXOffset = 0;
                newRow = true;
            }

            if (points.Count == 0)
                return (false, -1);

            SKPoint prevPoint = points.First();
            foreach (var point in points)
            {
                if (drawPoint)
                    canvas.DrawRect(new SKRect(point.X - size / 2, point.Y - size / 2, point.X + size / 2, point.Y + size / 2), paint);

                canvas.DrawLine(prevPoint, point, paint);
                prevPoint = point;
            }

            // draw Legend

            int heigth = bitmap.Height;

            int yOffset = 25; // TODO Make padding depending
            

            int iconDist = 20;

            var textPaint = MediumTextPaint;

            int yBase = heigth - yOffset - (int)textPaint.TextSize / 2 + labelRow * 20;
            int xBase = xOffset + labelXOffset;

            if (drawPoint)
                canvas.DrawRect(new SKRect(xBase - size / 2 - iconDist / 2, yBase - size / 2, xBase + size / 2 - iconDist / 2, yBase + size / 2), paint);

            canvas.DrawLine(new SKPoint(xBase - iconDist, yBase), new SKPoint(xBase, yBase), paint);
            canvas.DrawText(text, new SKPoint(xBase + 5, yBase + size), textPaint); // TODO Correct paint?

            if(labelYHeight > 0)
            {
                var specialPaint = textPaint;
                specialPaint.Color = paint.Color;

                canvas.DrawText(text, new SKPoint(bitmap.Width - 140 /* TODO dynamic trough padding */, labelYHeight), specialPaint); // TODO Correct paint?
                canvas.DrawBitmap(bitmapIcon, new SKPoint(bitmap.Width - 140 /* TODO dynamic trough padding */, labelYHeight));
            }

            return (newRow, (int)usedLabelWidth);
        }

        public static void DrawGrid(SKCanvas canvas, GridSize gridSize, Padding padding, List<string> xAxis, List<string> yAxis, string title, List<string> secondYAxis = null)
        {
            // TODO consider what to do with the last entry -> add empty label?
            int columns = xAxis.Count - 1;
            int rows = yAxis.Count - 1;


            var labelPaint = MediumTextPaint;

            // draw columns
            for (int i = 0; i <= columns; i++)
            {
                canvas.DrawText($"{xAxis[i]}", new SKPoint(gridSize.XMin + (i * (gridSize.XSize / columns)) - 25, gridSize.YMin + labelPaint.TextSize), labelPaint);

                if (i < columns)
                    canvas.DrawLine(new SKPoint(gridSize.XMin + (i * (gridSize.XSize / columns)), gridSize.YMin), new SKPoint(gridSize.XMin + (i * (gridSize.XSize / columns)), gridSize.YMax), DefaultDrawing);
            }
            canvas.DrawLine(new SKPoint(gridSize.XMax, gridSize.YMin), new SKPoint(gridSize.XMax, gridSize.YMax), DefaultDrawing);

            // draw rows
            for (int i = 0; i <= rows; i++)
            {
                var leftText = labelPaint;
                leftText.TextAlign = SKTextAlign.Right;

                canvas.DrawText($"{yAxis[i]}", new SKPoint(padding.Left - 5, padding.Top + gridSize.YSize - (gridSize.YSize / rows) * i + labelPaint.TextSize / 2), labelPaint);
                if (secondYAxis != null)
                    canvas.DrawText($"{secondYAxis[i]}", new SKPoint(gridSize.XMax + 15, padding.Top + gridSize.YSize - (gridSize.YSize / rows) * i + labelPaint.TextSize / 2), labelPaint);

                if (i < rows)
                    canvas.DrawLine(new SKPoint(gridSize.XMin, padding.Top + gridSize.YSize - (gridSize.YSize / rows) * i), new SKPoint(gridSize.XMax, padding.Top + gridSize.YSize - (gridSize.YSize / rows) * i), DefaultDrawing);
            }
            canvas.DrawLine(new SKPoint(gridSize.XMin, gridSize.YMax), new SKPoint(gridSize.XMax, gridSize.YMax), DefaultDrawing);

            // Draw title
            canvas.DrawText(title, new SKPoint(padding.Left, padding.Top / 2), TitleTextPaint);
        }
        public static SKBitmap CropImage(SKBitmap bitmap, SKRect cropRect)
        {

            SKBitmap croppedBitmap = new SKBitmap((int)cropRect.Width,
                                                  (int)cropRect.Height);
            SKRect dest = new SKRect(0, 0, cropRect.Width, cropRect.Height);
            SKRect source = new SKRect(cropRect.Left, cropRect.Top,
                                       cropRect.Right, cropRect.Bottom);

            using (SKCanvas canvas = new SKCanvas(croppedBitmap))
            {
                canvas.DrawBitmap(bitmap, source, dest);
            }

            return croppedBitmap;
        }

        /* USE THIS TO ENFORCE THE SAME STYLE FOR ALL IMAGES*/
        public static SKColor DiscordBackgroundColor
        {
            get { return new SKColor(54, 57, 63); }
        }
        public static SKColor WhiteColor
        {
            get { return new SKColor(255, 255, 255); }
        }

        public static Padding DefaultPadding
        {
            get { return new Padding(75, 75, 75, 75); }
        }

        public static SKTypeface Typeface_Arial
        {
            //get { return SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal); }
            get { return SKTypeface.FromFamilyName("Tomaha", SKFontStyle.Normal); }
        }

        public static SKPaint DefaultTextPaint
        {
            get
            {
                return new SKPaint()
                {
                    Typeface = Typeface_Arial,
                    TextSize = NormalTextSize,
                    Color = WhiteColor,
                    TextEncoding = SKTextEncoding.Utf8,
                    IsAntialias = true
                };
            }
        }
        public static SKPaint MediumTextPaint
        {
            get
            {
                return new SKPaint()
                {
                    Typeface = Typeface_Arial,
                    TextSize = MediumTextSize,
                    Color = WhiteColor,
                    TextEncoding = SKTextEncoding.Utf8,
                    IsAntialias = true
                };
            }
        }

        public static SKPaint TitleTextPaint
        {
            get
            {
                return new SKPaint()
                {
                    Typeface = Typeface_Arial,
                    TextSize = TitleTextSize,
                    Color = WhiteColor,
                    TextEncoding = SKTextEncoding.Utf8,
                    IsAntialias = true
                };
            }
        }

        public static SKPaint LargeTextPaint
        {
            get
            {
                return new SKPaint()
                {
                    Typeface = Typeface_Arial,
                    TextSize = LargeTextSize,
                    Color = WhiteColor,
                    TextEncoding = SKTextEncoding.Utf8,
                    IsAntialias = true
                };
            }
        }

        public static SKPaint DefaultDrawing
        {
            get
            {
                return new SKPaint()
                {
                    Color = WhiteColor,
                    IsAntialias = true
                };
            }
        }

        public static int LargeTextSize
        {
            get { return 20; }
        }
        public static int TitleTextSize
        {
            get { return 16; }
        }

        public static int NormalTextSize
        {
            get { return 11; }
        }
        public static int MediumTextSize
        {
            get { return 14; }
        }
    }
}
