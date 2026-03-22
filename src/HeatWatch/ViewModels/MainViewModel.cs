using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeatWatch.Core.Hardware;
using HeatWatch.Core.Models;
using HeatWatch.Core.Persistence;

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

    partial void OnIsTopmostChanged(bool value)
    {
        _settings.IsTopmost = value;
        SaveSettings();
    }

    public MainViewModel(SensorPoller poller, JsonSettingsRepository repo, AppSettings settings)
    {
        _poller = poller;
        _repo = repo;
        _settings = settings;

        _isTopmost = settings.IsTopmost;
        _showSettingsHint = !settings.HasSeenSettings;

        RebuildNodes(settings.Nodes);

        _poller.SensorsUpdated += OnSensorsUpdated;
    }

    public void RebuildNodes(IEnumerable<NodeDefinition> definitions)
    {
        Nodes.Clear();
        foreach (var def in definitions.Where(d => d.IsEnabled))
            Nodes.Add(new NodeViewModel(def));
    }

    private void OnSensorsUpdated(object? sender, IReadOnlyList<SensorReading> readings)
    {
        var lookup = readings.ToDictionary(r => r.Id);

        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            foreach (var node in Nodes)
            {
                if (lookup.TryGetValue(node.SensorId, out var reading))
                    node.UpdateValue(reading.Value); // Value is float? — null means no reading yet
            }
        }, DispatcherPriority.Background);
    }

    [RelayCommand]
    public void OpenSettings()
    {
        if (!_settings.HasSeenSettings)
        {
            _settings.HasSeenSettings = true;
            ShowSettingsHint = false;
            _repo.Save(_settings);
        }

        var vm = new SettingsViewModel(_settings, this, _repo);
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
        _poller.SensorsUpdated -= OnSensorsUpdated;
        _repo.Save(_settings);
    }
}
