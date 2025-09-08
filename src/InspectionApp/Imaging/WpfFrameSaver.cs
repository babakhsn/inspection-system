using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using InspectionCore.Abstractions;

namespace InspectionApp.Imaging
{
    public sealed class WpfFrameSaver : IFrameSaver
    {
        private readonly IFileSystem _fs;
        public WpfFrameSaver(IFileSystem fs) => _fs = fs;

        public Task SaveAsync(BitmapSource bitmap, string path, string format = "png", int quality = 90)
        {
            return Task.Run(() =>
            {
                BitmapEncoder encoder = format.ToLowerInvariant() switch
                {
                    "jpg" or "jpeg" => new JpegBitmapEncoder { QualityLevel = quality },
                    "bmp" => new BmpBitmapEncoder(),
                    "tif" or "tiff" => new TiffBitmapEncoder(),
                    "png" => new PngBitmapEncoder(),
                    _ => new PngBitmapEncoder()
                };

                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                _fs.CreateDirectory(Path.GetDirectoryName(path)!);
                using var stream = _fs.OpenWrite(path);
                encoder.Save(stream);
            });
        }
    }
}
