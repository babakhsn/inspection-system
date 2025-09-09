using Emgu.CV;
using Emgu.CV.CvEnum;
using InspectionCore.Processing;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static Emgu.CV.OCR.Tesseract;

namespace InspectionCore.Processing
{
    public sealed class EmguProcessingService : IProcessingService
    {
        public Task<byte[]> ApplyAsync(
            byte[] bgra, int width, int height, int stride,
            FilterType filter, int cannyThreshold1 = 100, int cannyThreshold2 = 200,
            CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                // Wrap the incoming BGRA buffer with a Mat without copying
                GCHandle handle = default;
                Mat srcBgra;
                try
                {
                    handle = GCHandle.Alloc(bgra, GCHandleType.Pinned);
                    srcBgra = new Mat(height, width, DepthType.Cv8U, 4, handle.AddrOfPinnedObject(), stride);
                }
                catch
                {
                    if (handle.IsAllocated) handle.Free();
                    throw;
                }

                using (srcBgra)
                using (var work = new Mat())
                {
                    switch (filter)
                    {
                        case FilterType.None:
                            return (byte[])bgra.Clone();

                        case FilterType.Grayscale:
                            CvInvoke.CvtColor(srcBgra, work, ColorConversion.Bgra2Gray);
                            using (var outBgra = new Mat())
                            {
                                CvInvoke.CvtColor(work, outBgra, ColorConversion.Gray2Bgra);
                                return MatToManagedBytes(outBgra);
                            }

                        case FilterType.EdgeDetection:
                            CvInvoke.CvtColor(srcBgra, work, ColorConversion.Bgra2Gray);
                            using (var edges = new Mat())
                            using (var outBgra2 = new Mat())
                            {
                                CvInvoke.Canny(work, edges, cannyThreshold1, cannyThreshold2);
                                CvInvoke.CvtColor(edges, outBgra2, ColorConversion.Gray2Bgra);
                                return MatToManagedBytes(outBgra2);
                            }

                        default:
                            return (byte[])bgra.Clone();
                    }
                }
            }, ct);
        }

        private static byte[] MatToManagedBytes(Mat m)
        {
            int byteCount = m.Rows * m.Cols * m.ElementSize;
            var buffer = new byte[byteCount];
            Marshal.Copy(m.DataPointer, buffer, 0, byteCount);
            return buffer;
        }
    }
}
