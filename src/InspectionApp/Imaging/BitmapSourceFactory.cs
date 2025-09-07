using Emgu.CV.Dnn;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace InspectionApp.Imaging

//    Take raw image data from a camera, OpenCV, or any other source.

//Wrap it into a WPF BitmapSource.

//Safely hand it off to your UI layer (Image.Source binding).
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
