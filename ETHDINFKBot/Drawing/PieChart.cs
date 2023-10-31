


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
        (double L, double c, double h) linear_srgb_to_oklab(SKColor c)
        {
            // make color linear
            /*c = new SKColor(
                (byte)(c.Red <= 10 ? c.Red / 3294.6 : Math.Pow((c.Red + 14) / 269.3, 2.4) * 3294.6),
                (byte)(c.Green <= 10 ? c.Green / 3294.6 : Math.Pow((c.Green + 14) / 269.3, 2.4) * 3294.6),
                (byte)(c.Blue <= 10 ? c.Blue / 3294.6 : Math.Pow((c.Blue + 14) / 269.3, 2.4) * 3294.6)
            );*/

            double r = c.Red / 255.0;
            double g = c.Green / 255.0;
            double b = c.Blue / 255.0;

            double toLinear(double val)
            {
                if (val <= 0.04045)
                    return val / 12.92;
                else
                    return Math.Pow((val + 0.055) / 1.055, 2.4);
            }

            (double c, double h) cartesian_to_polar(double x, double y)
            {
                return (Math.Sqrt(x * x + y * y), Math.Atan2(y, x));
            }

            r = toLinear(r);
            g = toLinear(g);
            b = toLinear(b);

            double l = 0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b;
            double m = 0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b;
            double s = 0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b;

            double l_ = Math.Cbrt(l);
            double m_ = Math.Cbrt(m);
            double s_ = Math.Cbrt(s);

            double oklab_L = 0.2104542553f * l_ + 0.7936177850f * m_ - 0.0040720468f * s_;
            double oklab_a = 1.9779984951f * l_ - 2.4285922050f * m_ + 0.4505937099f * s_;
            double oklab_b = 0.0259040371f * l_ + 0.7827717662f * m_ - 0.8086757660f * s_;

            (double oklch_c, double oklch_h) = cartesian_to_polar(oklab_a, oklab_b);

            return (oklab_L, oklch_c, oklch_h);
        }

        SKColor oklach_to_skcolor(double L, double c, double h)
        {
            double toSrgb(double val)
            {
                if (val <= 0.0031308)
                    return val * 12.92;
                else
                    return 1.055 * Math.Pow(val, 1 / 2.4) - 0.055;
            }

            (double c, double h) polar_to_cartesian_x(double r, double a)
            {
                return (r * Math.Cos(a), r * Math.Sin(a));
            }

            (double a, double b) = polar_to_cartesian_x(c, h);

            double l_ = L + 0.3963377774f * a + 0.2158037573f * b;
            double m_ = L - 0.1055613458f * a - 0.0638541728f * b;
            double s_ = L - 0.0894841775f * a - 1.2914855480f * b;

            double l = l_ * l_ * l_;
            double m = m_ * m_ * m_;
            double s = s_ * s_ * s_;

            double l_r = +4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s;
            double l_g = -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s;
            double l_b = -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s;

            Console.WriteLine($"l_r: {l_r} | l_g: {l_g} | l_b: {l_b}");

            l_r = Math.Clamp(l_r, 0, 1);
            l_g = Math.Clamp(l_g, 0, 1);
            l_b = Math.Clamp(l_b, 0, 1);


            return new SKColor(
                (byte)(toSrgb(l_r) * 255),
                (byte)(toSrgb(l_g) * 255),
                (byte)(toSrgb(l_b) * 255)
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

                (double L, double c, double h) = linear_srgb_to_oklab(SKColor.FromHsl(0, 70, 50));
                //h += 255 * (i / (float)sizeLabels);
                double oldHue = h;
                h = 2 * Math.PI * (i / (float)sizeLabels);

                /*if(i % 2 == 0)
                    h += Math.PI;

                h %= 2 * Math.PI;*/

                if(i % 2 == 0)
                    L *= 1.1;

                if(i % 4 == 0)
                {
                    L /= 1.1;
                    L *= 0.9;
                }
                
                //h -= 0.01;
                Console.WriteLine($"old h: {oldHue} | new h: {h}");

                SKColor color = oklach_to_skcolor(L, c, h);

                var randPen = new SKPaint
                {
                    Color = color,
                    StrokeWidth = 5,
                    IsAntialias = true,

                };

                var randBrush = new SKPaint
                {
                    Color = color,
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
                if (currLineAngle < 90 || currLineAngle > 270)
                {
                    Canvas.DrawLine(pointEnd, new SKPoint(pointEnd.X + lineLength, pointEnd.Y), randPen);
                    lineEnd = new SKPoint(pointEnd.X + lineLength - 5, pointEnd.Y);
                }
                else
                {
                    Canvas.DrawLine(pointEnd, new SKPoint(pointEnd.X - lineLength, pointEnd.Y), randPen);
                    lineEnd = new SKPoint(pointEnd.X - lineLength + 5, pointEnd.Y);
                }



                // draw label
                var label = labels[i] + " " + data[i];
                var labelPaint = new SKPaint
                {
                    Color = color,
                    TextSize = 26,
                    IsAntialias = true
                };

                if (currLineAngle < 90 || currLineAngle > 270)
                    Canvas.DrawText(label, new SKPoint(lineEnd.X + 10, lineEnd.Y + labelPaint.TextSize / 2 - 4), labelPaint);
                else
                    Canvas.DrawText(label, new SKPoint(lineEnd.X - labelPaint.MeasureText(label) - 15, lineEnd.Y + labelPaint.TextSize / 2 - 4), labelPaint);

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