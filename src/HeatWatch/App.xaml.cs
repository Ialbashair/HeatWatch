using System.Drawing;
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
    private System.Windows.Forms.NotifyIcon? _trayIcon;

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

            // 7. Set up system tray icon
            SetupTrayIcon();

            // 8. Create and show window
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

            // 9. If tray mode is on, hide from taskbar and show tray icon immediately
            if (settings.MinimizeToTray)
            {
                window.ShowInTaskbar = false;
                if (_trayIcon is not null)
                    _trayIcon.Visible = true;
            }

            window.Show();

            // 10. Start polling after the window is visible
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

    private void SetupTrayIcon()
    {
        Icon icon;
        try
        {
            var iconStream = GetResourceStream(new Uri("pack://application:,,,/Assets/heatwatch-logo-16px.ico"))?.Stream;
            icon = iconStream is not null ? new Icon(iconStream) : SystemIcons.Application;
        }
        catch
        {
            icon = SystemIcons.Application;
        }

        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = icon,
            Text = "HeatWatch",
            Visible = false,
        };

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        var showItem = contextMenu.Items.Add("Show HeatWatch", null, (_, _) => ToggleMainWindow());
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, (_, _) => ExitApplication());
        _trayIcon.ContextMenuStrip = contextMenu;

        // Update the Show/Hide label each time the menu opens
        contextMenu.Opening += (_, _) =>
        {
            bool isVisible = MainWindow is MainWindow w && w.IsVisible;
            showItem.Text = isVisible ? "Hide HeatWatch" : "Show HeatWatch";
        };

        _trayIcon.DoubleClick += (_, _) => ToggleMainWindow();
    }

    public void ToggleMainWindow()
    {
        if (MainWindow is MainWindow window && window.IsVisible)
            HideMainWindow();
        else
            ShowMainWindow();
    }

    public void ShowMainWindow()
    {
        if (MainWindow is MainWindow window)
        {
            window.Show();
            window.WindowState = WindowState.Normal;
            window.Activate();
        }
    }

    public void HideMainWindow()
    {
        if (MainWindow is MainWindow window)
            window.Hide();
    }

    /// <summary>
    /// Called when the MinimizeToTray setting is toggled at runtime.
    /// </summary>
    public void ApplyTrayMode(bool enabled)
    {
        if (MainWindow is MainWindow window)
            window.ShowInTaskbar = !enabled;

        if (_trayIcon is not null)
            _trayIcon.Visible = enabled;
    }

    public void ExitApplication()
    {
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _poller?.Stop();
        _mainVm?.Dispose();
        _hardwareMonitor?.Dispose();
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        base.OnExit(e);
    }
}
