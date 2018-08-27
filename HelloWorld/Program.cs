using OpenCvSharp;
using System;
using System.Diagnostics;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();

            var file = args.Length != 0 ? args[0] : @"2018030321470048.mp4";
            var video = new VideoCapture(file);

            var srcFrame = new Mat();
            var binFrame = new Mat();

            var list = new System.Collections.Generic.List<Tuple<TimeSpan, TimeSpan>>();

            var prevStart = new TimeSpan();
            var pos = TimeSpan.Zero;

            while (true)
            {
                var nextPos = pos + Params.giantStep;

                CvUtil.GetFrame(video, pos, srcFrame);
                if (srcFrame.Empty())
                {
                    break;
                }

                CvUtil.Binarize(srcFrame, 7, binFrame);
                if (Params.GameStartTemplate.Match(binFrame))
                {
                    var se = Params.GameStartTemplate.FindMatchBoundary(video, pos);
                    var start = se.Item1;
                    var end = se.Item2;
                    Console.WriteLine($"{start}: game starts");
                    nextPos = end + Params.babyStep;
                    prevStart = start;
                }
                else if (Params.GameEndTemlate.Match(binFrame))
                {
                    var se = Params.GameEndTemlate.FindMatchBoundary(video, pos);
                    var end = se.Item2;
                    Console.WriteLine($"${end}: game ends");
                    nextPos = end + Params.babyStep;
                    FfMpegUtil.Cut(prevStart, end, "out");
                    prevStart = new TimeSpan();
                }
                pos = nextPos;
            }
            Console.WriteLine(sw.Elapsed);
        }
    }
}
