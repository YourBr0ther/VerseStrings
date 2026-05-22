using VerseStrings.Core;

namespace VerseStrings.Services;

public sealed class UpdateOrchestrator : IDisposable
{
    private readonly SettingsStore _settingsStore;
    private readonly GithubReleaseClient _github;
    private readonly Installer _installer;
    private readonly ToastService _toast;
    private readonly ProcessWatcher _processWatcher;

    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _checkLock = new(1, 1);

    public UpdateOrchestrator(
        SettingsStore settingsStore,
        GithubReleaseClient github,
        Installer installer,
        ToastService toast,
        ProcessWatcher processWatcher)
    {
        _settingsStore = settingsStore;
        _github = github;
        _installer = installer;
        _toast = toast;
        _processWatcher = processWatcher;
    }

    public event EventHandler? StatusChanged;

    public void Start()
    {
        _ = Task.Run(() => LoopAsync(_cts.Token));
    }

    public async Task CheckNowAsync()
    {
        await RunCheckAsync(_cts.Token, waitForGameExit: true);
    }

    /// <summary>
    /// Persist a new pack selection and trigger an immediate check. The
    /// existing install/backup flow handles the actual file replacement —
    /// clearing LastAppliedSha256 forces the next check to treat the new
    /// pack's release as a fresh install even when the SHA happens to
    /// match what's on disk.
    /// </summary>
    public async Task SwitchPackAsync(Pack pack, CancellationToken ct = default)
    {
        var settings = _settingsStore.Load();
        settings.SelectedPackId = pack.Id;
        settings.LastAppliedSha256 = null;
        _settingsStore.Save(settings);

        await RunCheckAsync(ct, waitForGameExit: true);
    }

    /// <summary>
    /// One-shot sync used by the standalone-mode UI. Returns the outcome so
    /// the caller can toast "already up to date" / "game running" / etc.
    /// Unlike the tray's polling loop, this does not block waiting for Star
    /// Citizen to exit — the user is in front of the window and can re-click
    /// Sync once the game is closed.
    /// </summary>
    public async Task<SyncOutcome> SyncNowAsync(CancellationToken ct = default)
    {
        return await RunCheckAsync(ct, waitForGameExit: false);
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(5), ct); }
        catch (OperationCanceledException) { return; }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunCheckAsync(ct, waitForGameExit: true);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                // Why catch-all: this loop is the only thing keeping the
                // watcher alive. Anything that escapes — a transient HTTP
                // exception, a JSON parse error in settings.json, an
                // unexpected I/O failure — must not kill polling forever.
                _toast.Show("Update check failed", ex.Message);
            }

            var interval = TimeSpan.FromMinutes(Math.Max(1, LoadIntervalMinutesSafe()));
            try { await Task.Delay(interval, ct); }
            catch (OperationCanceledException) { return; }
        }
    }

    private int LoadIntervalMinutesSafe()
    {
        try { return _settingsStore.Load().CheckIntervalMinutes; }
        catch { return 15; }
    }

    private async Task<SyncOutcome> RunCheckAsync(CancellationToken ct, bool waitForGameExit)
    {
        if (!await _checkLock.WaitAsync(0, ct)) return SyncOutcome.NoChange;
        try
        {
            var settings = _settingsStore.Load();
            if (string.IsNullOrWhiteSpace(settings.LiveFolderPath))
                return SyncOutcome.NoChange;

            var pack = Packs.ById(settings.SelectedPackId) ?? Packs.Default;
            var release = await _github.GetLatestAsync(pack.Repo, pack.AssetPattern, ct);
            if (release is null) return SyncOutcome.NoChange;

            if (string.Equals(release.AssetSha256, settings.LastAppliedSha256, StringComparison.OrdinalIgnoreCase))
                return SyncOutcome.NoChange;

            if (_processWatcher.IsGameRunning())
            {
                if (!waitForGameExit) return SyncOutcome.GameRunning;

                _toast.Show(
                    "VerseStrings update pending",
                    $"New release \"{release.Name}\" will install when you close Star Citizen.");
                await _processWatcher.WaitForGameExitAsync(TimeSpan.FromSeconds(15), ct);
                if (ct.IsCancellationRequested) return SyncOutcome.NoChange;
            }

            try
            {
                var result = await _installer.InstallAsync(release, settings.LiveFolderPath!, ct);

                settings = _settingsStore.Load();
                settings.LastAppliedSha256 = result.Sha256;
                settings.LastAppliedReleaseName = result.ReleaseName;
                settings.LastAppliedAt = DateTimeOffset.UtcNow;
                _settingsStore.Save(settings);

                _toast.Show(
                    "VerseStrings updated",
                    $"Installed \"{result.ReleaseName}\" — {result.FilesInstalled} files. Backup saved.");

                StatusChanged?.Invoke(this, EventArgs.Empty);
                return SyncOutcome.Installed;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _toast.Show("VerseStrings update failed", ex.Message);
                StatusChanged?.Invoke(this, EventArgs.Empty);
                return SyncOutcome.Failed;
            }
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
