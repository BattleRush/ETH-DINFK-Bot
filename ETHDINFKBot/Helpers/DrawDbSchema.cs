//using ETHDINFKBot.Drawing;
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Drawing.Drawing2D;
//using System.Drawing.Imaging;
//using System.IO;
//using System.Linq;
//using System.Text;

//namespace ETHDINFKBot.Helpers
//{
//    // SYSTEM.DRAWING
//    public class DrawDbSchema : IDisposable
//    {
//        public Bitmap Bitmap; // to get stream maybe change a bit to a method in this class to give the stream
//        private Graphics Graphics;
//        private List<DBTableInfo> DBTableInfo;


//        /* SETTINGS */

//        private int TopPadding = 50;
//        private int LeftPadding = 50;
//        private int TableWidth = 400;
//        private int ColumnCount = 5;

//        private int RowHeight = 35;

//        private Pen RelationPen = new Pen(Color.FromArgb(172, 224, 128, 0), 15);
//        private Font TextFont = new Font("Arial", 11);
//        private Font TitleFont = new Font("Arial", 16);
//        private Brush WhiteBrush = new SolidBrush(Color.White);

//        public DrawDbSchema(List<DBTableInfo> dbInfos)
//        {
//            Bitmap = new Bitmap(ColumnCount * (TableWidth + LeftPadding) + LeftPadding, 10000); // TODO insert into constructor
//            Graphics = Graphics.FromImage(Bitmap);
//            Graphics.SmoothingMode = SmoothingMode.AntiAlias;
//            DBTableInfo = dbInfos;
//            Graphics.Clear(DrawingHelper.DiscordBackgroundColor);
//        }

//        public void Dispose()
//        {
//            Bitmap.Dispose();
//            Graphics.Dispose();
//        }

//        //https://stackoverflow.com/questions/2265910/convert-an-image-to-grayscale
//        public static Bitmap MakeGrayscale3(Bitmap original)
//        {
//            //create a blank bitmap the same size as original
//            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

//            //get a graphics object from the new image
//            using (Graphics g = Graphics.FromImage(newBitmap))
//            {

//                //create the grayscale ColorMatrix
//                ColorMatrix colorMatrix = new ColorMatrix(
//                   new float[][]
//                   {
//             new float[] {.3f, .3f, .3f, 0, 0},
//             new float[] {.59f, .59f, .59f, 0, 0},
//             new float[] {.11f, .11f, .11f, 0, 0},
//             new float[] {0, 0, 0, 1, 0},
//             new float[] {0, 0, 0, 0, 1}
//                   });

//                //create some image attributes
//                using (ImageAttributes attributes = new ImageAttributes())
//                {

//                    //set the color matrix attribute
//                    attributes.SetColorMatrix(colorMatrix);

//                    //draw the original image on the new image
//                    //using the grayscale color matrix
//                    g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
//                                0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
//                }
//            }
//            return newBitmap;
//        }


//        // TODO rework and cleanup
//        public void DrawAllTables()
//        {
            
//            //Pen p = new Pen(b);

//            //Brush b2 = new SolidBrush(Color.Red);


//#if DEBUG
//            RelationPen = new Pen(Color.FromArgb(172, 128, 224, 0), 15);
//#endif
//            RelationPen.Width = 4;

            
//            Font drawFont2 = new Font("Arial", 16);
//            SolidBrush drawBrush = new SolidBrush(Color.Black);


//            string pathToImage = "";

//#if DEBUG
//            pathToImage = Path.Combine("Images", "keyicon.png");
//#else
//            pathToImage = Path.Combine(Program.BasePath, "Images", "keyicon.png");
//#endif

//            var key = new Bitmap(pathToImage);

//            // possible linux fix for green tint
//            key.MakeTransparent();


//            var fkKey = MakeGrayscale3(key);

//            var primaryKeys = GeneratePrimaryKeyPositions();

//            DrawRelations(primaryKeys);
//            var maxHeight = DrawTables(key, fkKey);

//            Bitmap = CropImage(Bitmap, new Rectangle(0, 0, ColumnCount * (TableWidth + LeftPadding) + LeftPadding, maxHeight));
//        }

//        private int DrawTables(Bitmap pkImage, Bitmap fkImage)
//        {
//            int currentHeight = TopPadding;
//            int currentRowMaxHeight = 0;
//            int tableIndex = 0;

//            Pen p = new Pen(WhiteBrush);

//            foreach (var dbTable in DBTableInfo)
//            {
//                int tableHeight = dbTable.FieldInfos.Count * RowHeight;
//                currentRowMaxHeight = Math.Max(currentRowMaxHeight, tableHeight);

//                int countTable = 0;
//                Graphics.DrawString(dbTable.TableName, TitleFont, WhiteBrush, new Point(LeftPadding + (TableWidth + LeftPadding) * (tableIndex % ColumnCount), TopPadding + countTable * RowHeight + currentHeight - 35));


//                foreach (var field in dbTable.FieldInfos)
//                {
//                    if (field.IsPrimaryKey)
//                    {
//                        int x = LeftPadding + (TableWidth + LeftPadding) * (tableIndex % ColumnCount) + TableWidth - 35;
//                        int y = TopPadding + countTable * RowHeight + currentHeight + 8;

//                        Graphics.DrawImage(pkImage, x, y, 30, 20);

//                        //primaryKeys.Add(dbTable.TableName, new Point(x + 35, y + 4));
//                    }
//                    Brush brush = new SolidBrush(Color.FromArgb(128, 137, 153, 162)); // TODO move above
//                    Graphics.FillRectangle(brush, new Rectangle(LeftPadding + (TableWidth + LeftPadding) * (tableIndex % ColumnCount), TopPadding + countTable * RowHeight + currentHeight, TableWidth, RowHeight));
//                    Graphics.DrawRectangle(p, new Rectangle(LeftPadding + (TableWidth + LeftPadding) * (tableIndex % ColumnCount), TopPadding + countTable * RowHeight + currentHeight, TableWidth, RowHeight));

//                    var type = field.Type.Replace("EGER", "");

//                    Graphics.DrawString($"{field.Name} ({type}{(field.Nullable ? ", NULL" : "")})",
//                        TextFont, WhiteBrush, new Point(LeftPadding + (TableWidth + LeftPadding) * (tableIndex % ColumnCount) + 10, TopPadding + countTable * RowHeight + currentHeight + 10));

//                    if (field.IsForeignKey && !field.IsPrimaryKey)
//                    {
//                        Graphics.DrawImage(fkImage, LeftPadding + (TableWidth + LeftPadding) * (tableIndex % ColumnCount) + TableWidth - 35, TopPadding + countTable * RowHeight + currentHeight + 8, 30, 20);

//                        //if (primaryKeys.ContainsKey(field.ForeignKeyInfo.ToTable))
//                        //{
//                            //Graphics.DrawLine(p, new Point(leftPadding + (width + leftPadding) * count + width - 35, topPadding + countTable * height + custHeight + 8), primaryKeys[field.ForeignKeyInfo.ToTable]);
//                        //}
//                    }
//                    countTable++;
//                }

//                tableIndex++;

//                if (tableIndex % ColumnCount == 0)
//                {
//                    currentHeight += currentRowMaxHeight + TopPadding;
//                    currentRowMaxHeight = 0;
//                }
//            }

//            currentHeight += currentRowMaxHeight + TopPadding*2;
//            return currentHeight;
//        }

//        private void DrawRelations(List<KeyValuePair<string, Point>> primaryKeys)
//        {
//            int currentHeight = TopPadding;
//            int currentRowMaxHeight = 0;
//            int tableIndex = 0;

//            foreach (var dbTable in DBTableInfo)
//            {
//                int countTableRow = 0;
//                foreach (var field in dbTable.FieldInfos)
//                {
//                    int tableHeight = dbTable.FieldInfos.Count * RowHeight;
//                    currentRowMaxHeight = Math.Max(currentRowMaxHeight, tableHeight);

//                    if (field.IsForeignKey && !field.IsPrimaryKey)
//                    {
//                        var tablePrimaryKeys = primaryKeys.Where(i => i.Key == field.ForeignKeyInfo.ToTable);
//                        if (tablePrimaryKeys.Count() > 0)
//                        {
//                            var pkPos = tablePrimaryKeys.First();

//                            if(tablePrimaryKeys.Count() > 1)
//                            {
//                                // composite PK -> match by name
//                                // TODO

//                                // workaround since only one coposite key for now but TODO FIX
//                                pkPos = field.Name == "YPos" ? tablePrimaryKeys.Last() : pkPos;
//                            }

//                            var p1 = new Point(LeftPadding + (TableWidth + LeftPadding) * (tableIndex % ColumnCount) + TableWidth, TopPadding + countTableRow * RowHeight + currentHeight + 20);
//                            var p2 = pkPos.Value;


//                            var ang1 = 135;
//                            var ang2 = -135;

//                            if (p1.X == p2.X)
//                            {
//                                //p1.X += width;
//                                ang1 = 0;
//                                ang2 = 180;
//                            }
//                            else if (p1.X >= p2.X)
//                            {
//                                p1.X -= TableWidth;
//                                ang1 = 135;
//                                ang2 = -135;
//                            }
//                            else
//                            {
//                                p2.X -= TableWidth;
//                                ang1 = 0;
//                                ang2 = 0;
//                            }



//                            var len = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));

//                            var length = (double)len * (1 / (double)3);
       
//                            var p11 = new Point((int)(p1.X + Math.Cos(ang1) * length), (int)(p1.Y + Math.Sin(ang1) * length));
//                            var p12 = new Point((int)(p2.X - Math.Cos(ang2) * length), (int)(p2.Y + Math.Sin(ang2) * length));


//                            Graphics.DrawBezier(RelationPen, p1, p11, p12, p2);
//                        }
//                        else
//                        {

//                        }
//                    }

//                    countTableRow++;
//                }

//                tableIndex++;

//                if (tableIndex % ColumnCount == 0)
//                {
//                    currentHeight += currentRowMaxHeight + TopPadding;
//                    currentRowMaxHeight = 0;
//                }
//            }
//        }

//        private List<KeyValuePair<string, Point>> GeneratePrimaryKeyPositions()
//        {
//            // TODO remove constants

//            List<KeyValuePair<string, Point>> primaryKeys = new List<KeyValuePair<string, Point>>();

//            int currentHeight = TopPadding;
//            int currentRowMaxHeight = 0;
//            int tableIndex = 0;

//            foreach (var dbTable in DBTableInfo)
//            {
//                int tableHeight = dbTable.FieldInfos.Count * RowHeight;
//                currentRowMaxHeight = Math.Max(currentRowMaxHeight, tableHeight);

//                int tableRow = 0;
//                foreach (var field in dbTable.FieldInfos)
//                {
//                    if (field.IsPrimaryKey)
//                    {
//                        int x = LeftPadding + (TableWidth + LeftPadding) * (tableIndex % ColumnCount) + TableWidth - 35;
//                        int y = currentHeight + tableRow * RowHeight + tableRow + 8 + TopPadding;

//                        primaryKeys.Add(new KeyValuePair<string, Point>(dbTable.TableName, new Point(x + 35, y + 4)));
//                    }

//                    tableRow++;
//                }

//                tableIndex++;

//                if (tableIndex % ColumnCount == 0)
//                {
//                    currentHeight += currentRowMaxHeight + TopPadding;
//                    currentRowMaxHeight = 0;
//                }
//            }

//            return primaryKeys;
//        }


//        // TODO Duplicate 
//        private static Bitmap CropImage(Bitmap bmpImage, Rectangle cropArea)
//        {
//            return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
//        }
//    }
//}
