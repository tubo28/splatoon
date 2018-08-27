using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;

namespace HelloWorld
{
    class CvUtil
    {
        public static void GetFrame(VideoCapture video, TimeSpan pos, Mat res)
        {
            video.Set(CaptureProperty.PosMsec, pos.TotalMilliseconds);
            video.Read(res);
        }

        static Mat tmp = new Mat();

        public static void Binarize(Mat mat, int blockSize, Mat res)
        {
            Cv2.CvtColor(mat, tmp, ColorConversionCodes.RGB2GRAY);
            Binarizer.Sauvola(tmp, res, blockSize, 0.15, 32);
        }
    }
}
