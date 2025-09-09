using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using InspectionApp.Common;
using InspectionApp.Imaging;
using InspectionCore.Camera;
using System.IO;
using Serilog;
using InspectionCore.Abstractions;
using InspectionCore.Processing;

namespace InspectionApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ICameraService _camera;
        private readonly IClock _clock;
        private readonly IFrameSaver _frameSaver;
        private readonly IFileSystem _fs;
        private readonly IProcessingService _processing;
        private CancellationTokenSource? _linkedCts;

        private string _title = "Mini Inspection System";
        private bool _isCapturing;
        private string _statusMessage = "Ready";
        private DateTime _lastAction = DateTime.Now;
        private BitmapSource? _liveFrame;
        private string _captureDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "InspectionSystem", "Captures");

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsCapturing
        {
            get => _isCapturing;
            set
            {
                if (SetProperty(ref _isCapturing, value))
                {
                    StartCaptureCommand.RaiseCanExecuteChanged();
                    StopCaptureCommand.RaiseCanExecuteChanged();
                    CaptureFrameCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public DateTime LastAction
        {
            get => _lastAction;
            set => SetProperty(ref _lastAction, value);
        }

        public BitmapSource? LiveFrame
        {
            get => _liveFrame;
            set => SetProperty(ref _liveFrame, value);
        }

        // Filter UI state
        private FilterType _selectedFilter = FilterType.None;
        public FilterType SelectedFilter
        {
            get => _selectedFilter;
            set => SetProperty(ref _selectedFilter, value);
        }

        // Edge detection parameters
        private int _cannyThreshold1 = 100;
        private int _cannyThreshold2 = 200;

        public int CannyThreshold1
        {
            get => _cannyThreshold1;
            set => SetProperty(ref _cannyThreshold1, value);
        }

        public int CannyThreshold2
        {
            get => _cannyThreshold2;
            set => SetProperty(ref _cannyThreshold2, value);
        }

        public RelayCommand StartCaptureCommand { get; }
        public RelayCommand StopCaptureCommand { get; }

        public string CaptureDirectory
        {
            get => _captureDirectory;
            set => SetProperty(ref _captureDirectory, value);
        }

        public RelayCommand CaptureFrameCommand { get; }

        public MainViewModel(
            ICameraService camera, 
            IClock clock, 
            IFrameSaver frameSaver, 
            IFileSystem fs, 
            IProcessingService processing
            )
        {
            _camera = camera;
            _clock = clock;
            _frameSaver = frameSaver;
            _fs = fs;
            _processing = processing;

            _camera.FrameCaptured += OnFrameCaptured;

            StartCaptureCommand = new RelayCommand(async () => await OnStartCaptureAsync(), CanStart);
            StopCaptureCommand = new RelayCommand(async () => await OnStopCaptureAsync(), CanStop);
            CaptureFrameCommand = new RelayCommand(async () => await OnCaptureAsync(), CanCapture);

            try
            {
                _fs.CreateDirectory(CaptureDirectory);
                Log.Information("Capture directory set to {Dir}", CaptureDirectory);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create capture directory {Dir}", CaptureDirectory);
            }
        }

        private bool CanStart() => !IsCapturing;
        private bool CanStop() => IsCapturing;

        private async Task OnStartCaptureAsync()
        {
            try
            {
                _linkedCts = new CancellationTokenSource();
                await _camera.StartAsync(0, _linkedCts.Token);
                IsCapturing = true;
                LastAction = _clock.Now;
                StatusMessage = "Live capture started.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task OnStopCaptureAsync()
        {
            try
            {
                _linkedCts?.Cancel();
                await _camera.StopAsync();
                IsCapturing = false;
                LastAction = _clock.Now;
                StatusMessage = "Live capture stopped.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async void OnFrameCaptured(object? sender, byte[]? buffer)
        {
            if (buffer is null || !_camera.IsRunning) return;

            try
            {
                // Apply selected filter (BGRA32 -> BGRA32)
                var processed = await _processing.ApplyAsync(
                    buffer, _camera.Width, _camera.Height, _camera.Stride,
                    SelectedFilter, CannyThreshold1, CannyThreshold2);

                var bs = BitmapSourceFactory.FromBgra32(
                    processed, _camera.Width, _camera.Height, _camera.Stride);

                LiveFrame = bs;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error applying filter");
            }

            //// Create a frozen BitmapSource on the background thread; safe for UI binding
            //var bs = BitmapSourceFactory.FromBgra32(
            //    buffer, _camera.Width, _camera.Height, _camera.Stride
            //);

            //LiveFrame = bs; // triggers PropertyChanged → UI updates
        }

        private bool CanCapture() => LiveFrame is not null;

        private static Task SaveBitmapSourceAsync(BitmapSource bitmap, string path, string format = "png", int quality = 90)
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
                using var fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                encoder.Save(fs);
            });
        }

        private async Task OnCaptureAsync()
        {
            try
            {
                if (LiveFrame is null)
                {
                    StatusMessage = "No frame available to capture.";
                    return;
                }

                Directory.CreateDirectory(CaptureDirectory);

                var ts = _clock.Now;
                var fileName = $"capture_{ts:yyyyMMdd_HHmmss_fff}.png";
                var fullPath = Path.Combine(CaptureDirectory, fileName);

                await _frameSaver.SaveAsync(LiveFrame, fullPath, format: "png", quality: 90);

                LastAction = ts;
                StatusMessage = $"Saved: {fileName}";
                Log.Information("Frame saved to {Path}", fullPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save frame");
                StatusMessage = $"Error saving frame: {ex.Message}";
            }
        }


    }
}
