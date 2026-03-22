namespace HeatWatch.Core.Models;

public sealed class AppSettings
{
    public bool IsTopmost { get; set; } = true;
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;
    public int PollingIntervalMs { get; set; } = 1000;
    public bool HasSeenSettings { get; set; } = false;
    public List<NodeDefinition> Nodes { get; set; } = new List<NodeDefinition>();
}
