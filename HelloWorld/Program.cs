using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Diagnostics;

namespace HelloWorld
{
    class Program
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

            public Tuple<long, long> FindMatchBoundary(VideoCapture video, long framePosMsec)
            {
                return Tuple.Create(FindMatchStart(video, framePosMsec), FindMatchEnd(video, framePosMsec));
            }

            static Mat searchFrame = new Mat();
            static Mat searchBinFrame = new Mat();

            private long FindMatchStart(VideoCapture video, long fromPosMsec)
            {
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
                    if (!Match(searchBinFrame))
                    {
                        break;
                    }
                    Console.WriteLine($"match starts at {video.Get(CaptureProperty.PosFrames)} frm");
                    m = tryMsec;
                }
                return m;
            }

            private long FindMatchEnd(VideoCapture video, long fromPosMsec)
            {
                var m = fromPosMsec;
                while (true)
                {
                    var tryMsec = m + babyStep;
                    if (tryMsec >= video.Fps * video.FrameCount)
                    {
                        break;
                    }

                    GetFrameByMsec(video, tryMsec, searchFrame);
                    Binarize(searchFrame, 7, searchBinFrame);
                    if (!Match(searchBinFrame))
                    {
                        break;
                    }
                    Console.WriteLine($"match ends at   {video.Get(CaptureProperty.PosFrames)} frm");
                    m = tryMsec;
                }
                return m;
            }
        }

        static Template GameStartTemplate = null;
        static Template GameEndTemlate = null;

        static long babyStep = 200;
        static long giantStep = 2500;

        static void Main(string[] args)
        {
            GameStartTemplate = new Template(Cv2.ImRead("waku.png"), new Rect(688, 289, 541, 250));
            GameEndTemlate = new Template(Cv2.ImRead("p.png"), new Rect(237, 68, 50, 83));

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
                    if (GameStartTemplate.Match(binFrame))
                    {
                        var se = GameStartTemplate.FindMatchBoundary(video, posMsec);
                        var start = se.Item1;
                        var end = se.Item2;
                        Console.WriteLine($"{start:0000000}ms: match starts");
                        nextPosMsec = end + babyStep;
                    }
                    else if (GameEndTemlate.Match(binFrame))
                    {
                        var se = GameEndTemlate.FindMatchBoundary(video, posMsec);
                        var end = se.Item2;
                        Console.WriteLine($"{end:0000000}ms: match ends");
                        nextPosMsec = end + babyStep;
                    }
                    posMsec = nextPosMsec;
                }
            }
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
