using System;
using InspectionApp.Common;

namespace InspectionApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private string _title = "Mini Inspection System";
        private bool _isCapturing;
        private string _statusMessage = "Ready";
        private DateTime _lastAction = DateTime.Now;

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

        public RelayCommand StartCaptureCommand { get; }
        public RelayCommand StopCaptureCommand { get; }

        public MainViewModel()
        {
            StartCaptureCommand = new RelayCommand(OnStartCapture, CanStart);
            StopCaptureCommand = new RelayCommand(OnStopCapture, CanStop);
        }

        private bool CanStart() => !IsCapturing;
        private bool CanStop() => IsCapturing;

        private void OnStartCapture()
        {
            // Dummy action for Day 2—no real camera yet
            IsCapturing = true;
            LastAction = DateTime.Now;
            StatusMessage = "Capture started (dummy).";
        }

        private void OnStopCapture()
        {
            // Dummy action for Day 2—no real camera yet
            IsCapturing = false;
            LastAction = DateTime.Now;
            StatusMessage = "Capture stopped (dummy).";
        }
    }
}
