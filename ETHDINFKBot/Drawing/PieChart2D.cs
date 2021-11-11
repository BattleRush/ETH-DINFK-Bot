////using ETHDINFKBot.Helpers;
////using System;
////using System.Collections.Generic;
////using System.Drawing;
////using System.IO;
////using System.Linq;
////using System.Text;
////using System.Threading.Tasks;

////namespace ETHDINFKBot.Drawing
////{
////    public class PieChart2D : IDisposable
////    {
////        private Graphics Graphics;
////        private Bitmap Bitmap;
////        private Padding Padding;
////        private List<string> Labels;
////        private List<int> Data;

////        public PieChart2D(List<string> labels, List<int> data)
////        {
////            var drawInfo = DrawingHelper.GetEmptyGraphics();
////            Graphics = drawInfo.Graphics;
////            Bitmap = drawInfo.Bitmap;

////            Padding = DrawingHelper.DefaultPadding;

////            Labels = labels;
////            Data = data;
////        }

////        public Stream DrawChart()
////        {
////            DrawPieChart();
////            var stream = CommonHelper.GetStream(Bitmap);
////            return stream;
////        }

////        private void DrawPieChart()
////        {
////            var rect = new Rectangle(50, 50, 1000, 1000);

////            Random rnd = new Random();

////            float currAngle = 0;

////            int index = 0;
////            foreach (var item in CalcPercentages())
////            {
////                Color randomColor = Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));

////                var randPen = new Pen(randomColor);
////                var randBrush = new SolidBrush(randomColor);

////                // todo own custom func for lulz
////                //Graphics.DrawPie(randPen, rect, currAngle, currAngle + item);
////                Graphics.FillPie(randBrush, rect, currAngle, item * 360);
////                currAngle += item * 360;
////                index++;
////            }

////        }

////        private IEnumerable<float> CalcPercentages()
////        {
////            int sum = Data.Sum();

////            foreach (var item in Data)
////                yield return item / (float)sum;
////        }

////        public void Dispose()
////        {
////            Graphics.Dispose();
////            Bitmap.Dispose();
////        }
////    }
////}
