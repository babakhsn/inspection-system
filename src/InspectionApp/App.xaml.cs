using System;
using System.Windows;
using Serilog;
using Serilog.Events;

namespace InspectionApp
{
    public partial class App : Application
    {
        public App()
        {
            // Configure Serilog once for the whole app
            var logsDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InspectionSystem", "logs");

            System.IO.Directory.CreateDirectory(logsDir);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .WriteTo.Console()
                .WriteTo.File(
                    path: System.IO.Path.Combine(logsDir, "app-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    shared: true,
                    restrictedToMinimumLevel: LogEventLevel.Debug)
                .Enrich.FromLogContext()
                .CreateLogger();

            Log.Information("Application starting up");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application exiting");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
