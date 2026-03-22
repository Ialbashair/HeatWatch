using CommunityToolkit.Mvvm.ComponentModel;
using LibreHardwareMonitor.Hardware;

namespace HeatWatch.Core.Models;

// Extends ObservableObject so Label and IsEnabled changes propagate to the
// Settings window checkboxes / text boxes without rebuilding the collection.
public sealed partial class NodeDefinition : ObservableObject
{
    public string SensorId { get; set; } = string.Empty;
    public string HardwareType { get; set; } = string.Empty;
    public string HardwareName { get; set; } = string.Empty;
    public SensorType SensorType { get; set; }

    [ObservableProperty] private string _label = string.Empty;
    [ObservableProperty] private bool _isEnabled;
}
