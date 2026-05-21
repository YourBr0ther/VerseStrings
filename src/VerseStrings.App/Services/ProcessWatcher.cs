using System.Diagnostics;

namespace VerseStrings.Services;

public sealed class ProcessWatcher
{
    public bool IsGameRunning() =>
        Process.GetProcessesByName("StarCitizen").Length > 0;

    public async Task WaitForGameExitAsync(TimeSpan pollInterval, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && IsGameRunning())
        {
            try { await Task.Delay(pollInterval, ct); }
            catch (OperationCanceledException) { return; }
        }
    }
}
