using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeatWatch.Core.Models;
using HeatWatch.Core.Persistence;
using HeatWatch.Core.Startup;
using HeatWatch.Core.Theming;

namespace HeatWatch.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly MainViewModel _mainVm;
    private readonly JsonSettingsRepository _repo;

    public ObservableCollection<NodeDefinition> AllNodes { get; }
    public ICollectionView FilteredNodes { get; }

    public string AppVersion { get; } =
        $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(2) ?? "1.0"}";

    [ObservableProperty] private int _selectedTabIndex = 0;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _autoHideControls;
    [ObservableProperty] private bool _useHeatColors;

    public bool ShowDriverWarningTab => _mainVm.ShowCpuDriverWarning;
    public bool IsLearnMoreSelected  => _selectedTabIndex == 3;

    partial void OnSelectedTabIndexChanged(int value) =>
        OnPropertyChanged(nameof(IsLearnMoreSelected));
    [ObservableProperty] private AppTheme _selectedTheme;
    [ObservableProperty] private AppView _selectedView;
    [ObservableProperty] private bool _isRunAtStartup;
    [ObservableProperty] private bool _isResizable;
    [ObservableProperty] private bool _minimizeToTray;

    partial void OnSearchTextChanged(string value) => FilteredNodes.Refresh();

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        OnPropertyChanged(nameof(IsThemeDark));
        OnPropertyChanged(nameof(IsThemeLight));
        OnPropertyChanged(nameof(IsThemeSystem));
    }

    partial void OnSelectedViewChanged(AppView value)
    {
        OnPropertyChanged(nameof(IsViewStacked));
        OnPropertyChanged(nameof(IsViewGrid));
    }

    public bool IsThemeDark   { get => _selectedTheme == AppTheme.Dark;          set { if (value) SelectedTheme = AppTheme.Dark; } }
    public bool IsThemeLight  { get => _selectedTheme == AppTheme.Light;         set { if (value) SelectedTheme = AppTheme.Light; } }
    public bool IsThemeSystem { get => _selectedTheme == AppTheme.SystemDefault; set { if (value) SelectedTheme = AppTheme.SystemDefault; } }

    public bool IsViewStacked { get => _selectedView == AppView.Stacked; set { if (value) SelectedView = AppView.Stacked; } }
    public bool IsViewGrid    { get => _selectedView == AppView.Grid;    set { if (value) SelectedView = AppView.Grid; } }

    public SettingsViewModel(AppSettings settings, MainViewModel mainVm, JsonSettingsRepository repo)
    {
        _settings = settings;
        _mainVm = mainVm;
        _repo = repo;
        _selectedTheme     = settings.Theme;
        _selectedView      = settings.View;
        _isRunAtStartup    = StartupManager.IsEnabled();
        _isResizable       = settings.IsResizable;
        _autoHideControls  = settings.AutoHideControls;
        _useHeatColors     = settings.UseHeatColors;
        _minimizeToTray    = settings.MinimizeToTray;

        AllNodes = new ObservableCollection<NodeDefinition>(
            settings.Nodes.Select(n => new NodeDefinition
            {
                SensorId = n.SensorId,
                Label = n.Label,
                HardwareType = n.HardwareType,
                HardwareName = n.HardwareName,
                SensorType = n.SensorType,
                IsEnabled = n.IsEnabled,
            })
        );

        FilteredNodes = CollectionViewSource.GetDefaultView(AllNodes);
        FilteredNodes.Filter = obj =>
            obj is NodeDefinition node &&
            (string.IsNullOrWhiteSpace(SearchText) ||
             node.Label.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
             node.HardwareName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand]
    public void MoveUp(NodeDefinition node)
    {
        int index = AllNodes.IndexOf(node);
        if (index > 0)
            AllNodes.Move(index, index - 1);
    }

    [RelayCommand]
    public void MoveDown(NodeDefinition node)
    {
        int index = AllNodes.IndexOf(node);
        if (index >= 0 && index < AllNodes.Count - 1)
            AllNodes.Move(index, index + 1);
    }

    [RelayCommand]
    public void SelectAll()
    {
        foreach (var node in AllNodes)
            node.IsEnabled = true;
    }

    [RelayCommand]
    public void DeselectAll()
    {
        foreach (var node in AllNodes)
            node.IsEnabled = false;
    }

    [RelayCommand]
    public void Save(System.Windows.Window window)
    {
        _settings.Nodes            = [.. AllNodes];
        _settings.Theme            = SelectedTheme;
        _settings.View             = SelectedView;
        _settings.IsResizable      = IsResizable;
        _settings.AutoHideControls = AutoHideControls;
        _settings.UseHeatColors    = UseHeatColors;
        _settings.MinimizeToTray   = MinimizeToTray;
        _mainVm.RebuildNodes(_settings.Nodes);
        _mainVm.AppView            = SelectedView;
        _mainVm.IsResizable        = IsResizable;
        _mainVm.AutoHideControls   = AutoHideControls;
        _mainVm.UseHeatColors      = UseHeatColors;
        _mainVm.MinimizeToTray     = MinimizeToTray;
        ThemeManager.Apply(SelectedTheme);
        if (IsRunAtStartup) StartupManager.Enable();
        else                StartupManager.Disable();
        _repo.Save(_settings);
        window.Close();
    }

    [RelayCommand]
    public void SelectLearnMoreTab() => SelectedTabIndex = 3;

    [RelayCommand]
    public void Cancel(System.Windows.Window window) => window.Close();
}
