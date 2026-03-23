using System.Diagnostics;

namespace HeatWatch.Core.Startup;

/// <summary>
/// Manages the Windows auto-start entry for HeatWatch.
/// Uses Task Scheduler (schtasks) rather than the registry Run key because
/// the app requires administrator elevation — a Run key entry would trigger
/// a UAC prompt on every login, whereas a scheduled task with /rl HIGHEST does not.
/// </summary>
public static class StartupManager
{
    private const string TaskName = "HeatWatch";

    public static bool IsEnabled()
    {
        var result = Run("schtasks", $"/query /tn \"{TaskName}\"");
        return result == 0;
    }

    public static void Enable()
    {
        var exe = Environment.ProcessPath ?? string.Empty;
        Run("schtasks",
            $"/create /tn \"{TaskName}\" /tr \"\\\"{exe}\\\"\" /sc ONLOGON /rl HIGHEST /f");
    }

    public static void Disable()
    {
        Run("schtasks", $"/delete /tn \"{TaskName}\" /f");
    }

    private static int Run(string file, string args)
    {
        using var p = new Process
        {
            StartInfo = new ProcessStartInfo(file, args)
            {
                UseShellExecute  = false,
                CreateNoWindow   = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            }
        };
        p.Start();
        p.WaitForExit();
        return p.ExitCode;
    }
}
