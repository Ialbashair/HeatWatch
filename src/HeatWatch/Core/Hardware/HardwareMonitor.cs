using LibreHardwareMonitor.Hardware;

namespace HeatWatch.Core.Hardware;

public sealed class HardwareMonitor : IDisposable
{
    private readonly Computer _computer;
    private readonly UpdateVisitor _visitor = new();
    private bool _disposed;

    private static readonly HashSet<SensorType> SupportedTypes =
    [
        SensorType.Temperature,
        SensorType.Clock,
        SensorType.Load,
        SensorType.Data,
        SensorType.SmallData,
        SensorType.Power,
        SensorType.Fan,
    ];

    public HardwareMonitor()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
            IsMemoryEnabled = true,
            IsStorageEnabled = false,
            IsNetworkEnabled = false,
            IsControllerEnabled = false,
            IsBatteryEnabled = false,
        };
        _computer.Open();
    }

    public IReadOnlyList<SensorReading> GetAllReadings()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _computer.Accept(_visitor);

        var readings = new List<SensorReading>();
        var seenIds = new HashSet<string>();
        foreach (IHardware hardware in _computer.Hardware)
            CollectFromHardware(hardware, readings, seenIds);

        return readings;
    }

    private static void CollectFromHardware(IHardware hardware, List<SensorReading> readings, HashSet<string> seenIds)
    {
        foreach (ISensor sensor in hardware.Sensors)
        {
            if (!SupportedTypes.Contains(sensor.SensorType))
                continue;

            var id = BuildSensorId(hardware, sensor);
            if (!seenIds.Add(id))
                continue; // LHM sometimes exposes the same sensor on both a hardware node and its sub-hardware

            readings.Add(new SensorReading(
                Id: id,
                HardwareName: hardware.Name,
                HardwareType: hardware.HardwareType.ToString(),
                SensorName: sensor.Name,
                Type: sensor.SensorType,
                Value: sensor.Value,
                Unit: GetUnit(sensor.SensorType)
            ));
        }

        foreach (IHardware sub in hardware.SubHardware)
            CollectFromHardware(sub, readings, seenIds);
    }

    public static string BuildSensorId(IHardware hardware, ISensor sensor)
        => $"{hardware.Identifier}/{sensor.SensorType}/{sensor.Index}";

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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _computer.Close();
    }
}
