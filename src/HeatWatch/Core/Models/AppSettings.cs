namespace HeatWatch.Core.Models;

public sealed class AppSettings
{
    public bool IsTopmost { get; set; } = false;
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;
    public int PollingIntervalMs { get; set; } = 1000;
    public bool HasSeenSettings { get; set; } = false;
    public AppTheme Theme { get; set; } = AppTheme.SystemDefault;
    public AppView View { get; set; } = AppView.Grid;
    public bool IsResizable { get; set; } = false;
    public double NodeOpacity { get; set; } = 0.8;
    public double BackgroundOpacity { get; set; } = 0.5;
    public bool AutoHideControls { get; set; } = false;
    public bool UseHeatColors { get; set; } = true;
    public List<NodeDefinition> Nodes { get; set; } = new List<NodeDefinition>();
}
