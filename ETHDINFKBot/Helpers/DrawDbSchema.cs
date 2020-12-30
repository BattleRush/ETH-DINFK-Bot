using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace ETHDINFKBot.Helpers
{
    public class DrawDbSchema : IDisposable
    {
        private Bitmap Bitmap;
        private Graphics Graphics;
        private List<DBTableInfo> DBTableInfo;
        public DrawDbSchema(List<DBTableInfo> dbInfos)
        {
            Bitmap = new Bitmap(1920, 1080); // TODO insert into constructor
            Graphics = Graphics.FromImage(Bitmap);
            Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            DBTableInfo = dbInfos;
            Graphics.Clear(Color.FromArgb(54, 57, 63));
        }

        public void Dispose()
        {
            Bitmap.Dispose();
        }

        //https://stackoverflow.com/questions/2265910/convert-an-image-to-grayscale
        public static Bitmap MakeGrayscale3(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            using (Graphics g = Graphics.FromImage(newBitmap))
            {

                //create the grayscale ColorMatrix
                ColorMatrix colorMatrix = new ColorMatrix(
                   new float[][]
                   {
             new float[] {.3f, .3f, .3f, 0, 0},
             new float[] {.59f, .59f, .59f, 0, 0},
             new float[] {.11f, .11f, .11f, 0, 0},
             new float[] {0, 0, 0, 1, 0},
             new float[] {0, 0, 0, 0, 1}
                   });

                //create some image attributes
                using (ImageAttributes attributes = new ImageAttributes())
                {

                    //set the color matrix attribute
                    attributes.SetColorMatrix(colorMatrix);

                    //draw the original image on the new image
                    //using the grayscale color matrix
                    g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                                0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
                }
            }
            return newBitmap;
        }

        public static Bitmap TranslateBitmapToTryToRemoveGreenTint(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            using (Graphics g = Graphics.FromImage(newBitmap))
            {

                //create the grayscale ColorMatrix
                ColorMatrix colorMatrix = new ColorMatrix(
                   new float[][]
                   {
             new float[] {.3f, .3f, .3f, 0, 0},
             new float[] {.59f, .59f, .59f, 0, 0},
             new float[] {.11f, .11f, .11f, 0, 0},
             new float[] {0, 0, 0, 1, 0},
             new float[] {0, 0, 0, 0, 1}
                   });

                //create some image attributes
                using (ImageAttributes attributes = new ImageAttributes())
                {

                    //set the color matrix attribute
                    //attributes.SetColorMatrix(colorMatrix);

                    //draw the original image on the new image
                    //using the grayscale color matrix
                    g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                                0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
                }
            }
            return newBitmap;
        }

        public void DrawAllTables()
        {
            Brush b = new SolidBrush(Color.White);
            Pen p = new Pen(b);

            Brush b2 = new SolidBrush(Color.Red);
            Pen pen2 = new Pen(Color.FromArgb(172, 224, 128, 0), 15);

#if DEBUG
            pen2 = new Pen(Color.FromArgb(172, 128, 224, 0), 15);
#endif

            pen2.Width = 4;

            Font drawFont = new Font("Arial", 11);
            Font drawFont2 = new Font("Arial", 16);
            SolidBrush drawBrush = new SolidBrush(Color.Black);

            int leftPadding = 50;
            int topPadding = 50;
            int width = 260;
            int height = 35;

            int perTableHeight = 350;

            string pathToImage = "";

#if DEBUG
            pathToImage = Path.Combine("Images", "keyicon.png");
#else
            pathToImage = Path.Combine(Program.BasePath, "Images", "keyicon.png");
#endif

            var key = TranslateBitmapToTryToRemoveGreenTint(new Bitmap(pathToImage));

            // possible linux fix for green tint
            key.MakeTransparent();


            var fkKey = MakeGrayscale3(key);

            int count = 0;
            int row = 0;

            Dictionary<string, Point> primaryKeys = new Dictionary<string, Point>();

            foreach (var dbTable in DBTableInfo)
            {
                if (row == 0)
                {
                    perTableHeight = 210;
                }
                else
                {
                    perTableHeight = 360;
                }

                int countTable = 0;

                int custHeight = row * perTableHeight - 150;
                custHeight = custHeight < 0 ? 0 : custHeight;

                //Graphics.DrawString(dbTable.TableName, drawFont2, b, new Point(leftPadding + (width + leftPadding) * count - 5, topPadding + countTable * height + custHeight - 35));


                foreach (var field in dbTable.FieldInfos)
                {
                    if (field.IsPrimaryKey)
                    {
                        int x = leftPadding + (width + leftPadding) * count + width - 35;
                        int y = topPadding + countTable * height + custHeight + 8;

                        //Graphics.DrawImage(key, x, y, 30, 20);

                        primaryKeys.Add(dbTable.TableName, new Point(x + 35, y + 4));
                    }

                    //Graphics.DrawRectangle(p, new Rectangle(leftPadding + (width + leftPadding) * count, topPadding + countTable * height + custHeight, width, height));

                    var type = field.Type.Replace("EGER", "");

                    //Graphics.DrawString($"{field.Name} ({type}{(field.Nullable ? ", NULL" : "")})", drawFont, b, new Point(leftPadding + (width + leftPadding) * count + 10, topPadding + countTable * height + custHeight + 10));

                    if (field.IsForeignKey && !field.IsPrimaryKey)
                    {
                        //Graphics.DrawImage(fkKey, leftPadding + (width + leftPadding) * count + width - 35, topPadding + countTable * height + custHeight + 8, 30, 20);

                        if (primaryKeys.ContainsKey(field.ForeignKeyInfo.ToTable))
                        {
                            //Graphics.DrawLine(p, new Point(leftPadding + (width + leftPadding) * count + width - 35, topPadding + countTable * height + custHeight + 8), primaryKeys[field.ForeignKeyInfo.ToTable]);
                        }
                    }
                    countTable++;
                }




                count++;

                if (count > 5)
                {
                    count = 0;
                    row++;
                }
            }


            count = 0;
            row = 0;
            foreach (var dbTable in DBTableInfo)
            {

                int countTable = 0;
                foreach (var field in dbTable.FieldInfos)
                {
                    if (row == 0)
                    {
                        perTableHeight = 210;
                    }
                    else
                    {
                        perTableHeight = 360;
                    }


                    int custHeight = row * perTableHeight - 150;
                    custHeight = custHeight < 0 ? 0 : custHeight;


                    if (field.IsForeignKey && !field.IsPrimaryKey)
                    {

                        if (primaryKeys.ContainsKey(field.ForeignKeyInfo.ToTable))
                        {
                            var p1 = new Point(leftPadding + (width + leftPadding) * count + width, topPadding + countTable * height + custHeight + 20);
                            var p2 = primaryKeys[field.ForeignKeyInfo.ToTable];


                            var ang1 = 135;
                            var ang2 = -135;

                            if (p1.X == p2.X)
                            {
                                //p1.X += width;
                                ang1 = 0;
                                ang2 = 180;
                            }
                            else if (p1.X >= p2.X)
                            {
                                p1.X -= width;
                                ang1 = 135;
                                ang2 = -135;
                            }
                            else
                            {
                                p2.X -= width;
                                ang1 = 0;
                                ang2 = 0;
                            }

                            var path = GetPath(p1, p2);






                            var len = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));

                            var ax1 = Math.Cos(ang1) * (double)len * (1 / (double)3);
                            var ay1 = Math.Sin(ang1) * (double)len * (1 / (double)3);

                            var ax2 = Math.Cos(ang2) * (double)len * (1 / (double)3);
                            var ay2 = Math.Sin(ang2) * (double)len * (1 / (double)3);


                            var p11 = new Point((int)(p1.X + ax1), (int)(p1.Y + ay1));
                            var p12 = new Point((int)(p2.X - ax2), (int)(p2.Y + ay2));


                            Graphics.DrawBezier(pen2, p1, p11, p12, p2);
                        }
                        else
                        {

                        }
                    }

                    countTable++;
                }

                count++;

                if (count > 5)
                {
                    count = 0;
                    row++;
                }
            }



            count = 0;
            row = 0;

            //Dictionary<string, Point> primaryKeys = new Dictionary<string, Point>();

            foreach (var dbTable in DBTableInfo)
            {
                if (row == 0)
                {
                    perTableHeight = 210;
                }
                else
                {
                    perTableHeight = 360;
                }

                int countTable = 0;

                int custHeight = row * perTableHeight - 150;
                custHeight = custHeight < 0 ? 0 : custHeight;

                Graphics.DrawString(dbTable.TableName, drawFont2, b, new Point(leftPadding + (width + leftPadding) * count, topPadding + countTable * height + custHeight - 35));


                foreach (var field in dbTable.FieldInfos)
                {
                    if (field.IsPrimaryKey)
                    {
                        int x = leftPadding + (width + leftPadding) * count + width - 35;
                        int y = topPadding + countTable * height + custHeight + 8;

                        Graphics.DrawImage(key, x, y, 30, 20);

                        //primaryKeys.Add(dbTable.TableName, new Point(x + 35, y + 4));
                    }
                    Brush brush = new SolidBrush(Color.FromArgb(128, 137, 153, 162));
                    Graphics.FillRectangle(brush, new Rectangle(leftPadding + (width + leftPadding) * count, topPadding + countTable * height + custHeight, width, height));
                    Graphics.DrawRectangle(p, new Rectangle(leftPadding + (width + leftPadding) * count, topPadding + countTable * height + custHeight, width, height));
                    
                    var type = field.Type.Replace("EGER", "");

                    Graphics.DrawString($"{field.Name} ({type}{(field.Nullable ? ", NULL" : "")})", drawFont, b, new Point(leftPadding + (width + leftPadding) * count + 10, topPadding + countTable * height + custHeight + 10));

                    if (field.IsForeignKey && !field.IsPrimaryKey)
                    {
                        Graphics.DrawImage(fkKey, leftPadding + (width + leftPadding) * count + width - 35, topPadding + countTable * height + custHeight + 8, 30, 20);

                        if (primaryKeys.ContainsKey(field.ForeignKeyInfo.ToTable))
                        {
                            //Graphics.DrawLine(p, new Point(leftPadding + (width + leftPadding) * count + width - 35, topPadding + countTable * height + custHeight + 8), primaryKeys[field.ForeignKeyInfo.ToTable]);
                        }
                    }
                    countTable++;
                }




                count++;

                if (count > 5)
                {
                    count = 0;
                    row++;
                }
            }


        }

        private GraphicsPath GetPath(Point p1, Point p2)
        {
            return null;
            /*
            GraphicsPath path = new GraphicsPath();

            PointF[] ptsArray =
            {
                         
                };

            QuadraticBezierSegment qg = new QuadraticBezierSegment();

            qg.Point1 = new Point(Convert.ToDouble(p1x.Text), Convert.ToDouble(p1y.Text));
            //P1

            qg.Point2 = new Point(Convert.ToDouble(p2x.Text), Convert.ToDouble(p2y.Text));


            // Add a curve, a rectangle, an ellipse, and a line  
            path.AddBeziers(ptsArray);

            return path;
            */
        }

        private void CreateBlankImage()
        {
            /*using ()
            {
                using (var graphics = Graphics.FromImage(image))
                {
                    graphics.CompositingQuality = CompositingQuality.HighSpeed;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    //graphics.DrawImage(image, 0, 0, width, height);
                    Pen p = new Pen(Color.Red);
                    var b = new SolidBrush(p.Color);

                    graphics.DrawEllipse(p, new Rectangle(new Point(40, 40), new Size(50, 50)));
                }
    

            }*/
        }


        public Stream GetStream()
        {
            Stream ms = new MemoryStream();
            Bitmap.Save(ms, ImageFormat.Png);
            ms.Position = 0;

            return ms;

            //await Context.Channel.SendFileAsync(ms, "test.png");
        }
    }
}
