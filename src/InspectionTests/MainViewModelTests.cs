using FluentAssertions;
using InspectionApp.Imaging;
using InspectionApp.ViewModels;
using InspectionCore.Abstractions;
using InspectionCore.Camera;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Xunit;

namespace InspectionTests
{
    public class MainViewModelTests
    {
        [WpfFact]
        public async Task StartStop_ShouldToggleIsCapturing_AndUpdateStatus()
        {
            var camera = Substitute.For<ICameraService>();
            camera.IsRunning.Returns(ci => true);

            var clock = Substitute.For<IClock>();
            clock.Now.Returns(new DateTime(2025, 1, 1, 12, 0, 0));

            var fs = Substitute.For<IFileSystem>();
            var saver = Substitute.For<IFrameSaver>();

            var vm = new MainViewModel(camera, clock, saver, fs);

            vm.StartCaptureCommand.CanExecute(null).Should().BeTrue();

            await vm.StartCaptureCommand.ExecuteAsync();
            await camera.Received(1).StartAsync(0, Arg.Any<CancellationToken>());
            vm.IsCapturing.Should().BeTrue();
            vm.StatusMessage.Should().Contain("started");

            await vm.StopCaptureCommand.ExecuteAsync();
            await camera.Received(1).StopAsync();
            vm.IsCapturing.Should().BeFalse();
            vm.StatusMessage.Should().Contain("stopped");
        }

        [WpfFact]
        public async Task Capture_ShouldSave_WhenLiveFrameExists()
        {
            var camera = Substitute.For<ICameraService>();
            var clock = Substitute.For<IClock>();
            clock.Now.Returns(new DateTime(2025, 1, 1, 12, 0, 0, 123));

            var fs = Substitute.For<IFileSystem>();
            var saver = Substitute.For<IFrameSaver>();

            var vm = new MainViewModel(camera, clock, saver, fs);

            // Create a tiny 2x2 bitmap
            var wb = new WriteableBitmap(2, 2, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
            vm.LiveFrame = wb;

            vm.CaptureFrameCommand.CanExecute(null).Should().BeTrue();

            await vm.CaptureFrameCommand.ExecuteAsync();

            await saver.Received(1).SaveAsync(
                Arg.Any<BitmapSource>(),
                Arg.Is<string>(p => p.EndsWith("20250101_120000_123.png")),
                "png", 90);
        }
    }

    internal static class CommandExtensions
    {
        public static Task ExecuteAsync(this InspectionApp.Common.RelayCommand cmd)
        {
            var tcs = new TaskCompletionSource();
            try
            {
                cmd.Execute(null);
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            return tcs.Task;
        }
    }
}
