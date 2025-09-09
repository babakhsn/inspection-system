using System.Threading;
using System.Threading.Tasks;

namespace InspectionCore.Processing
{
    public interface IProcessingService
    {
        /// <summary>
        /// Process a BGRA32 image and return BGRA32 bytes with the filter applied.
        /// </summary>
        Task<byte[]> ApplyAsync(
            byte[] bgra, int width, int height, int stride,
            FilterType filter, int cannyThreshold1 = 100, int cannyThreshold2 = 200,
            CancellationToken ct = default);
    }
}
