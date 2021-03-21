using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public GridSize(Bitmap bitmap, Padding padding)
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
    public static class DrawingHelper
    {
        public static (Graphics Graphics, Bitmap Bitmap) GetEmptyGraphics(int width = 1920, int height = 1080)
        {
            var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb); // TODO see if needed format
            var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(DrawingHelper.DiscordBackgroundColor);

            return (graphics, bitmap);
        }


        // TODO duplicate params similar to GetPoints
        public static (List<string> XAxisLables, List<string> YAxisLabels) GetLabels(Dictionary<DateTimeOffset, int> data, int columns, int rows, bool yZeroIndexed = true, DateTime? minDate = null, DateTime? maxDate = null)
        {
            if (columns < 1)
                columns = 1;
            if (rows < 1)
                rows = 1;


            // dupe code from GetPoints
            DateTimeOffset firstDateTime = minDate ?? data.First().Key;
            DateTimeOffset lastDateTime = maxDate ?? data.Last().Key;

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
            xAxisLabels.Add(currentDate.ToString("yyyy.MM.dd HH:mm"));

            for (int i = 0; i < columns; i++)
            {
                currentDate = currentDate.AddSeconds(intervalColumn);
                xAxisLabels.Add(currentDate.ToString("yyyy.MM.dd HH:mm"));
            }


            // Y AXIS
            int intervalRow = (maxVal - minVal) / rows;

            int currentValue = minVal;

            List<string> yAxisLabels = new List<string>();
            yAxisLabels.Add(currentValue.ToString());

            for (int i = 0; i < columns; i++)
            {
                currentValue += intervalRow;
                yAxisLabels.Add(currentValue.ToString());
            }

            return (xAxisLabels, yAxisLabels);
        }

        public static List<Point> GetPoints(Dictionary<DateTimeOffset, int> data, GridSize gridSize, bool yZeroIndexed = true, DateTime? minDate = null, DateTime? maxDate = null, bool overlapDays = false)
        {
            // TODO implement overlap days

            List<Point> dataPoints = new List<Point>();

            // assume the dictionary is ordered
            // todo ensure that just in case

            DateTimeOffset firstDateTime = minDate ?? data.First().Key;
            DateTimeOffset lastDateTime = maxDate ?? data.Last().Key;

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
                long currentTotalSec = (long)(item.Key - firstDateTime).TotalSeconds; // sec granularity

                decimal xPercentage = currentTotalSec / (decimal)totalSec;
                decimal yPercentage = (item.Value - minVal) / (decimal)(maxVal - minVal);

                int xpos = (int)(gridSize.XSize * xPercentage) + gridSize.XMin;
                int ypos = gridSize.YSize - (int)(gridSize.YSize * yPercentage) + gridSize.YMax;

                dataPoints.Add(new Point(xpos, ypos));
            }

            return dataPoints;
        }

        public static void DrawPoints(Graphics graphics, List<Point> points)
        {
            foreach (var point in points)
            {
                graphics.DrawRectangle(Pen_Blue_Transparent, new Rectangle(point.X - 3, point.Y - 3, 6, 6));
            }
        }

        public static void DrawGrid(Graphics graphics, GridSize gridSize, Padding padding, List<string> xAxis, List<string> yAxis)
        {
            var pen = Pen_White;
            var font = TitleFont;
            var brush = SolidBrush_White;

            // TODO consider what to do with the last entry -> add empty label?
            int columns = xAxis.Count - 1;
            int rows = yAxis.Count - 1;

            int fontHeight = 11;

            // draw columns
            for (int i = 0; i <= columns; i++)
            {
                graphics.DrawString($"{xAxis[i]}", font, brush, new Point(gridSize.XMin + (i * (gridSize.XSize / columns)), gridSize.YMin + fontHeight));

                if (i < columns)
                    graphics.DrawLine(pen, new Point(gridSize.XMin + (i * (gridSize.XSize / columns)), gridSize.YMin), new Point(gridSize.XMin + (i * (gridSize.XSize / columns)), gridSize.YMax));
            }
            graphics.DrawLine(pen, new Point(gridSize.XMax, gridSize.YMin), new Point(gridSize.XMax, gridSize.YMax));

            // draw rows
            for (int i = 0; i <= rows; i++)
            {
                graphics.DrawString($"{yAxis[i]}", font, brush, new Point(40, padding.Top + gridSize.YSize - (gridSize.YSize / rows) * i - fontHeight / 2 /* to center it vertically */));

                if (i < rows)
                    graphics.DrawLine(pen, new Point(gridSize.XMin, padding.Top + gridSize.YSize - (gridSize.YSize / rows) * i), new Point(gridSize.XMax, padding.Top + gridSize.YSize - (gridSize.YSize / rows) * i));
            }
            graphics.DrawLine(pen, new Point(gridSize.XMin, gridSize.YMax), new Point(gridSize.XMax, gridSize.YMax));
        }


        /* USE THIS TO ENFORCE THE SAME STYLE FOR ALL IMAGES*/
        public static Color DiscordBackgroundColor
        {
            get { return Color.FromArgb(54, 57, 63); }
        }

        public static Padding DefaultPadding
        {
            get { return new Padding(100, 50, 100, 100); }
        }

        public static Font NormalTextFont
        {
            get { return new Font("Arial", 11); }
        }

        public static Font LargerTextFont
        {
            get { return new Font("Arial", 14); }
        }

        public static Font TitleFont
        {
            get { return new Font("Arial", 16); }
        }

        public static Pen Pen_White
        {
            get { return new Pen(SolidBrush_White); }
        }
        public static Pen Pen_Blue_Transparent
        {
            get { return new Pen(Color.FromArgb(172, 224, 128, 0), 15); }
        }
        public static SolidBrush SolidBrush_Yellow
        {
            get { return new SolidBrush(Color.Yellow); }
        }
        public static SolidBrush SolidBrush_Black
        {
            get { return new SolidBrush(Color.Black); }
        }

        public static SolidBrush SolidBrush_White
        {
            get { return new SolidBrush(Color.White); }
        }

        public static SolidBrush SolidBrush_Red
        {
            get { return new SolidBrush(Color.Red); }
        }
        public static SolidBrush SolidBrush_Blue
        {
            get { return new SolidBrush(Color.Blue); }
        }
    }
}
