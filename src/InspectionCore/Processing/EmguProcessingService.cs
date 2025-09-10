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

                        case FilterType.DefectDetection:
                            // 1) BGRA -> BGR + GRAY
                            using (var srcBgr = new Mat())
                            {
                                CvInvoke.CvtColor(srcBgra, srcBgr, ColorConversion.Bgra2Bgr);
                                using (var gray = new Mat())
                                {
                                    CvInvoke.CvtColor(srcBgr, gray, ColorConversion.Bgr2Gray);

                                    // 2) Blur
                                    CvInvoke.GaussianBlur(gray, gray, new System.Drawing.Size(3, 3), 0);

                                    // 3) Sobel magnitude (edges/scratches)
                                    using (var gradX16 = new Mat())
                                    using (var gradY16 = new Mat())
                                    using (var gradX = new Mat())
                                    using (var gradY = new Mat())
                                    using (var mag = new Mat())
                                    {
                                        CvInvoke.Sobel(gray, gradX16, DepthType.Cv16S, 1, 0, 3);
                                        CvInvoke.Sobel(gray, gradY16, DepthType.Cv16S, 0, 1, 3);
                                        CvInvoke.ConvertScaleAbs(gradX16, gradX, 1.0, 0.0);
                                        CvInvoke.ConvertScaleAbs(gradY16, gradY, 1.0, 0.0);
                                        CvInvoke.AddWeighted(gradX, 0.5, gradY, 0.5, 0, mag);

                                        // 4) Threshold (Otsu)
                                        using (var mask = new Mat())
                                        {
                                            CvInvoke.Threshold(mag, mask, 0, 255, ThresholdType.Otsu | ThresholdType.Binary);

                                            // 5) Dilate a bit to make scratches visible
                                            // Fix for CS0103: The name 'ElementShape' does not exist in the current context
                                            // The correct enum for structuring element shape in Emgu.CV is 'MorphShapes'.
                                            // Fix for IDE0063: 'using' statement can be simplified
                                            // Use the simplified using declaration.

                                            using var kernel = CvInvoke.GetStructuringElement(MorphShapes.Rectangle, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
                                            {
                                                CvInvoke.Dilate(mask, mask, kernel, new System.Drawing.Point(-1, -1), 1, BorderType.Default, default);

                                                // 6) Draw contours in red over original BGR
                                                using (var contours = new Emgu.CV.Util.VectorOfVectorOfPoint())
                                                {
                                                    CvInvoke.FindContours(mask, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                                                    using (var overlay = srcBgr.Clone())
                                                    {
                                                        for (int i = 0; i < contours.Size; i++)
                                                        {
                                                            CvInvoke.DrawContours(overlay, contours, i, new Emgu.CV.Structure.MCvScalar(0, 0, 255), 2);
                                                        }

                                                        // 7) BGR -> BGRA and return
                                                        using (var outBgra3 = new Mat())
                                                        {
                                                            CvInvoke.CvtColor(overlay, outBgra3, ColorConversion.Bgr2Bgra);
                                                            return MatToManagedBytes(outBgra3);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
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
