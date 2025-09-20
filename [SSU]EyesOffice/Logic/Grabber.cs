using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _SSU_EyesOffice.Logic
{
    using System.IO;
    using Emgu.CV;
    using Emgu.CV.Structure;

    /// <summary>
    /// Базовый класс для источника изображения
    /// в нашем случае источником является видеофайл
    /// </summary>
    public class Grabber
    {
        /// <summary>самое-самое оригинальное изображение</summary>
        private Image<Bgr, Byte> image;

        /// <summary>устройство захвата</summary>
        private IntPtr capture;

        public int Width
        {
            get
            {
                return (int)CvInvoke.cvGetCaptureProperty(capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH);
            }
        }

        public int Height
        {
            get
            {
                return (int)CvInvoke.cvGetCaptureProperty(capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT);
            }
        }

        public double Fps
        {
            get
            {
                return CvInvoke.cvGetCaptureProperty(capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FPS);
            }
        }

        /// <summary>Длина видео-файла</summary>
        public int Length
        {
            get
            {
                return (int)CvInvoke.cvGetCaptureProperty(this.capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_COUNT);
            }
        }

        public int WebCamIndex
        {
            get;
            private set;
        }

        /// <summary>Оригинальное изображение</summary>
        public Image<Bgr, Byte> Image
        {
            get
            {
                return this.image;
            }
        }

        /// <summary>
        /// Возвращает текущую позицию в видеопотоке
        /// </summary>
        /// <returns></returns>
        public int GetFramePos()
        {
            return (int)CvInvoke.cvGetCaptureProperty(this.capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_FRAMES);
        }

        /// <summary>
        /// Возвращает текущую позицию в видеопотоке
        /// </summary>
        /// <returns></returns>
        public int GetTimePos()
        {
            return (int)CvInvoke.cvGetCaptureProperty(this.capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_MSEC);
        }

        /// <summary>
        /// Задаем устройство, откуда будем захватывать изображения
        /// если указан индекс для веб-камеры videoFilePath игнорируется
        /// </summary>
        /// <param name="videoFilePath">путь к файлу</param>
        /// /// <param name="webCamIndex">индекс веб-камеры</param>
        public bool SetCapture(string videoFilePath, int webCamIndex = -1)
        {
            this.WebCamIndex = webCamIndex;
            this.capture = IntPtr.Zero;
            if (webCamIndex < 0)
            {
                if (!File.Exists(videoFilePath))
                {
                    throw new FileNotFoundException($"Couldn't create capture: file {videoFilePath} was not found");
                }
                else
                {
                    this.capture = CvInvoke.cvCreateFileCapture(videoFilePath);
                }
            }
            else
            {
                this.capture = CvInvoke.cvCreateCameraCapture(webCamIndex);
            }


            if (this.capture != IntPtr.Zero)
            {
                this.image = new Image<Bgr, byte>(
                    (int)CvInvoke.cvGetCaptureProperty(this.capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH),
                    (int)CvInvoke.cvGetCaptureProperty(this.capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT));

                this.GrabImage();
                int pos1 = (int)CvInvoke.cvGetCaptureProperty(this.capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_FRAMES);
                this.GrabImage();
                int pos2 = (int)CvInvoke.cvGetCaptureProperty(this.capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_FRAMES);
                if (webCamIndex < 0 && (pos2 - pos1 != 1 || (int)CvInvoke.cvGetCaptureProperty(this.capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_FRAMES) < 0))
                {
                    throw new FileLoadException($"Not supported file");
                }

            }
            else
            {
                throw new Exception($"Couldn't create capture. Not supported file or stream");
            }

            this.SetFramePos(0);

            return true;
        }

        /// <summary>
        /// Захватываем очередное изображение с нашего устройства захвата
        /// throw NullReferenceException if couldn't grab
        /// </summary>
        public void GrabImage()
        {
            try
            {
                this.image.Ptr = CvInvoke.cvQueryFrame(this.capture);
                if (this.image.Ptr == IntPtr.Zero)
                {
                    throw new NullReferenceException();
                }
            }
            catch
            {
                throw new NullReferenceException();
            }
        }

        /// <summary>
        /// Задаем позицию в виде с которой будем захватывать в следующий раз
        /// </summary>
        /// <param name="pos">индекс кадра</param>
        public void SetFramePos(int position)
        {
            if (this.WebCamIndex >= 0)
            {
                return;
            }

            if (position < 0 || this.Length < position)
            {
                throw new System.ArgumentOutOfRangeException();
            }

            double positiontoset = position;
            double pos = positiontoset - 12;
            if (pos < 0) pos = 0;
            CvInvoke.cvSetCaptureProperty(this.capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_FRAMES, pos);
            if (positiontoset == 0)
            {
                return;
            }
            while (pos < position)
            {
                GrabImage();
                pos = CvInvoke.cvGetCaptureProperty(this.capture, Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_FRAMES);
                if (pos == position)
                    break;
            }
        }

        /// <summary>
        /// Задаем позицию и захватываем с неё изображение
        /// </summary>
        /// <param name="pos">индекс кадра</param>
        public void GrabImageByPos(int pos)
        {
            if (this.WebCamIndex >= 0)
            {
                return;
            }

            this.SetFramePos(pos);
            this.GrabImage();
        }

        public void ReleaseCapture()
        {
            this.Release();
            try
            {
                CvInvoke.cvReleaseCapture(ref this.capture);
            }
            catch { }
        }

        public void Release()
        {
            if (this.image != null)
            {
                this.Image.Ptr = IntPtr.Zero;
            }
        }
    }
}
