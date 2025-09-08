using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace InspectionApp.Imaging
{
    public interface IFrameSaver
    {
        Task SaveAsync(BitmapSource bitmap, string path, string format = "png", int quality = 90);
    }
}
