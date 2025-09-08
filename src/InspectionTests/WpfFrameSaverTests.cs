using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using InspectionApp.Imaging;
using InspectionCore.Abstractions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace InspectionTests
{
    public class WpfFrameSaverTests
    {
        [WpfFact] // If your runner supports WpfFact; otherwise use [Fact] and ensure STA.
        public async Task SaveAsync_WritesPngToDisk()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "InspectionSystemTests", Guid.NewGuid().ToString("N"));
            var fs = new RealFileSystem();
            var saver = new WpfFrameSaver(fs);

            var bmp = new WriteableBitmap(2, 2, 96, 96, PixelFormats.Bgra32, null);
            bmp.Lock();
            try
            {
                // simple fill
                unsafe
                {
                    var p = (byte*)bmp.BackBuffer.ToPointer();
                    for (int i = 0; i < 2 * 2 * 4; i++) p[i] = 200;
                }
                bmp.AddDirtyRect(new System.Windows.Int32Rect(0, 0, 2, 2));
            }
            finally
            {
                bmp.Unlock();
            }
            bmp.Freeze();

            Directory.CreateDirectory(tempDir);
            var path = Path.Combine(tempDir, "test.png");

            await saver.SaveAsync(bmp, path, "png", 90);

            fs.FileExists(path).Should().BeTrue();
            new FileInfo(path).Length.Should().BeGreaterThan(0);
        }
    }
}
