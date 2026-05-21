using System.Diagnostics;

namespace VerseStrings.Services;

public sealed class ProcessWatcher
{
    private static readonly string[] GameProcessNames = { "StarCitizen", "StarCitizen_Launcher" };

    public bool IsGameRunning()
    {
        foreach (var name in GameProcessNames)
        {
            if (Process.GetProcessesByName(name).Length > 0)
                return true;
        }
        return false;
    }

    public async Task WaitForGameExitAsync(TimeSpan pollInterval, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && IsGameRunning())
        {
            try { await Task.Delay(pollInterval, ct); }
            catch (OperationCanceledException) { return; }
        }
    }
}
