using System;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Runtime.InteropServices;

namespace InspectionCore.Camera
{
    public class EmguCameraService : ICameraService
    {
        private VideoCapture? _capture;
        private Task? _loopTask;
        private CancellationTokenSource? _cts;

        public bool IsRunning { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Stride { get; private set; }
        public string PixelFormat { get; private set; } = "BGRA32";

        public event EventHandler<byte[]?>? FrameCaptured;

        public async Task StartAsync(int cameraIndex = 0, CancellationToken ct = default)
        {
            if (IsRunning) return;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            _capture = new VideoCapture(cameraIndex, VideoCapture.API.Any);
            if (!_capture.IsOpened)
                throw new InvalidOperationException($"Cannot open camera index {cameraIndex}.");

            // Initialize dimensions
            Width = (int)_capture.Get(CapProp.FrameWidth);
            Height = (int)_capture.Get(CapProp.FrameHeight);
            // We’ll emit BGRA32 to make WPF conversion easy (4 bytes per pixel)
            Stride = Width * 4;

            IsRunning = true;

            _loopTask = Task.Run(() => CaptureLoop(_cts.Token), _cts.Token);
            await Task.CompletedTask;
        }

        private void CaptureLoop(CancellationToken ct)
        {
            using var frame = new Mat();
            using var bgra = new Mat();

            while (!ct.IsCancellationRequested)
            {
                _capture!.Read(frame);
                if (frame.IsEmpty) continue;

                // Convert to BGRA so WPF can consume easily
                CvInvoke.CvtColor(frame, bgra, ColorConversion.Bgr2Bgra);

                // Copy to managed array
                int byteCount = bgra.Rows * bgra.Cols * bgra.ElementSize;
                var buffer = new byte[byteCount];
                //bgra.GetArray(out byte[]? tmp);
                //// GetArray allocates; when available, prefer Marshal.Copy from bgra.DataPointer for perf.
                //Buffer.BlockCopy(tmp, 0, buffer, 0, byteCount);
                Marshal.Copy(bgra.DataPointer, buffer, 0, byteCount);

                FrameCaptured?.Invoke(this, buffer);
            }
        }

        public async Task StopAsync()
        {
            if (!IsRunning) return;
            IsRunning = false;

            _cts?.Cancel();
            if (_loopTask is not null)
            {
                try { await _loopTask; } catch { /* ignore */ }
            }

            _loopTask = null;
            _cts?.Dispose(); _cts = null;

            _capture?.Dispose(); _capture = null;
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }
    }
}
