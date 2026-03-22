using LibreHardwareMonitor.Hardware;

namespace HeatWatch.Core.Hardware;

public record SensorReading(
    string Id,
    string HardwareName,
    string HardwareType,
    string SensorName,
    SensorType Type,
    float? Value,
    string Unit
);
