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
    [ObservableProperty] private Brush _heatColor = _neutralAccent;
    [ObservableProperty] private string _icon;

    private readonly SensorType _sensorType;
    private float? _lastValue;
    private bool _useHeatColors = true;

    public bool UseHeatColors
    {
        get => _useHeatColors;
        set
        {
            if (_useHeatColors == value) return;
            _useHeatColors = value;
            if (_lastValue.HasValue)
                HeatColor = (_sensorType == SensorType.Temperature && _useHeatColors)
                    ? GetTemperatureColor(_lastValue.Value)
                    : _neutralAccent;
        }
    }

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
        _lastValue = v;

        var formatted = _sensorType switch
        {
            SensorType.Temperature => v.ToString("F1"),
            SensorType.Clock       => v >= 1000 ? $"{v / 1000f:F2} GHz" : v.ToString("F0"),
            SensorType.Load        => v.ToString("F1"),
            SensorType.Data        => v.ToString("F2"),
            SensorType.SmallData   => v.ToString("F0"),
            SensorType.Power       => v.ToString("F1"),
            SensorType.Fan         => v.ToString("F0"),
            _                      => v.ToString("F1"),
        };

        // Only raise property changes when the display text actually differs
        if (Value != formatted)
            Value = formatted;

        // Override unit display for clocks that were converted to GHz
        var unit = (_sensorType == SensorType.Clock && v >= 1000) ? string.Empty : GetUnit(_sensorType);
        if (Unit != unit)
            Unit = unit;

        var brush = (_sensorType == SensorType.Temperature && _useHeatColors)
            ? GetTemperatureColor(v)
            : _neutralAccent;
        if (HeatColor != brush)
            HeatColor = brush;
    }

    // Solid colors — used as the left accent strip on the node card.
    // All brushes are frozen so they're thread-safe and skip WPF change-tracking.
    private static readonly Brush _neutralAccent = Freeze(new SolidColorBrush(Color.FromRgb(64, 64, 64)));
    private static readonly Brush _coolBrush     = Freeze(new SolidColorBrush(Color.FromRgb(61, 196, 90)));
    private static readonly Brush _warmBrush     = Freeze(new SolidColorBrush(Color.FromRgb(212, 160, 23)));
    private static readonly Brush _hotBrush      = Freeze(new SolidColorBrush(Color.FromRgb(212, 103, 23)));
    private static readonly Brush _critBrush     = Freeze(new SolidColorBrush(Color.FromRgb(212, 48, 23)));

    private static Brush Freeze(SolidColorBrush b) { b.Freeze(); return b; }

    private static Brush GetTemperatureColor(float temp) => temp switch
    {
        < 50 => _coolBrush,
        < 70 => _warmBrush,
        < 85 => _hotBrush,
        _    => _critBrush,
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
