using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace InspectionCore.Processing
{
    public sealed class EmguViewportTransformer : IViewportTransformer
    {
        public Task<byte[]> ApplyAsync(
            byte[] bgra, int width, int height, int stride,
            double panXPercent, double panYPercent, double zoomFactor,
            CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                if (zoomFactor <= 1.0001 && Math.Abs(panXPercent) < 0.001 && Math.Abs(panYPercent) < 0.001)
                {
                    // No transform needed
                    return (byte[])bgra.Clone();
                }

                // Create Mat wrapper over BGRA32 buffer
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
                using (var srcBgr = new Mat())
                {
                    CvInvoke.CvtColor(srcBgra, srcBgr, ColorConversion.Bgra2Bgr);

                    // Compute crop window based on zoom
                    double cropW = width / zoomFactor;
                    double cropH = height / zoomFactor;

                    // Pan offsets: -100..100% shifts the crop center across the leftover margins
                    // Pan of 100% means move crop center to the extreme right/bottom bounds.
                    double maxOffsetX = (width - cropW) / 2.0;
                    double maxOffsetY = (height - cropH) / 2.0;

                    double centerX = width / 2.0 + (panXPercent / 100.0) * maxOffsetX;
                    double centerY = height / 2.0 + (panYPercent / 100.0) * maxOffsetY;

                    // Derive crop rect (clamped)
                    double x = Math.Clamp(centerX - cropW / 2.0, 0, width - cropW);
                    double y = Math.Clamp(centerY - cropH / 2.0, 0, height - cropH);

                    var roi = new Rectangle((int)Math.Round(x), (int)Math.Round(y),
                                            (int)Math.Round(cropW), (int)Math.Round(cropH));

                    using (var cropped = new Mat(srcBgr, roi))
                    using (var resized = new Mat())
                    using (var outBgra = new Mat())
                    {
                        CvInvoke.Resize(cropped, resized, new System.Drawing.Size(width, height), 0, 0, Inter.Linear);
                        CvInvoke.CvtColor(resized, outBgra, ColorConversion.Bgr2Bgra);

                        int byteCount = outBgra.Rows * outBgra.Cols * outBgra.ElementSize;
                        var buffer = new byte[byteCount];
                        Marshal.Copy(outBgra.DataPointer, buffer, 0, byteCount);
                        return buffer;
                    }
                }
            }, ct);
        }
    }
}
