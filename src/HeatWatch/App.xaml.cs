using System.Windows;
using HeatWatch.Core.Hardware;
using HeatWatch.Core.Persistence;
using HeatWatch.Core.Theming;
using HeatWatch.ViewModels;
using HeatWatch.Views;
using LibreHardwareMonitor.Hardware;

namespace HeatWatch;

public partial class App : Application
{
    private HardwareMonitor? _hardwareMonitor;
    private SensorPoller? _poller;
    private MainViewModel? _mainVm;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // 1. Open hardware access
            _hardwareMonitor = new HardwareMonitor();

            // 2. Discover all available sensors
            var discovered = _hardwareMonitor.GetAllReadings();

            // 3. Load + merge user settings
            var repo = new JsonSettingsRepository();
            var settings = repo.Load(discovered);

            // 4. Apply saved theme before any window is created
            ThemeManager.Apply(settings.Theme);

            // 5. Set up polling
            _poller = new SensorPoller(_hardwareMonitor, settings.PollingIntervalMs);

            // 6. Wire up ViewModel
            bool cpuTempDriverBlocked =
                discovered.Any(r => r.HardwareType.Contains("Cpu") && r.Type == SensorType.Load) &&
                !discovered.Any(r => r.HardwareType.Contains("Cpu") && r.Type == SensorType.Temperature);

            _mainVm = new MainViewModel(_poller, repo, settings);
            _mainVm.ShowCpuDriverWarning = cpuTempDriverBlocked;

            // 7. Create and show window
            var window = new MainWindow { DataContext = _mainVm };

            if (!double.IsNaN(settings.WindowLeft) && !double.IsNaN(settings.WindowTop))
            {
                window.Left = settings.WindowLeft;
                window.Top = settings.WindowTop;
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Left = SystemParameters.WorkArea.Right - window.Width - 20;
                window.Top = 20;
            }

            MainWindow = window;
            window.Show();

            // 8. Start polling after the window is visible
            _poller.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"HeatWatch failed to start:\n\n{ex.Message}\n\nMake sure you are running as Administrator.",
                "HeatWatch — Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _poller?.Stop();
        _mainVm?.Dispose();
        _hardwareMonitor?.Dispose();
        base.OnExit(e);
    }
}
