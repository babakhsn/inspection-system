using InspectionApp.Imaging;
using InspectionApp.ViewModels;
using InspectionCore.Abstractions;
using InspectionCore.Camera;
using InspectionCore.Processing;
using System.Windows;

namespace InspectionApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var vm = new MainViewModel(
                camera: new EmguCameraService(),
                clock: new SystemClock(),
                frameSaver: new WpfFrameSaver(new RealFileSystem()),
                fs: new RealFileSystem(),
                processing: new EmguProcessingService());

            DataContext = vm;
        }
    }
}
