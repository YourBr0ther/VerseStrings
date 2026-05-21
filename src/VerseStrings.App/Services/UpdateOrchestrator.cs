using VerseStrings.Core;

namespace VerseStrings.Services;

public sealed class UpdateOrchestrator : IDisposable
{
    private const string AssetName = "StarStrings.zip";

    private readonly SettingsStore _settings;
    private readonly GithubReleaseClient _github;
    private readonly Installer _installer;
    private readonly ToastService _toast;
    private readonly ProcessWatcher _processWatcher;

    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _checkLock = new(1, 1);

    public UpdateOrchestrator(
        SettingsStore settings,
        GithubReleaseClient github,
        Installer installer,
        ToastService toast,
        ProcessWatcher processWatcher)
    {
        _settings = settings;
        _github = github;
        _installer = installer;
        _toast = toast;
        _processWatcher = processWatcher;
    }

    public event EventHandler? StatusChanged;

    public Task StartAsync()
    {
        _ = Task.Run(() => LoopAsync(_cts.Token));
        return Task.CompletedTask;
    }

    public async Task CheckNowAsync()
    {
        await RunCheckAsync(_cts.Token);
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), ct);

        while (!ct.IsCancellationRequested)
        {
            await RunCheckAsync(ct);

            var settings = _settings.Load();
            var interval = TimeSpan.FromMinutes(Math.Max(1, settings.CheckIntervalMinutes));
            try { await Task.Delay(interval, ct); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task RunCheckAsync(CancellationToken ct)
    {
        if (!await _checkLock.WaitAsync(0, ct)) return;
        try
        {
            var settings = _settings.Load();
            if (string.IsNullOrWhiteSpace(settings.LiveFolderPath))
                return;

            var release = await _github.GetLatestAsync(settings.Repo, AssetName, ct);
            if (release is null) return;

            if (string.Equals(release.AssetSha256, settings.LastAppliedSha256, StringComparison.OrdinalIgnoreCase))
                return;

            if (_processWatcher.IsGameRunning())
            {
                _toast.Show(
                    "VerseStrings update pending",
                    $"New release \"{release.Name}\" will install when you close Star Citizen.");
                await _processWatcher.WaitForGameExitAsync(TimeSpan.FromSeconds(15), ct);
                if (ct.IsCancellationRequested) return;
            }

            try
            {
                var result = await _installer.InstallAsync(release, settings.LiveFolderPath!, ct);

                settings = _settings.Load();
                settings.LastAppliedSha256 = result.Sha256;
                settings.LastAppliedReleaseName = result.ReleaseName;
                settings.LastAppliedAt = DateTimeOffset.UtcNow;
                _settings.Save(settings);

                _toast.Show(
                    "VerseStrings updated",
                    $"Installed \"{result.ReleaseName}\" — {result.FilesInstalled} files. Backup saved.");
            }
            catch (Exception ex)
            {
                _toast.Show("VerseStrings update failed", ex.Message);
            }

            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _checkLock.Release();
        }
    }

    public void Dispose()
    {
        try { _cts.Cancel(); } catch { /* ignore */ }
        _cts.Dispose();
        _checkLock.Dispose();
    }
}
