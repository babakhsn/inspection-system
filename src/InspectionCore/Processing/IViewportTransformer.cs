using System.Threading;
using System.Threading.Tasks;

namespace InspectionCore.Processing
{
    public interface IViewportTransformer
    {
        /// <summary>
        /// Applies digital pan/zoom to a BGRA32 frame and returns BGRA32.
        /// </summary>
        Task<byte[]> ApplyAsync(
            byte[] bgra, int width, int height, int stride,
            double panXPercent, double panYPercent, double zoomFactor,
            CancellationToken ct = default);
    }
}
