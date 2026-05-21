using System.Net.Http;
using System.Windows;
using VerseStrings.Core;
using VerseStrings.Services;
using VerseStrings.Views;

namespace VerseStrings;

public partial class App : Application
{
    private const string SingleInstanceMutexName = "Global\\VerseStrings.SingleInstance";

    private Mutex? _singleInstanceMutex;
    private TrayController? _tray;
    private UpdateOrchestrator? _orchestrator;
    private HttpClient? _http;
    private SettingsStore? _settingsStore;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "VerseStrings is already running. Look for the icon in your system tray.",
                "Already running",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _settingsStore = SettingsStore.Default();
        var settings = _settingsStore.Load();

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var github = new GithubReleaseClient(_http);

        var backupsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VerseStrings", "backups");
        Directory.CreateDirectory(backupsRoot);

        var installer = new Installer(github, backupsRoot);
        var toast = new ToastService();
        var processWatcher = new ProcessWatcher();
        var autostart = new AutostartService();

        _orchestrator = new UpdateOrchestrator(_settingsStore, github, installer, toast, processWatcher);

        if (!settings.FirstRunCompleted)
        {
            RunFirstRunFlow();
            settings = _settingsStore.Load();
        }

        autostart.Sync(settings.AutostartEnabled);

        _tray = new TrayController(
            settingsStore: _settingsStore,
            orchestrator: _orchestrator,
            installer: installer,
            toast: toast,
            autostart: autostart,
            shutdown: Shutdown);
        _tray.Show();

        _ = _orchestrator.StartAsync();
        _ = CheckSelfUpdateAsync(new SelfUpdater(_http), toast);
    }

    private async Task CheckSelfUpdateAsync(SelfUpdater updater, ToastService toast)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            if (!await updater.CheckAsync()) return;

            Dispatcher.Invoke(() =>
            {
                _tray?.ShowSelfUpdateAvailable(updater.LatestVersion!);
                toast.Show(
                    $"VerseStrings v{updater.LatestVersion} is available",
                    "Open the tray menu and click \"Update VerseStrings\" to download.");
            });
        }
        catch
        {
            // Network or API errors are non-fatal — we'll check again next launch.
        }
    }

    private void RunFirstRunFlow()
    {
        var detected = GameLocator.TryDetectLiveFolder();
        var window = new SettingsWindow(_settingsStore!, detected, isFirstRun: true);
        window.ShowDialog();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _orchestrator?.Dispose();
        _http?.Dispose();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
