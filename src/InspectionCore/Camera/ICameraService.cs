using System;
using System.Threading;
using System.Threading.Tasks;

namespace InspectionCore.Camera
{
    public interface ICameraService : IAsyncDisposable
    {
        bool IsRunning { get; }
        Task StartAsync(int cameraIndex = 0, CancellationToken ct = default);
        Task StopAsync();
        event EventHandler<byte[]?>? FrameCaptured; // raw bytes (e.g., BGRA32) for UI layer to convert
        int Width { get; }
        int Height { get; }
        int Stride { get; } // bytes per row, for the emitted pixel format
        string PixelFormat { get; } // e.g., "BGRA32"
    }
}
