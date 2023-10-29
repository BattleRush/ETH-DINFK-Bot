


using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace ETHDINFKBot.Drawing
{
    public class PieChart : IDisposable
    {
        private int Width { get; set; }
        private int Height { get; set; }

        private int Padding { get; set; }

        private SKCanvas Canvas { get; set; }
        private SKBitmap Bitmap { get; set; }

        public PieChart()
        {
            Width = 2000;
            Height = 2000;

            Padding = 500;

            Bitmap = new SKBitmap(Width, Height);
            Canvas = new SKCanvas(Bitmap);

            Canvas.Clear(DrawingHelper.DiscordBackgroundColor); // TODO Gray color of discord
        }

        // https://bottosson.github.io/posts/oklab/
        // TODO check if this is correct (linear srgb to oklab)
        SKColor linear_srgb_to_oklab(SKColor c) 
        {
            double r = c.Red / 255.0;
            double g = c.Green / 255.0;
            double b = c.Blue / 255.0;

            double l = 0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b;
            double m = 0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b;
            double s = 0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b;

            double l_ = Math.Cbrt(l);
            double m_ = Math.Cbrt(m);
            double s_ = Math.Cbrt(s);

            return new SKColor(
                (byte)(0.2104542553f*l_ + 0.7936177850f*m_ - 0.0040720468f*s_),
                (byte)(1.9779984951f*l_ - 2.4285922050f*m_ + 0.4505937099f*s_),
                (byte)(0.0259040371f*l_ + 0.7827717662f*m_ - 0.8086757660f*s_)
            );
        }

        public void Data(List<string> labels, List<int> data)
        {
            var rect = new SKRect(Padding, Padding, Width - Padding, Height - Padding);

            var currAngle = 0f;

            int total = data.Sum();

            int sizeLabels = labels.Count;
            int sizeData = data.Count;

            if (sizeLabels != sizeData)
            {
                throw new Exception("Labels and data must be the same size");
            }

            var prevAngle = 360f;

            for (int i = 0; i < sizeLabels; i++)
            {
                float percentage = (float)data[i] / total;

                var randPen = new SKPaint
                {
                    Color = linear_srgb_to_oklab(SKColor.FromHsl(360 * i / sizeLabels, 100, 50)),
                    StrokeWidth = 5,
                    IsAntialias = true
                };

                var randBrush = new SKPaint
                {
                    Color = linear_srgb_to_oklab(SKColor.FromHsl(360 * i / sizeLabels, 100, 50)),
                    IsAntialias = true
                };

                Canvas.DrawArc(rect, currAngle, percentage * 360, false, randPen);
                Canvas.DrawArc(rect, currAngle, percentage * 360, true, randBrush);

                var middleAngle = currAngle + percentage * 180;

                int linePadding = 10;
                int lineLength = 50;

                // todo only if on the left side the collision between labels happens
                //if(prevAngle < 15f)
                //    lineLength = 100;

                var circleMiddle = new SKPoint(rect.MidX, rect.MidY);
                var radiusSmall = rect.Width / 2 + linePadding;
                var radiusLarge = rect.Width / 2 + lineLength;

                var pointStart = new SKPoint(
                    (float)(circleMiddle.X + radiusSmall * Math.Cos(middleAngle * Math.PI / 180)),
                    (float)(circleMiddle.Y + radiusSmall * Math.Sin(middleAngle * Math.PI / 180))
                );

                var pointEnd = new SKPoint(
                    (float)(circleMiddle.X + radiusLarge * Math.Cos(middleAngle * Math.PI / 180)),
                    (float)(circleMiddle.Y + radiusLarge * Math.Sin(middleAngle * Math.PI / 180))
                );

                Canvas.DrawLine(pointStart, pointEnd, randPen);

                var lineEnd = new SKPoint(pointEnd.X + lineLength, pointEnd.Y);
                var currLineAngle = currAngle + middleAngle;
                if(currAngle < 90 || currAngle > 270)
                {
                    Canvas.DrawLine(pointEnd, new SKPoint(pointEnd.X + lineLength, pointEnd.Y), randPen);
                    lineEnd = new SKPoint(pointEnd.X + lineLength - 2, pointEnd.Y);
                }
                else
                {
                    Canvas.DrawLine(pointEnd, new SKPoint(pointEnd.X - lineLength, pointEnd.Y), randPen);
                    lineEnd = new SKPoint(pointEnd.X - lineLength + 2, pointEnd.Y);
                }

                

                // draw label
                var label = labels[i] + " " + data[i];
                var labelPaint = new SKPaint
                {
                    Color = linear_srgb_to_oklab(SKColor.FromHsl(360 * i / sizeLabels, 100, 50)),
                    TextSize = 26,
                    IsAntialias = true
                };

                if(currAngle < 90 || currAngle > 270)
                    Canvas.DrawText(label, new SKPoint(lineEnd.X + 5, lineEnd.Y + labelPaint.TextSize / 2 - 4), labelPaint);
                else
                    Canvas.DrawText(label, new SKPoint(lineEnd.X - labelPaint.MeasureText(label), lineEnd.Y + labelPaint.TextSize / 2 -  4), labelPaint);

                //Canvas.DrawText(label, lineEnd, labelPaint);

                currAngle += percentage * 360;
                prevAngle = percentage * 360;
            }
        }

        public SKBitmap GetBitmap()
        {
            return Bitmap;
        }

        public void Dispose()
        {
            Canvas.Dispose();
            Bitmap.Dispose();
        }
    }
}