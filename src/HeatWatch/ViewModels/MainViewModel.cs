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
    [ObservableProperty] private double _nodeOpacity;
    [ObservableProperty] private double _backgroundOpacity;
    [ObservableProperty] private bool _autoHideControls;
    [ObservableProperty] private bool _useHeatColors;
    [ObservableProperty] private bool _controlsVisible = true;

    private readonly DispatcherTimer _hideTimer;

    partial void OnIsTopmostChanged(bool value)
    {
        _settings.IsTopmost = value;
        SaveSettings();
    }

    partial void OnAppViewChanged(AppView value)
    {
        OnPropertyChanged(nameof(IsStackedView));
        OnPropertyChanged(nameof(IsGridView));
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
        _nodeOpacity      = settings.NodeOpacity;
        _backgroundOpacity = settings.BackgroundOpacity;
        _autoHideControls = settings.AutoHideControls;
        _useHeatColors    = settings.UseHeatColors;

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
    }

    private void OnSensorsUpdated(object? sender, IReadOnlyList<SensorReading> readings)
    {
        var lookup = readings.ToDictionary(r => r.Id);

        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            foreach (var node in Nodes)
            {
                if (lookup.TryGetValue(node.SensorId, out var reading))
                    node.UpdateValue(reading.Value);
            }
        }, DispatcherPriority.Background);
    }

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
