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
                    var tryTime = m - babyStep;
                    if (tryTime < TimeSpan.Zero)
                    {
                        break;
                    }

                    GetFrame(video, tryTime, searchFrame);
                    Binarize(searchFrame, 7, searchBinFrame);
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
                    var tryTime = m + babyStep;
                    if (tryTime >= TimeSpan.FromSeconds(video.FrameCount/ video.Fps))
                    {
                        break;
                    }

                    GetFrame(video, tryTime, searchFrame);
                    Binarize(searchFrame, 7, searchBinFrame);
                    if (!Match(searchBinFrame))
                    {
                        break;
                    }
                    m = tryTime;
                }
                return m;
            }
        }

        static Template GameStartTemplate = null;
        static Template GameEndTemlate = null;

        static TimeSpan babyStep = TimeSpan.FromMilliseconds(250);
        static TimeSpan giantStep = TimeSpan.FromMilliseconds(2000);

        static void Main(string[] args)
        {
            GameStartTemplate = new Template(Cv2.ImRead("waku.png"), new Rect(688, 289, 541, 250));
            GameEndTemlate = new Template(Cv2.ImRead("p.png"), new Rect(237, 68, 50, 83));

            if (false)
            {
                /*
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
                */
            }
            else
            {
                var sw = new Stopwatch();
                sw.Start();
                var file = @"2018030321470048.mp4";
                var video = new VideoCapture(file);

                var srcFrame = new Mat();
                var binFrame = new Mat();

                var list = new System.Collections.Generic.List<Tuple<TimeSpan, TimeSpan>>();

                var s = new TimeSpan();
                var pos = TimeSpan.Zero;

                while (true)
                {
                    var nextPos = pos + giantStep;

                    GetFrame(video, pos, srcFrame);
                    if (srcFrame.Empty())
                    {
                        break;
                    }

                    Binarize(srcFrame, 7, binFrame);
                    if (GameStartTemplate.Match(binFrame))
                    {
                        var se = GameStartTemplate.FindMatchBoundary(video, pos);
                        var start = se.Item1;
                        var end = se.Item2;
                        Console.WriteLine($"{start}: game starts");
                        Console.WriteLine(sw.Elapsed);
                        nextPos = end + babyStep;
                        s = start;
                    }
                    else if (GameEndTemlate.Match(binFrame))
                    {
                        var se = GameEndTemlate.FindMatchBoundary(video, pos);
                        var end = se.Item2;
                        Console.WriteLine($"${end}: game ends");
                        Console.WriteLine(sw.Elapsed);
                        nextPos = end + babyStep;
                        list.Add(Tuple.Create(s, end));
                        Console.WriteLine($"{s} -> ${end}");
                        s = new TimeSpan();
                    }
                    pos = nextPos;
                }

                list.ForEach(x => Console.WriteLine(x));

                Console.WriteLine(sw.Elapsed);
            }
        }

        static void GetFrame(VideoCapture video, TimeSpan pos, Mat res)
        {
            video.Set(CaptureProperty.PosMsec, pos.TotalMilliseconds);
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
