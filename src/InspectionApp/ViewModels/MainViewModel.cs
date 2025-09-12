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
using InspectionCore.Motion;

namespace InspectionApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ICameraService _camera;
        private readonly IClock _clock;
        private readonly IFrameSaver _frameSaver;
        private readonly IFileSystem _fs;
        private readonly IProcessingService _processing;
        private readonly IMotorService _motor;
        private readonly IViewportTransformer _viewport;
        private double _panXPercent;
        private double _panYPercent;
        private double _zoomFactor = 1.0;
        private CancellationTokenSource? _autoCaptureCts;
        private bool _autoCaptureEnabled = true;
        private int _autoCaptureDebounceMs = 350;
        private BitmapSource? _originalFrame;
        private BitmapSource? _processedFrame;
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

        public BitmapSource? OriginalFrame
        {
            get => _originalFrame;
            set => SetProperty(ref _originalFrame, value);
        }

        public BitmapSource? ProcessedFrame
        {
            get => _processedFrame;
            set
            {
                if (SetProperty(ref _processedFrame, value))
                    CaptureFrameCommand.RaiseCanExecuteChanged();
            }
        }

        public int AutoCaptureDebounceMs
        {
            get => _autoCaptureDebounceMs;
            set => SetProperty(ref _autoCaptureDebounceMs, Math.Clamp(value, 50, 2000));
        }

        public double PanXPercent
        {
            get => _panXPercent;
            set
            {
                if (SetProperty(ref _panXPercent, value))
                    _motor.SetPanPercent(_panXPercent, _panYPercent);
            }
        }

        public bool AutoCaptureEnabled
        {
            get => _autoCaptureEnabled;
            set => SetProperty(ref _autoCaptureEnabled, value);
        }

        public double PanYPercent
        {
            get => _panYPercent;
            set
            {
                if (SetProperty(ref _panYPercent, value))
                    _motor.SetPanPercent(_panXPercent, _panYPercent);
            }
        }

        public double ZoomFactor
        {
            get => _zoomFactor;
            set
            {
                if (SetProperty(ref _zoomFactor, Math.Max(1.0, value)))
                    _motor.SetZoom(_zoomFactor);
            }
        }


        public RelayCommand ResetMotorCommand { get; }
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
            IProcessingService processing,
            IMotorService motor,
            IViewportTransformer viewport
            )
        {
            _camera = camera;
            _clock = clock;
            _frameSaver = frameSaver;
            _fs = fs;
            _processing = processing;
            _motor = motor;
            _viewport = viewport;

            _camera.FrameCaptured += OnFrameCaptured;
            _motor.StateChanged += async (_, s) =>
            {
                SetProperty(ref _panXPercent, s.PanXPercent, nameof(PanXPercent));
                SetProperty(ref _panYPercent, s.PanYPercent, nameof(PanYPercent));
                SetProperty(ref _zoomFactor, s.ZoomFactor, nameof(ZoomFactor));

                if (!AutoCaptureEnabled) return;
                if (!IsCapturing) return;

                // Debounce: cancel any pending capture and schedule a new one
                _autoCaptureCts?.Cancel();
                _autoCaptureCts?.Dispose();
                _autoCaptureCts = new System.Threading.CancellationTokenSource();
                var ct = _autoCaptureCts.Token;

                try
                {
                    await Task.Delay(AutoCaptureDebounceMs, ct);
                    if (ct.IsCancellationRequested) return;

                    await AutoCaptureCurrentAsync(s);  // see below
                }
                catch (TaskCanceledException) { /* ignore */ }
                catch (Exception ex)
                {
                    Log.Error(ex, "Auto-capture debounce error");
                }
            };

            StartCaptureCommand = new RelayCommand(async () => await OnStartCaptureAsync(), CanStart);
            StopCaptureCommand = new RelayCommand(async () => await OnStopCaptureAsync(), CanStop);
            CaptureFrameCommand = new RelayCommand(async () => await OnCaptureAsync(), CanCapture);
            ResetMotorCommand = new RelayCommand(OnResetMotor);

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
                // 1) Show RAW immediately (original)
                var raw = BitmapSourceFactory.FromBgra32(
                    buffer, _camera.Width, _camera.Height, _camera.Stride);
                OriginalFrame = raw;

                // 2) Apply motor (pan/zoom) to BGRA
                var motorState = _motor.State;
                var transformedBytes = await _viewport.ApplyAsync(
                    buffer, _camera.Width, _camera.Height, _camera.Stride,
                    motorState.PanXPercent, motorState.PanYPercent, motorState.ZoomFactor);

                // 3) Process transformed frame with selected filter
                var processedBytes = await _processing.ApplyAsync(
                    transformedBytes, _camera.Width, _camera.Height, _camera.Stride,
                    SelectedFilter, CannyThreshold1, CannyThreshold2);

                var processed = BitmapSourceFactory.FromBgra32(
                    processedBytes, _camera.Width, _camera.Height, _camera.Stride);

                ProcessedFrame = processed;
                LiveFrame = processed; // keep legacy binding behavior
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in motor transform or processing");
            }
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
                if (ProcessedFrame is null)
                {
                    StatusMessage = "No processed frame to save.";
                    return;
                }

                var ts = _clock.Now;
                var fileName = $"capture_{ts:yyyyMMdd_HHmmss_fff}.png";
                var fullPath = Path.Combine(CaptureDirectory, fileName);

                await _frameSaver.SaveAsync(ProcessedFrame, fullPath, "png", 90);

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

        private void OnResetMotor()
        {
            _motor.Reset();
            PanXPercent = 0;
            PanYPercent = 0;
            ZoomFactor = 1.0;
        }

        private async Task AutoCaptureCurrentAsync(InspectionCore.Motion.MotorState s)
        {
            try
            {
                var frame = ProcessedFrame ?? LiveFrame;
                if (frame is null)
                {
                    StatusMessage = "Auto-capture skipped (no frame).";
                    return;
                }

                var ts = _clock.Now;
                var fn = $"auto_{ts:yyyyMMdd_HHmmss_fff}_px{Math.Round(s.PanXPercent)}_py{Math.Round(s.PanYPercent)}_z{Math.Round(s.ZoomFactor, 2)}.png";
                var full = Path.Combine(CaptureDirectory, fn);

                await _frameSaver.SaveAsync(frame, full, "png", 90);

                LastAction = ts;
                StatusMessage = $"Auto-saved: {fn}";
                Log.Information("Auto-captured to {Path}", full);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Auto-capture failed");
                StatusMessage = $"Auto-capture error: {ex.Message}";
            }
        }




    }
}
