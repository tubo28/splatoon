using System;
using System.Diagnostics;

namespace HelloWorld
{
    class FfMpegUtil
    {
        public static void Cut(TimeSpan start, TimeSpan end, String filename)
        {
            var s = start.ToString(@"hh\:mm\:ss\.fff");
            var t = end.ToString(@"hh\:mm\:ss\.fff");
            var args = $"-ss {s} -i video.mp4 -to ${t} -c copy -copyts {filename}";
            Process.Start(Params.Ffmpeg, args);
        }
    }
}
