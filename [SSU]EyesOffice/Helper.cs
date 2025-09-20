using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _SSU_EyesOffice
{
    public static class Helper
    {
        public static Rectangle Reduce(this Rectangle rect, double factor)
        {
            return new Rectangle((int)(rect.X / factor), (int)(rect.Y / factor), (int)(rect.Width / factor), (int)(rect.Height / factor));
        }

        public static Image<Bgr, Byte> Reduce(this Image<Bgr, Byte> img, double reduceFactor)
        {
            var imageSmall = new Image<Bgr, byte>(new Size((int)(img.Width / reduceFactor), (int)(img.Height / reduceFactor)));
            CvInvoke.cvResize(img.Ptr, imageSmall.Ptr, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            return imageSmall;
        }

        public static PointF Scale(this PointF pt, double factor)
        {
            return new PointF((float)(pt.X * factor), (float)(pt.Y * factor));
        }

        public static float Mediane(this float[] arr)
        {
            var list = arr.ToList();
            list.Sort();
            return list[list.Count / 2];
        }
    }
}
