using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeatWatch.Core.Models;
using HeatWatch.Core.Persistence;
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
    [ObservableProperty] private AppTheme _selectedTheme;

    partial void OnSearchTextChanged(string value) => FilteredNodes.Refresh();

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        OnPropertyChanged(nameof(IsThemeDark));
        OnPropertyChanged(nameof(IsThemeLight));
        OnPropertyChanged(nameof(IsThemeSystem));
    }

    public bool IsThemeDark   { get => _selectedTheme == AppTheme.Dark;          set { if (value) SelectedTheme = AppTheme.Dark; } }
    public bool IsThemeLight  { get => _selectedTheme == AppTheme.Light;         set { if (value) SelectedTheme = AppTheme.Light; } }
    public bool IsThemeSystem { get => _selectedTheme == AppTheme.SystemDefault; set { if (value) SelectedTheme = AppTheme.SystemDefault; } }

    public SettingsViewModel(AppSettings settings, MainViewModel mainVm, JsonSettingsRepository repo)
    {
        _settings = settings;
        _mainVm = mainVm;
        _repo = repo;
        _selectedTheme = settings.Theme;

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
        _settings.Nodes = [.. AllNodes];
        _settings.Theme = SelectedTheme;
        _mainVm.RebuildNodes(_settings.Nodes);
        ThemeManager.Apply(SelectedTheme);
        _repo.Save(_settings);
        window.Close();
    }

    [RelayCommand]
    public void Cancel(System.Windows.Window window) => window.Close();
}
