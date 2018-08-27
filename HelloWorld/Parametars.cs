using OpenCvSharp;
using System;

namespace HelloWorld
{
    class Params
    {
        public static string Ffmpeg = @"C:\msys64\mingw64\bin\ffmpeg.exe";
        public static TimeSpan babyStep = TimeSpan.FromMilliseconds(250);
        public static TimeSpan giantStep = TimeSpan.FromMilliseconds(2000);
        public static Template GameStartTemplate = new Template(Cv2.ImRead("waku.png"), new Rect(688, 289, 541, 250));
        public static Template GameEndTemlate = new Template(Cv2.ImRead("p.png"), new Rect(237, 68, 50, 83));
    }
}
