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
        await RunCheckAsync(_cts.Token);
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

        await RunCheckAsync(ct);
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(5), ct); }
        catch (OperationCanceledException) { return; }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunCheckAsync(ct);
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

    private async Task RunCheckAsync(CancellationToken ct)
    {
        if (!await _checkLock.WaitAsync(0, ct)) return;
        try
        {
            var settings = _settingsStore.Load();
            if (string.IsNullOrWhiteSpace(settings.LiveFolderPath))
                return;

            var pack = Packs.ById(settings.SelectedPackId) ?? Packs.Default;
            var release = await _github.GetLatestAsync(pack.Repo, pack.AssetPattern, ct);
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

                settings = _settingsStore.Load();
                settings.LastAppliedSha256 = result.Sha256;
                settings.LastAppliedReleaseName = result.ReleaseName;
                settings.LastAppliedAt = DateTimeOffset.UtcNow;
                _settingsStore.Save(settings);

                _toast.Show(
                    "VerseStrings updated",
                    $"Installed \"{result.ReleaseName}\" — {result.FilesInstalled} files. Backup saved.");
            }
            catch (OperationCanceledException)
            {
                throw;
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
