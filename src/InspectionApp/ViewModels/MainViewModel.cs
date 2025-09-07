using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using InspectionApp.Common;
using InspectionApp.Imaging;
using InspectionCore.Camera;

namespace InspectionApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ICameraService _camera;
        private CancellationTokenSource? _linkedCts;

        private string _title = "Mini Inspection System";
        private bool _isCapturing;
        private string _statusMessage = "Ready";
        private DateTime _lastAction = DateTime.Now;
        private BitmapSource? _liveFrame;

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

        public RelayCommand StartCaptureCommand { get; }
        public RelayCommand StopCaptureCommand { get; }

        public MainViewModel()
        {
            _camera = new EmguCameraService();
            _camera.FrameCaptured += OnFrameCaptured;

            StartCaptureCommand = new RelayCommand(async () => await OnStartCaptureAsync(), CanStart);
            StopCaptureCommand = new RelayCommand(async () => await OnStopCaptureAsync(), CanStop);
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
                LastAction = DateTime.Now;
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
                LastAction = DateTime.Now;
                StatusMessage = "Live capture stopped.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void OnFrameCaptured(object? sender, byte[]? buffer)
        {
            if (buffer is null || !_camera.IsRunning) return;

            // Create a frozen BitmapSource on the background thread; safe for UI binding
            var bs = BitmapSourceFactory.FromBgra32(
                buffer, _camera.Width, _camera.Height, _camera.Stride
            );

            LiveFrame = bs; // triggers PropertyChanged → UI updates
        }
    }
}
