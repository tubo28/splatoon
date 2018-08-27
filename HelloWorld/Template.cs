using OpenCvSharp;
using System;

namespace HelloWorld
{
    class Template
    {
        Mat binMat;
        readonly byte[] temporaryArray;
        readonly byte[] array;
        readonly int size;

        Rect rect;

        public Template(Mat mat, Rect rect)
        {
            binMat = Mat.Zeros(new Size(mat.Width, mat.Height), MatType.CV_8SC1);
            for (int i = 0; i < mat.Height; i++)
            {
                for (int j = 0; j < mat.Width; j++)
                {
                    var colorVec = mat.Get<Vec3b>(i, j);
                    var c = (byte)(Math.Max(colorVec[0], Math.Max(colorVec[1], colorVec[2])) == 0 ? 0 : 255);
                    binMat.Set(i, j, new Vec3b(c, c, c));
                }
            }

            size = binMat.Height * binMat.Width;

            array = new byte[size];
            binMat.GetArray(0, 0, array);

            temporaryArray = new byte[size];

            this.rect = rect;
        }

        public bool Match(Mat binMat)
        {
            return Likelihood(binMat) >= 0.7;
        }

        double Likelihood(Mat binMat)
        {
            var cropped = new Mat(binMat, rect);
            cropped.GetArray(0, 0, temporaryArray);

            var den = 0;
            var nume = 0;
            for (int i = 0; i < size; i++)
            {
                if (array[i] == 0)
                {
                    den++;
                    if (temporaryArray[i] == 0)
                    {
                        nume++;
                    }
                }
            }
            return 1.0 * nume / den;
        }

        public Tuple<TimeSpan, TimeSpan> FindMatchBoundary(VideoCapture video, TimeSpan pos)
        {
            return Tuple.Create(FindMatchStart(video, pos), FindMatchEnd(video, pos));
        }

        static Mat searchFrame = new Mat();
        static Mat searchBinFrame = new Mat();

        private TimeSpan FindMatchStart(VideoCapture video, TimeSpan from)
        {
            var m = from;
            while (true)
            {
                var tryTime = m - Params.babyStep;
                if (tryTime < TimeSpan.Zero)
                {
                    break;
                }

                CvUtil.GetFrame(video, tryTime, searchFrame);
                CvUtil.Binarize(searchFrame, 7, searchBinFrame);
                if (!Match(searchBinFrame))
                {
                    break;
                }
                m = tryTime;
            }
            return m;
        }

        private TimeSpan FindMatchEnd(VideoCapture video, TimeSpan from)
        {
            var m = from;
            while (true)
            {
                var tryTime = m + Params.babyStep;
                if (tryTime >= TimeSpan.FromSeconds(video.FrameCount / video.Fps))
                {
                    break;
                }

                CvUtil.GetFrame(video, tryTime, searchFrame);
                CvUtil.Binarize(searchFrame, 7, searchBinFrame);
                if (!Match(searchBinFrame))
                {
                    break;
                }
                m = tryTime;
            }
            return m;
        }
    }

}
