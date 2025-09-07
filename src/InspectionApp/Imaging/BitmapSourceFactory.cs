using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace InspectionApp.Imaging
{
    public static class BitmapSourceFactory
    {
        public static BitmapSource FromBgra32(byte[] buffer, int width, int height, int stride)
        {
            var bs = BitmapSource.Create(
                width, height,                // pixel width/height
                96, 96,                       // dpi X/Y
                PixelFormats.Bgra32,          // must match service output
                null,                         // palette
                buffer,                       // pixel data
                stride                        // stride (bytes per row)
            );
            bs.Freeze(); // make it cross-thread friendly
            return bs;
        }
    }
}
