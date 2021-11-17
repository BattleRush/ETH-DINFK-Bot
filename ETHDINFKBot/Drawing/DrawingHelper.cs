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
            xAxisLabels.Add(currentDate.ToString("MM.dd\nHH:mm"));

            for (int i = 0; i < columns; i++)
            {
                currentDate = currentDate.AddSeconds(intervalColumn);
                xAxisLabels.Add(currentDate.ToString("MM.dd\nHH:mm"));
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

        public static List<SKPoint> GetPoints(Dictionary<DateTime, int> data, GridSize gridSize, bool yZeroIndexed = true, DateTime? minDate = null, DateTime? maxDate = null, bool overlapDays = false)
        {
            // TODO implement overlap days

            List<SKPoint> dataPoints = new List<SKPoint>();

            // assume the dictionary is ordered
            // todo ensure that just in case

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

        public static void DrawLine(SKCanvas canvas, SKBitmap bitmap, List<SKPoint> points, int size = 6, SKPaint paint = null, string text = "", int index = 0, bool drawPoint = false)
        {
            if (paint == null)
            {
                paint = new SKPaint()
                {
                    Color = WhiteColor
                };
            }

            if (points.Count == 0)
                return;

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
            int labelWidth = 250;

            int yOffset = 50;
            int xOffset = 120;

            int iconDist = 20;

            if (drawPoint)
            {
                int x = xOffset - iconDist / 2 + labelWidth * index;
                int y = heigth - yOffset + 10;

                canvas.DrawRect(new SKRect(x - size / 2, y - size / 2, x + size / 2, y + size / 2), paint);
            }

            //pen.Width = 2;

            canvas.DrawLine(new SKPoint(xOffset - iconDist + labelWidth * index, heigth - yOffset + 10), new SKPoint(xOffset + labelWidth * index, heigth - yOffset + 10), paint);
            canvas.DrawText(text, new SKPoint(xOffset + labelWidth * index, heigth - yOffset), DefaultTextPaint); // TODO Correct paint?
        }

        public static void DrawGrid(SKCanvas canvas, GridSize gridSize, Padding padding, List<string> xAxis, List<string> yAxis, string title, List<string> secondYAxis = null)
        {
            //var pen = Pen_White;
            //var font = TitleFont;
            //var brush = SolidBrush_White;

            var paint = new SKPaint(); // TODO Define paint -> TODO Pass as parameter

            // TODO consider what to do with the last entry -> add empty label?
            int columns = xAxis.Count - 1;
            int rows = yAxis.Count - 1;

            int fontHeight = 11;

            // draw columns
            for (int i = 0; i <= columns; i++)
            {
                canvas.DrawText($"{xAxis[i]}", new SKPoint(gridSize.XMin + (i * (gridSize.XSize / columns)) - 25, gridSize.YMin + fontHeight - 3), DefaultTextPaint);

                if (i < columns)
                    canvas.DrawLine(new SKPoint(gridSize.XMin + (i * (gridSize.XSize / columns)), gridSize.YMin), new SKPoint(gridSize.XMin + (i * (gridSize.XSize / columns)), gridSize.YMax), paint);
            }
            canvas.DrawLine(new SKPoint(gridSize.XMax, gridSize.YMin), new SKPoint(gridSize.XMax, gridSize.YMax), paint);

            // draw rows
            for (int i = 0; i <= rows; i++)
            {
                canvas.DrawText($"{yAxis[i]}", new SKPoint(20, padding.Top + gridSize.YSize - (gridSize.YSize / rows) * i - fontHeight / 2 /* to center it vertically */), DefaultTextPaint);
                if (secondYAxis != null)
                    canvas.DrawText($"{secondYAxis[i]}", new SKPoint(gridSize.XMax + 10, padding.Top + gridSize.YSize - (gridSize.YSize / rows) * i - fontHeight / 2 /* to center it vertically */), DefaultTextPaint);

                if (i < rows)
                    canvas.DrawLine(new SKPoint(gridSize.XMin, padding.Top + gridSize.YSize - (gridSize.YSize / rows) * i), new SKPoint(gridSize.XMax, padding.Top + gridSize.YSize - (gridSize.YSize / rows) * i), paint);
            }
            canvas.DrawLine(new SKPoint(gridSize.XMin, gridSize.YMax), new SKPoint(gridSize.XMax, gridSize.YMax), paint);

            // Draw title
            canvas.DrawText(title, new SKPoint(100, 20), TitleTextPaint);

            var paint3 = new SKPaint
            {
                TextSize = 64.0f,
                IsAntialias = true,
                Color = new SKColor(136, 136, 136),
                TextScaleX = 1.5f

            };
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
            get { return new Padding(75, 100, 125, 100); }
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

        public static int TitleTextSize
        {
            get { return 16; }
        }

        public static int NormalTextSize
        {
            get { return 11; }
        }
    }
}
