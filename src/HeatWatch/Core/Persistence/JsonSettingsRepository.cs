using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using HeatWatch.Core.Hardware;
using HeatWatch.Core.Models;
using LibreHardwareMonitor.Hardware;

namespace HeatWatch.Core.Persistence;

[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(NodeDefinition))]
[JsonSerializable(typeof(List<NodeDefinition>))]
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
internal partial class SettingsJsonContext : JsonSerializerContext { }

public sealed class JsonSettingsRepository
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HeatWatch");

    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new JsonStringEnumConverter() },
    };

    public AppSettings Load(IReadOnlyList<SensorReading> discoveredSensors)
    {
        AppSettings? saved = null;

        if (File.Exists(SettingsPath))
        {
            try
            {
                var json = File.ReadAllText(SettingsPath);
                saved = JsonSerializer.Deserialize<AppSettings>(json, Options);
            }
            catch
            {
                // Corrupted settings — start fresh
            }
        }

        return MergeWithDiscovered(saved ?? new AppSettings(), discoveredSensors);
    }

    private static AppSettings MergeWithDiscovered(AppSettings saved, IReadOnlyList<SensorReading> discovered)
    {
        var savedById = saved.Nodes.ToDictionary(n => n.SensorId);
        var result = new List<NodeDefinition>();

        // Preserve order of saved nodes that still exist
        foreach (var node in saved.Nodes)
        {
            if (discovered.Any(r => r.Id == node.SensorId))
                result.Add(node);
        }

        // Append newly discovered sensors not in saved list (all disabled initially)
        bool isFirstRun = saved.Nodes.Count == 0;

        foreach (var reading in discovered)
        {
            if (savedById.ContainsKey(reading.Id))
                continue;

            result.Add(new NodeDefinition
            {
                SensorId = reading.Id,
                Label = reading.SensorName,
                HardwareType = reading.HardwareType,
                HardwareName = reading.HardwareName,
                SensorType = reading.Type,
                IsEnabled = false,
            });
        }

        // First run: enable exactly one CPU temp and one GPU temp
        if (isFirstRun)
            ApplyFirstRunDefaults(result);

        // Safety net: if nothing is enabled after all that, enable all CPU+GPU temps
        if (result.Count > 0 && !result.Any(n => n.IsEnabled))
            ApplyFirstRunDefaults(result);

        saved.Nodes = result;
        return saved;
    }

    private static void ApplyFirstRunDefaults(List<NodeDefinition> nodes)
    {
        // Best CPU temp: prefer "Package", then "Tdie"/"Tctl" (AMD), then first CPU temp
        var cpuTemp =
            nodes.FirstOrDefault(n => IsCpuTemp(n) && n.Label.Contains("Package", StringComparison.OrdinalIgnoreCase)) ??
            nodes.FirstOrDefault(n => IsCpuTemp(n) && (n.Label.Contains("Tdie", StringComparison.OrdinalIgnoreCase) ||
                                                        n.Label.Contains("Tctl", StringComparison.OrdinalIgnoreCase))) ??
            nodes.FirstOrDefault(n => IsCpuTemp(n));

        // Best GPU temp: prefer "Core", then first GPU temp
        var gpuTemp =
            nodes.FirstOrDefault(n => IsGpuTemp(n) && n.Label.Contains("Core", StringComparison.OrdinalIgnoreCase)) ??
            nodes.FirstOrDefault(n => IsGpuTemp(n));

        if (cpuTemp is not null) cpuTemp.IsEnabled = true;
        if (gpuTemp is not null) gpuTemp.IsEnabled = true;
    }

    private static bool IsCpuTemp(NodeDefinition n) =>
        n.SensorType == SensorType.Temperature && n.HardwareType.Contains("Cpu");

    private static bool IsGpuTemp(NodeDefinition n) =>
        n.SensorType == SensorType.Temperature && n.HardwareType.Contains("Gpu");

public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDir);

        var json = JsonSerializer.Serialize(settings, Options);
        var tmp = SettingsPath + ".tmp";
        File.WriteAllText(tmp, json);

        // Atomic replace
        if (File.Exists(SettingsPath))
            File.Replace(tmp, SettingsPath, null);
        else
            File.Move(tmp, SettingsPath);
    }
}
