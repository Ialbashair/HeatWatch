using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using HeatWatch.Core.Models;
using LibreHardwareMonitor.Hardware;

namespace HeatWatch.ViewModels;

public sealed partial class NodeViewModel : ObservableObject
{
    public string SensorId { get; }

    [ObservableProperty] private string _label;
    [ObservableProperty] private string _value = "--";
    [ObservableProperty] private string _unit;
    [ObservableProperty] private Brush _heatColor = new SolidColorBrush(Color.FromRgb(64, 64, 64));
    [ObservableProperty] private string _icon;

    private readonly SensorType _sensorType;

    public NodeViewModel(NodeDefinition definition)
    {
        SensorId = definition.SensorId;
        _label = definition.Label;
        _unit = GetUnit(definition.SensorType);
        _sensorType = definition.SensorType;
        _icon = GetIcon(definition.SensorType, definition.HardwareType);
    }

    public void UpdateValue(float? value)
    {
        if (value is null)
            return; // keep showing "--" until a real reading arrives

        float v = value.Value;

        Value = _sensorType switch
        {
            SensorType.Temperature => v.ToString("F1"),
            SensorType.Clock       => v >= 1000 ? (v / 1000f).ToString("F2") + " GHz" : v.ToString("F0"),
            SensorType.Load        => v.ToString("F1"),
            SensorType.Data        => v.ToString("F2"),
            SensorType.SmallData   => v.ToString("F0"),
            SensorType.Power       => v.ToString("F1"),
            SensorType.Fan         => v.ToString("F0"),
            _                      => v.ToString("F1"),
        };

        // Override unit display for clocks that were converted to GHz
        if (_sensorType == SensorType.Clock && v >= 1000)
            Unit = string.Empty; // unit already embedded in Value string
        else
            Unit = GetUnit(_sensorType);

        HeatColor = _sensorType == SensorType.Temperature
            ? GetTemperatureColor(v)
            : _neutralAccent;
    }

    // Solid colors — used as the left accent strip on the node card
    private static readonly Brush _neutralAccent = new SolidColorBrush(Color.FromRgb(64, 64, 64));

    private static Brush GetTemperatureColor(float temp) => temp switch
    {
        < 50 => new SolidColorBrush(Color.FromRgb(61, 196, 90)),
        < 70 => new SolidColorBrush(Color.FromRgb(212, 160, 23)),
        < 85 => new SolidColorBrush(Color.FromRgb(212, 103, 23)),
        _    => new SolidColorBrush(Color.FromRgb(212, 48, 23)),
    };

    private static string GetUnit(SensorType type) => type switch
    {
        SensorType.Temperature => "°C",
        SensorType.Clock       => "MHz",
        SensorType.Load        => "%",
        SensorType.Data        => "GB",
        SensorType.SmallData   => "MB",
        SensorType.Power       => "W",
        SensorType.Fan         => "RPM",
        _                      => string.Empty,
    };

    private static string GetIcon(SensorType sensorType, string hardwareType) => sensorType switch
    {
        SensorType.Temperature => hardwareType.Contains("Gpu") ? "\uE7F4" : "\uE9CA",
        SensorType.Clock       => "\uEC92",
        SensorType.Load        => "\uE9D2",
        SensorType.Data        => "\uEDA2",
        SensorType.SmallData   => "\uEDA2",
        SensorType.Power       => "\uE945",
        SensorType.Fan         => "\uEA3A",
        _                      => "\uE9CE",
    };
}
