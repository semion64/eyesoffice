using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _SSU_EyesOffice.Logic
{
    public class GrayBin
    {
        public static Image<Gray, Byte> ToGray(Image<Bgr, Byte> imgColor)
        {
            Image<Gray, Byte> grayImg = new Image<Gray, byte>(imgColor.Width, imgColor.Height);

            CvInvoke.cvCvtColor(imgColor.Ptr, grayImg.Ptr, Emgu.CV.CvEnum.COLOR_CONVERSION.CV_BGR2GRAY);

            return grayImg;
        }

        public static Image<Gray, Byte> Binarize(Image<Gray, Byte> imgGray, int maxLevel, int minLevel = 0)
        {
            Image<Gray, Byte> binImg = new Image<Gray, byte>(imgGray.Width, imgGray.Height);
            CvInvoke.cvInRangeS(imgGray, new MCvScalar(minLevel), new MCvScalar(maxLevel), binImg);
            return binImg;
        }

        public static void BinarizeImmediate(Image<Gray, Byte> imgGray, int maxLevel, int minLevel = 0)
        {
            CvInvoke.cvInRangeS(imgGray, new MCvScalar(minLevel), new MCvScalar(maxLevel), imgGray);
        }

        public static void InvertInRegions(Image<Gray, Byte> imgBin, Rectangle roiLeft, Rectangle roiRight)
        {
            for (int y = 0; y < imgBin.Height; y++)
            {
                for (int x = 0; x < imgBin.Width; x++)
                {
                    if (roiLeft.Contains(x, y) || roiRight.Contains(x, y))
                    {
                        imgBin.Data[y, x, 0] = imgBin.Data[y, x, 0] > 0 ? (byte)0 : (byte)255;
                    }
                    else
                    {
                        imgBin.Data[y, x, 0] = 0;
                    }
                }
            }
        }

        public static Image<Hsv, byte> ToHsv(Image<Bgr, Byte> imgColor)
        {
            var hsv_and = new Image<Hsv, byte>(imgColor.Size);
            CvInvoke.cvCvtColor(imgColor, hsv_and, Emgu.CV.CvEnum.COLOR_CONVERSION.CV_BGR2HSV);

            return hsv_and;
        }

        public static Image<Gray, Byte>[] ToRgbChanels(Image<Bgr, Byte> imgColor)
        {
            Image<Gray, byte>[] chanels = imgColor.Split();
            //chanels[0] = chanels[0].InRange(Rmin, Rmax);
            //chanels[1] = chanels[1].InRange(Gmin, Gmax);
            //chanels[2] = chanels[2].InRange(Bmin, Bmax);
            //result = chanels[0].And(chanels[1]);
            //result = result.And(chanels[2]);
            //result = result.Dilate(dialate);
            return chanels;
        }

        public static Image<Gray, Byte> And(List<Image<Gray, Byte>> images)
        {
            if (images.Count <= 0)
            {
                return null;
            }

            Image<Gray, Byte> result = images[0].Clone();
            foreach (var item in images.Skip(1))
            {
                result = result.And(item);
            }

            return result;
        }

        public static void Smooth(Image<Gray, Byte> img, int level = 5)
        {
            CvInvoke.cvSmooth(img, img, Emgu.CV.CvEnum.SMOOTH_TYPE.CV_GAUSSIAN, level, 0, 0, 0);
        }

        public static void Normalize(Image<Gray, Byte> img, int levelMin = 0, int levelMax=255)
        {
            CvInvoke.cvNormalize(img, img, levelMin, levelMax, Emgu.CV.CvEnum.NORM_TYPE.CV_MINMAX, IntPtr.Zero);
        }

        
    }
}
