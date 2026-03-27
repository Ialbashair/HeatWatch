using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using HeatWatch.ViewModels;

namespace HeatWatch.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _vm;
    private readonly DispatcherTimer _positionSaveTimer;

    public MainWindow()
    {
        InitializeComponent();

        _positionSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _positionSaveTimer.Tick += (_, _) =>
        {
            _positionSaveTimer.Stop();
            _vm?.PersistWindowPosition(Left, Top);
        };

        DataContextChanged += (_, e) =>
        {
            if (_vm is not null)
                _vm.PropertyChanged -= OnVmPropertyChanged;

            _vm = e.NewValue as MainViewModel;

            if (_vm is not null)
                _vm.PropertyChanged += OnVmPropertyChanged;
        };
        LocationChanged += OnLocationChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.ControlsVisible))
            return;

        // Let WPF remeasure and resize to fit the now-visible/collapsed title bar.
        SizeToContent = SizeToContent.Height;

        // In resizable mode, restore Manual after the layout pass completes.
        if (_vm!.IsResizable)
            Dispatcher.InvokeAsync(() => SizeToContent = SizeToContent.Manual,
                DispatcherPriority.Loaded);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        _positionSaveTimer.Stop();
        _positionSaveTimer.Start();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_vm is { MinimizeToTray: true } && Application.Current is App app)
        {
            e.Cancel = true;

            // Show first-time tray notification
            if (!_vm.HasSeenTrayMessage)
            {
                _vm.HasSeenTrayMessage = true;
                _vm.ShowTrayMessage = true;

                // Auto-dismiss after 5 seconds, then hide window
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    _vm.ShowTrayMessage = false;
                    app.HideMainWindow();
                };
                timer.Start();
            }
            else
            {
                app.HideMainWindow();
            }
            return;
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _positionSaveTimer.Stop();
        _vm?.PersistWindowPosition(Left, Top);
        _vm?.Dispose();
        base.OnClosed(e);

        if (Application.Current is App app)
            app.ExitApplication();
    }

    protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        _vm?.NotifyMouseEntered();
    }

    protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _vm?.NotifyMouseLeft();
    }

    private void Button_Click(object sender, RoutedEventArgs e) { }
}
