using ETHDINFKBot.Drawing;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ETHDINFKBot.Helpers
{
    public class DrawDbSchema : IDisposable
    {
        public SKBitmap Bitmap; // to get stream maybe change a bit to a method in this class to give the stream
        private SKCanvas Canvas;
        private List<DBTableInfo> DBTableInfo;


        /* SETTINGS */

        private int TopPadding = 50;
        private int LeftPadding = 50;
        private int TableWidth = 400;
        private int ColumnCount = 5;

        private int RowHeight = 35;

        //private Pen RelationPen = new Pen(Color.FromArgb(172, 224, 128, 0), 15);
        //private Font TextFont = new Font("Arial", 11);
        //private Font TitleFont = new Font("Arial", 16);
        //private Brush WhiteBrush = new SolidBrush(Color.White);

        public DrawDbSchema(List<DBTableInfo> dbInfos)
        {
            Bitmap = new SKBitmap(ColumnCount * (TableWidth + LeftPadding) + LeftPadding, 10000); // TODO insert into constructor
            Canvas = new SKCanvas(Bitmap);

            DBTableInfo = dbInfos;
            Canvas.Clear(DrawingHelper.DiscordBackgroundColor);
        }

        public void Dispose()
        {
            Bitmap.Dispose();
            Canvas.Dispose();
        }

        //https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/effects/color-filters
        public static SKBitmap MakeGrayscale3(SKBitmap original)
        {
            // TODO TEST

            //create a blank bitmap the same size as original
            SKBitmap newBitmap = new SKBitmap(original.Width, original.Height);

            //get a graphics object from the new image
            using (SKCanvas g = new SKCanvas(newBitmap))
            {
                using (SKPaint paint = new SKPaint())
                {
                    paint.ColorFilter =
                        SKColorFilter.CreateColorMatrix(new float[]
                        {
                    0.21f, 0.72f, 0.07f, 0, 0,
                    0.21f, 0.72f, 0.07f, 0, 0,
                    0.21f, 0.72f, 0.07f, 0, 0,
                    0,     0,     0,     1, 0
                        });

                    g.DrawBitmap(newBitmap, new SKPoint(0, 0), paint);
                }
            }
            return newBitmap;
        }


        // TODO rework and cleanup
        public void DrawAllTables()
        {

            //Pen p = new Pen(b);

            //Brush b2 = new SolidBrush(Color.Red);


            //#if DEBUG
            //            RelationPen = new Pen(Color.FromArgb(172, 128, 224, 0), 15);
            //#endif
            //            RelationPen.Width = 4;


            //            Font drawFont2 = new Font("Arial", 16);
            //            SolidBrush drawBrush = new SolidBrush(Color.Black);


            string pathToImage = "";

#if DEBUG
            pathToImage = Path.Combine("Images", "Icons");
#else
            pathToImage = Path.Combine(Program.ApplicationSetting.BasePath, "Images", "Icons");
#endif

            var pkBitmap = SKBitmap.Decode(Path.Combine(pathToImage, "pk.png"));
            pkBitmap = pkBitmap.Resize(new SKSizeI(35, 20), SKFilterQuality.High);

            var fkBitmap = SKBitmap.Decode(Path.Combine(pathToImage, "fk.png"));
            fkBitmap = fkBitmap.Resize(new SKSizeI(35, 20), SKFilterQuality.High);

            Dictionary<string, SKBitmap> icons = new Dictionary<string, SKBitmap>();
            icons.Add("int", SKBitmap.Decode(Path.Combine(pathToImage,  "Integer_16x.png")));
            icons.Add("string", SKBitmap.Decode(Path.Combine(pathToImage, "String_16x.png")));
            icons.Add("bool", SKBitmap.Decode(Path.Combine(pathToImage, "TrueFalse_16x.png")));
            icons.Add("datetime", SKBitmap.Decode(Path.Combine(pathToImage, "DateTimeAxis_16x.png")));

            // possible linux fix for green tint
            //key.MakeTransparent();



            var primaryKeys = GeneratePrimaryKeyPositions();

            DrawRelations(primaryKeys);
            var maxHeight = DrawTables(pkBitmap, fkBitmap, icons);

            Bitmap = DrawingHelper.CropImage(Bitmap, new SKRect(0, 0, ColumnCount * (TableWidth + LeftPadding) + LeftPadding, maxHeight));
        }

        private int DrawTables(SKBitmap pkImage, SKBitmap fkImage, Dictionary<string, SKBitmap> icons)
        {
            int currentHeight = TopPadding;
            int currentRowMaxHeight = 0;
            int tableIndex = 0;


            foreach (var dbTable in DBTableInfo)
            {
                int tableHeight = dbTable.FieldInfos.Count * RowHeight;
                currentRowMaxHeight = Math.Max(currentRowMaxHeight, tableHeight);

                int countTable = 0;
                Canvas.DrawText(dbTable.TableName, new SKPoint(LeftPadding + (TableWidth + LeftPadding) * (tableIndex % ColumnCount), TopPadding + countTable * RowHeight + currentHeight - 8), DrawingHelper.LargeTextPaint);


                foreach (var field in dbTable.FieldInfos)
                {
                    int tableCellX = LeftPadding + (TableWidth + LeftPadding) * (tableIndex % ColumnCount);
                    int tableCellY = TopPadding + countTable * RowHeight + currentHeight;

                    // For PK/FK Images
                    int x = tableCellX + TableWidth - 40;
                    int y = tableCellY + 8;

                    if (field.IsPrimaryKey)
                    {


                        Canvas.DrawBitmap(pkImage, new SKPoint(x, y));

                        //primaryKeys.Add(dbTable.TableName, new Point(x + 35, y + 4));
                    }

                    //Brush brush = new SolidBrush(Color.FromArgb(128, 137, 153, 162)); // TODO move above

                    // TODO Find coresponding function
                    //Canvas.FillRect(brush, new SKRect(LeftPadding + (TableWidth + LeftPadding) * (tableIndex % ColumnCount), TopPadding + countTable * RowHeight + currentHeight, TableWidth, RowHeight));

                    // Border
                    Canvas.DrawRect(new SKRect(tableCellX, tableCellY, tableCellX + TableWidth, tableCellY + RowHeight), new SKPaint()
                    {
                        Color = new SKColor(255, 255, 255),
                        Style = SKPaintStyle.Stroke
                    });

                    Canvas.DrawRect(new SKRect(tableCellX, tableCellY, tableCellX + TableWidth, tableCellY + RowHeight), new SKPaint()
                    {
                        Color = new SKColor(255, 255, 255, 50)
                    });

                    var type = field.Type.Replace("EGER", "");

                    Canvas.DrawText($"{field.Name} ({type}{(field.Nullable ? ", NULL" : "")})", new SKPoint(tableCellX + 25 /* for the icon*/ + 8, tableCellY + RowHeight / 2 + (int)(DrawingHelper.TitleTextPaint.TextSize / 2)), DrawingHelper.TitleTextPaint);

                    if (icons.ContainsKey(field.GeneralType))
                    {
                        var bitmap = icons[field.GeneralType];
                        // TODO the resize once
                        var icon = bitmap.Resize(new SKSizeI(25, 25), SKFilterQuality.High);

                        Canvas.DrawBitmap(icon, tableCellX + 5, tableCellY + 5);
                    }

                    if (field.IsForeignKey && !field.IsPrimaryKey)
                    {
                        Canvas.DrawBitmap(fkImage, x, y);

                        //if (primaryKeys.ContainsKey(field.ForeignKeyInfo.ToTable))
                        //{
                        //Graphics.DrawLine(p, new Point(leftPadding + (width + leftPadding) * count + width - 35, topPadding + countTable * height + custHeight + 8), primaryKeys[field.ForeignKeyInfo.ToTable]);
                        //}
                    }
                    countTable++;
                }

                tableIndex++;

                if (tableIndex % ColumnCount == 0)
                {
                    currentHeight += currentRowMaxHeight + TopPadding;
                    currentRowMaxHeight = 0;
                }
            }

            currentHeight += currentRowMaxHeight + TopPadding * 2;
            return currentHeight;
        }

        private void DrawRelations(List<KeyValuePair<string, SKPoint>> primaryKeys)
        {
            int currentHeight = TopPadding;
            int currentRowMaxHeight = 0;
            int tableIndex = 0;

            foreach (var dbTable in DBTableInfo)
            {
                int countTableRow = 0;
                foreach (var field in dbTable.FieldInfos)
                {
                    int tableHeight = dbTable.FieldInfos.Count * RowHeight;
                    currentRowMaxHeight = Math.Max(currentRowMaxHeight, tableHeight);

                    if (field.IsForeignKey /*&& !field.IsPrimaryKey*/ /*Disabled for PostgreSQL*/)
                    {
                        var tablePrimaryKeys = primaryKeys.Where(i => i.Key == field.ForeignKeyInfo.ToTable);
                        if (tablePrimaryKeys.Count() > 0)
                        {
                            var pkPos = tablePrimaryKeys.First();

                            if (tablePrimaryKeys.Count() > 1)
                            {
                                // composite PK -> match by name
                                // TODO

                                // workaround since only one coposite key for now but TODO FIX
                                pkPos = field.Name == "YPos" ? tablePrimaryKeys.Last() : pkPos;
                            }

                            var p1 = new SKPoint(LeftPadding + (TableWidth + LeftPadding) * (tableIndex % ColumnCount) + TableWidth, TopPadding + countTableRow * RowHeight + currentHeight + RowHeight / 2);
                            var p2 = pkPos.Value;


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
                                p1.X -= TableWidth;
                                ang1 = 135;
                                ang2 = -135;
                            }
                            else
                            {
                                p2.X -= TableWidth;
                                ang1 = 0;
                                ang2 = 0;
                            }



                            var len = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));

                            var length = (double)len * (1 / (double)3);

                            var p11 = new SKPoint((int)(p1.X + Math.Cos(ang1) * length), (int)(p1.Y + Math.Sin(ang1) * length));
                            var p12 = new SKPoint((int)(p2.X - Math.Cos(ang2) * length), (int)(p2.Y + Math.Sin(ang2) * length));


                            using (SKPath path = new SKPath())
                            {
                                path.MoveTo(p1);
                                path.CubicTo(p11, p12, p2);

                                Canvas.DrawPath(path, new SKPaint() { Color = new SKColor(255, 0, 0), IsStroke = true, StrokeWidth = 4, IsAntialias = true, StrokeCap = SKStrokeCap.Square });
                            }

                            //Canvas.DrawLine(p1, p2, new SKPaint() { Color  = new SKColor(255, 0, 0), StrokeWidth = 3, BlendMode = SKBlendMode.Lighten});
                        }
                        else
                        {

                        }
                    }

                    countTableRow++;
                }

                tableIndex++;

                if (tableIndex % ColumnCount == 0)
                {
                    currentHeight += currentRowMaxHeight + TopPadding;
                    currentRowMaxHeight = 0;
                }
            }
        }

        private List<KeyValuePair<string, SKPoint>> GeneratePrimaryKeyPositions()
        {
            // TODO remove constants

            List<KeyValuePair<string, SKPoint>> primaryKeys = new List<KeyValuePair<string, SKPoint>>();

            int currentHeight = TopPadding;
            int currentRowMaxHeight = 0;
            int tableIndex = 0;

            foreach (var dbTable in DBTableInfo)
            {
                int tableHeight = dbTable.FieldInfos.Count * RowHeight;
                currentRowMaxHeight = Math.Max(currentRowMaxHeight, tableHeight);

                int tableRow = 0;
                foreach (var field in dbTable.FieldInfos)
                {
                    if (field.IsPrimaryKey)
                    {
                        int x = LeftPadding + (TableWidth + LeftPadding) * (tableIndex % ColumnCount) + TableWidth - 35;
                        int y = currentHeight + tableRow * RowHeight + TopPadding + RowHeight / 2;

                        primaryKeys.Add(new KeyValuePair<string, SKPoint>(dbTable.TableName, new SKPoint(x + 35, y + 4)));
                    }

                    tableRow++;
                }

                tableIndex++;

                if (tableIndex % ColumnCount == 0)
                {
                    currentHeight += currentRowMaxHeight + TopPadding;
                    currentRowMaxHeight = 0;
                }
            }

            return primaryKeys;
        }
    }
}
