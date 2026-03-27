using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeatWatch.Core.Hardware;
using HeatWatch.Core.Models;
using HeatWatch.Core.Persistence;
using SizeToContent = System.Windows.SizeToContent;

namespace HeatWatch.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly SensorPoller _poller;
    private readonly JsonSettingsRepository _repo;
    private readonly AppSettings _settings;

    public ObservableCollection<NodeViewModel> Nodes { get; } = [];

    [ObservableProperty] private bool _isTopmost;
    [ObservableProperty] private bool _showSettingsHint;
    [ObservableProperty] private bool _showCpuDriverWarning;
    [ObservableProperty] private AppView _appView;
    [ObservableProperty] private bool _isResizable;
    [ObservableProperty] private bool _autoHideControls;
    [ObservableProperty] private bool _useHeatColors;
    [ObservableProperty] private bool _minimizeToTray;
    [ObservableProperty] private bool _showTrayMessage;
    [ObservableProperty] private bool _controlsVisible = true;

    private readonly DispatcherTimer _hideTimer;

    partial void OnIsTopmostChanged(bool value)
    {
        _settings.IsTopmost = value;
        SaveSettings();
    }

    partial void OnMinimizeToTrayChanged(bool value)
    {
        if (Application.Current is App app)
            app.ApplyTrayMode(value);
    }

    partial void OnAppViewChanged(AppView value)
    {
        OnPropertyChanged(nameof(IsStackedView));
        OnPropertyChanged(nameof(IsGridView));
        OnPropertyChanged(nameof(WindowMinHeight));
        OnPropertyChanged(nameof(WindowMinWidth));
    }

    partial void OnIsResizableChanged(bool value)
    {
        OnPropertyChanged(nameof(ResizeMode));
        OnPropertyChanged(nameof(SizeToContent));
    }

    partial void OnAutoHideControlsChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowTitleBar));
        if (!value) { _hideTimer.Stop(); ControlsVisible = true; }
    }

    partial void OnControlsVisibleChanged(bool value) =>
        OnPropertyChanged(nameof(ShowTitleBar));

    partial void OnUseHeatColorsChanged(bool value)
    {
        foreach (var node in Nodes)
            node.UseHeatColors = value;
    }

    public bool IsStackedView => _appView == AppView.Stacked;
    public bool IsGridView    => _appView == AppView.Grid;
    public bool ShowTitleBar  => !_autoHideControls || _controlsVisible;

    // Dynamic min size based on node count and view mode.
    // Height includes title bar; focus mode adjusts actual Height in code-behind.
    private const double TitleBarHeight = 37;
    private const double ContentPadding = 15;
    private const double StackedNodeHeight = 38;
    private const double GridRowHeight = 74;
    private const int GridColumns = 3;
    private const double MinEmptySize = 200;
    private const double GridCardMinWidth = 90;
    private const double ContentHorizontalPadding = 16;
    private const double StackedContentMinWidth = 200;

    public double WindowMinHeight
    {
        get
        {
            int count = Nodes.Count;
            if (count == 0)
                return MinEmptySize;

            double chrome = TitleBarHeight + ContentPadding;

            double content = _appView == AppView.Grid
                ? Math.Ceiling((double)count / GridColumns) * GridRowHeight
                : count * StackedNodeHeight;

            return chrome + content;
        }
    }

    public double WindowMinWidth
    {
        get
        {
            int count = Nodes.Count;
            if (count == 0)
                return MinEmptySize;

            double contentWidth = _appView == AppView.Grid
                ? Math.Min(count, GridColumns) * GridCardMinWidth + ContentHorizontalPadding
                : StackedContentMinWidth;

            return Math.Max(contentWidth, MinEmptySize);
        }
    }

    public ResizeMode ResizeMode =>
        _isResizable ? ResizeMode.CanResizeWithGrip : ResizeMode.NoResize;

    public SizeToContent SizeToContent =>
        _isResizable ? SizeToContent.Manual : SizeToContent.Height;

    public void NotifyMouseEntered()
    {
        _hideTimer.Stop();
        ControlsVisible = true;
    }

    public void NotifyMouseLeft()
    {
        if (_autoHideControls)
            _hideTimer.Start();
    }

    public MainViewModel(SensorPoller poller, JsonSettingsRepository repo, AppSettings settings)
    {
        _poller = poller;
        _repo = repo;
        _settings = settings;

        _isTopmost        = settings.IsTopmost;
        _showSettingsHint = !settings.HasSeenSettings;
        _appView          = settings.View;
        _isResizable      = settings.IsResizable;
        _autoHideControls = settings.AutoHideControls;
        _useHeatColors    = settings.UseHeatColors;
        _minimizeToTray   = settings.MinimizeToTray;

        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _hideTimer.Tick += (_, _) => { _hideTimer.Stop(); ControlsVisible = false; };

        RebuildNodes(settings.Nodes);

        _poller.SensorsUpdated += OnSensorsUpdated;
    }

    public void RebuildNodes(IEnumerable<NodeDefinition> definitions)
    {
        Nodes.Clear();
        foreach (var def in definitions.Where(d => d.IsEnabled))
        {
            var node = new NodeViewModel(def) { UseHeatColors = _useHeatColors };
            Nodes.Add(node);
        }
        OnPropertyChanged(nameof(WindowMinHeight));
        OnPropertyChanged(nameof(WindowMinWidth));
    }

    private Dictionary<string, SensorReading>? _readingsLookup;

    private void OnSensorsUpdated(object? sender, IReadOnlyList<SensorReading> readings)
    {
        _readingsLookup ??= new Dictionary<string, SensorReading>(readings.Count);
        var lookup = _readingsLookup;
        lookup.Clear();
        foreach (var r in readings)
            lookup[r.Id] = r;

        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            foreach (var node in Nodes)
            {
                if (lookup.TryGetValue(node.SensorId, out var reading))
                    node.UpdateValue(reading.Value);
            }
        }, DispatcherPriority.Background);
    }

    public bool HasSeenTrayMessage
    {
        get => _settings.HasSeenTrayMessage;
        set
        {
            _settings.HasSeenTrayMessage = value;
            SaveSettings();
        }
    }

    [RelayCommand]
    private void DismissTrayMessage() => ShowTrayMessage = false;

    [RelayCommand]
    public void OpenSettings() => OpenSettingsAtTab(0);

    [RelayCommand]
    public void OpenBehaviourSettings() => OpenSettingsAtTab(2);

    [RelayCommand]
    public void OpenDriverWarning() => OpenSettingsAtTab(3);

    private void OpenSettingsAtTab(int tab)
    {
        if (!_settings.HasSeenSettings)
        {
            _settings.HasSeenSettings = true;
            ShowSettingsHint = false;
            _repo.Save(_settings);
        }

        var vm = new SettingsViewModel(_settings, this, _repo);
        vm.SelectedTabIndex = tab;
        var window = new Views.SettingsWindow { DataContext = vm };
        window.ShowDialog();
    }

    public void SaveSettings() => _repo.Save(_settings);

    public void PersistWindowPosition(double left, double top)
    {
        _settings.WindowLeft = left;
        _settings.WindowTop = top;
        _repo.Save(_settings);
    }

    public void Dispose()
    {
        _hideTimer.Stop();
        _poller.SensorsUpdated -= OnSensorsUpdated;
        _repo.Save(_settings);
    }
}
