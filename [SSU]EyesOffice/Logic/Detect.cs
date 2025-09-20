using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
namespace _SSU_EyesOffice.Logic
{
    public class Detect
    {
        public static PointF DetectMean(Image<Gray, Byte> img, Rectangle roi)
        {
            int totalX = 0;
            int totalY = 0;
            int cnt = 0;
            for (int y = roi.Y; y < roi.Y + roi.Height; y++)
            {
                for (int x = roi.X; x < roi.X + roi.Width; x++)
                {
                    if(img.Data[y, x, 0] > 0)
                    {
                        totalX += x;
                        totalY += y;
                        cnt++; 
                    }
                }
            }

            if (cnt <= 0)
            {
                return PointF.Empty;
            }

            return new PointF(totalX / cnt, totalY / cnt);
        }

        public static PointF DetectMedian(Image<Gray, Byte> img, Rectangle roi)
        {
            var totalX = new List<int>();
            var totalY = new List<int>();
            for (int y = roi.Y; y < roi.Y + roi.Height; y++)
            {
                for (int x = roi.X; x < roi.X + roi.Width; x++)
                {
                    if (img.Data[y, x, 0] > 0)
                    {
                        totalX.Add(x);
                        totalY.Add(y);
                    }
                }
            }

            if (totalX.Count <= 0 || totalY.Count <= 0)
            {
                return PointF.Empty;
            }

            totalX.Sort();
            totalY.Sort();

            return new PointF(totalX[totalX.Count / 2], totalY[totalY.Count / 2]);
        }

        public static PointF EyeCenter(Image<Gray, Byte> imgBytes)
        {
            double max = 0;
            var maxPoint = System.Drawing.Point.Empty;

            for (int y = 2; y < imgBytes.Height - 3; y++)
            {
                for (int x = 2; x < imgBytes.Width - 3; x++)
                {
                    var cur = CenterFunc(imgBytes, y, x);
                    if (cur > max)
                    {
                        max = cur;
                        maxPoint.X = x;
                        maxPoint.Y = y;
                    }
                }
            }

            return maxPoint;
        }

        private static double CenterFunc(Image<Gray, Byte> img, int i0, int j0)
        {
            double res = 0;

            double w = 255 - img.Data[i0, j0, 0];

            for (int i = 0; i < i0 - 1; i++)
            {
                for (int j = 0; j < j0 - 1; j++)
                {
                    //if (i == i0 || j == j0)
                    //{
                    //    continue;
                    //}

                    var gradient = new Vector(
                        img.Data[i + 1, j, 0] - img.Data[i, j, 0],
                        img.Data[i, j + 1, 0] - img.Data[i, j, 0]
                        );

                    var length = Math.Sqrt(Math.Pow(i - i0, 2) + Math.Pow(j - j0, 2));

                    var f = new Vector(
                       (i - i0) / length,
                       (j - j0) / length
                       );

                    res += (w / Math.Sqrt(1.0 + (Math.Pow(i - i0, 2) / Math.Pow(j - j0, 2)))) * Vector.Multiply(gradient, f);
                }
            }

            return res;
        }
    }
}
