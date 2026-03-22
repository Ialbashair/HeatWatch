namespace HeatWatch.Core.Hardware;

public sealed class SensorPoller : IDisposable
{
    private readonly HardwareMonitor _monitor;
    private readonly int _intervalMs;
    private CancellationTokenSource? _cts;
    private Task? _pollingTask;

    public event EventHandler<IReadOnlyList<SensorReading>>? SensorsUpdated;

    public SensorPoller(HardwareMonitor monitor, int intervalMs = 1000)
    {
        _monitor = monitor;
        _intervalMs = intervalMs;
    }

    public void Start()
    {
        if (_pollingTask is not null) return;
        _cts = new CancellationTokenSource();
        _pollingTask = RunAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _pollingTask = null;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_intervalMs));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                var readings = _monitor.GetAllReadings();
                SensorsUpdated?.Invoke(this, readings);
            }
        }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
