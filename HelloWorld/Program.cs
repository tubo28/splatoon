using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Diagnostics;

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
    }

    class Program
    {

        static Template MatchStartTemplate = null;
        static Template MatchEndTemlate = null;
        static long babyStep = 500;
        static long giantStep = 2000;

        static void Main(string[] args)
        {
            MatchStartTemplate = new Template(Cv2.ImRead("waku.png"), new Rect(688, 289, 541, 250));
            MatchEndTemlate = new Template(Cv2.ImRead("p.png"), new Rect(237, 68, 50, 83));
            

            if (false)
            {
                var sw = new Stopwatch();
                sw.Start();
                var file = @"2018030321470048.mp4";
                var video = new VideoCapture(file);

                long[] matchEndTimes =
                {
                    0000406000,
                    0000802000,
                    0001506000,
                    0002130000,
                    0002910000,
                    0003478000,
                    0004116000,
                    0004682000,
                    0005168000,
                };

                foreach (var time in matchEndTimes)
                {
                    var mat = new Mat();
                    GetFrameByMsec(video, time, mat);
                    var frame = video.Get(CaptureProperty.PosFrames);
                    var bin = new Mat();
                    Binarize(mat, 3, bin);
                    Cv2.ImWrite($"bin_{frame}.png", bin);
                }
            }
            else
            {
                var sw = new Stopwatch();
                sw.Start();
                var file = @"2018030321470048.mp4";
                var video = new VideoCapture(file);

                var srcFrame = new Mat();
                var binFrame = new Mat();
                var searchFrame = new Mat();
                var searchBinFrame = new Mat();

                var posMsec = 0L;
                while (true)
                {
                    
                    var nextPosMsec = posMsec + giantStep;

                    GetFrameByMsec(video, posMsec, srcFrame);
                    if (srcFrame.Empty())
                    {
                        break;
                    }

                    Binarize(srcFrame, 7, binFrame);
                    if (MatchStartTemplate.Match(binFrame))
                    {
                        Console.WriteLine($"match starts at {video.Get(CaptureProperty.PosFrames)} frm");
                        var t = FindMatchStart(video, searchFrame, searchBinFrame, posMsec);
                    }
                    else if(MatchEndTemlate.Match(binFrame))
                    {
                        Console.WriteLine($"match ends at   {video.Get(CaptureProperty.PosFrames)} frm");
                        nextPosMsec = FindMatchEnd(video, searchFrame, searchBinFrame, posMsec);
                    }
                    posMsec = nextPosMsec;
                }
            }
        }

        private static long FindMatchEnd(VideoCapture video, Mat searchFrame, Mat searchBinFrame, long fromPosMsec)
        {
            long res = 0;
            var m = fromPosMsec;
            while (true)
            {
                var tryMsec = m + babyStep;
                if (tryMsec >= video.Fps * video.FrameCount)
                {
                    break;
                }

                res = tryMsec + giantStep;
                GetFrameByMsec(video, tryMsec, searchFrame);
                Binarize(searchFrame, 7, searchBinFrame);
                if (!MatchEndTemlate.Match(searchBinFrame))
                {
                    break;
                }
                else
                {
                    Console.WriteLine($"match ends at   {video.Get(CaptureProperty.PosFrames)} frm");
                }
                m = tryMsec;
            }
            return res;
        }

        private static long FindMatchStart(VideoCapture video, Mat searchFrame, Mat searchBinFrame, long fromPosMsec)
        {
            var res = fromPosMsec;
            var m = fromPosMsec;
            while (true)
            {
                var tryMsec = m - babyStep;
                if (tryMsec < 0)
                {
                    break;
                }

                GetFrameByMsec(video, tryMsec, searchFrame);
                Binarize(searchFrame, 7, searchBinFrame);
                if (!MatchStartTemplate.Match(searchBinFrame))
                {
                    break;
                }
                else
                {
                    Console.WriteLine($"match starts at {video.Get(CaptureProperty.PosFrames)} frm");
                    res = tryMsec;
                }
                m = tryMsec;
            }
            return res;
        }

        static void GetFrameByMsec(VideoCapture video, long ms, Mat res)
        {
            video.Set(CaptureProperty.PosMsec, ms);
            video.Read(res);
        }

        static Mat tmp = new Mat();
        static void Binarize(Mat mat, int blockSize, Mat res)
        {
            Cv2.CvtColor(mat, tmp, ColorConversionCodes.RGB2GRAY);
            Binarizer.Sauvola(tmp, res, blockSize, 0.15, 32);
        }
    }
}
