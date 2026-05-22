using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using VerseStrings.Core;
using VerseStrings.Services;
using VerseStrings.Views;

namespace VerseStrings;

public partial class App : Application
{
    private const string SingleInstanceMutexName = $"Global\\{Branding.AppName}.SingleInstance";

    private Mutex? _singleInstanceMutex;
    private TrayController? _tray;
    private UpdateOrchestrator? _orchestrator;
    private HttpClient? _http;
    private SettingsStore? _settingsStore;
    private ToastService? _toast;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        HookGlobalExceptionHandlers();

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

        AppSettings settings;
        try
        {
            settings = _settingsStore.Load();
        }
        catch (JsonException ex)
        {
            // Why: SettingsStore deliberately throws on corrupt JSON rather
            // than silently overwriting with defaults. At startup that throw
            // would otherwise dump a stack trace on the user. Surface a clean
            // message pointing at the file so they can fix or delete it.
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Branding.AppName, "settings.json");
            MessageBox.Show(
                $"Couldn't read your VerseStrings settings — the file looks corrupted.\n\n" +
                $"File: {path}\n\nDetails: {ex.Message}\n\n" +
                "Fix the file or delete it (the app will recreate it on next launch).",
                "Settings file is corrupted",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var github = new GithubReleaseClient(_http);

        var backupsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Branding.AppName, "backups");
        Directory.CreateDirectory(backupsRoot);

        var installer = new Installer(github, backupsRoot);
        _toast = new ToastService();
        var processWatcher = new ProcessWatcher();
        var autostart = new AutostartService();

        _orchestrator = new UpdateOrchestrator(_settingsStore, github, installer, _toast, processWatcher);

        if (!settings.FirstRunCompleted)
        {
            ApplyInstallerPackHint(settings, e.Args);
            RunFirstRunFlow();
            settings = _settingsStore.Load();
        }

        autostart.Sync(settings.AutostartEnabled);

        _tray = new TrayController(
            settingsStore: _settingsStore,
            orchestrator: _orchestrator,
            installer: installer,
            toast: _toast,
            autostart: autostart,
            shutdown: Shutdown);
        _tray.Show();

        _orchestrator.Start();
        _ = CheckSelfUpdateAsync(new SelfUpdater(_http), _toast);
    }

    private void HookGlobalExceptionHandlers()
    {
        // Why hook all three: any one of them can fire on its own.
        //   - DispatcherUnhandledException: WPF UI-thread exceptions.
        //   - TaskScheduler.UnobservedTaskException: faulted Tasks no one awaited.
        //   - AppDomain.UnhandledException: everything else (background threads,
        //     finalizers). This one can't actually prevent process termination
        //     on modern .NET, but it still gives us one last chance to tell
        //     the user *why* before the process dies.
        DispatcherUnhandledException += (_, ev) =>
        {
            ReportUnhandled("Unexpected error", ev.Exception);
            ev.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (_, ev) =>
        {
            ReportUnhandled("Background task failed", ev.Exception);
            ev.SetObserved();
        };

        AppDomain.CurrentDomain.UnhandledException += (_, ev) =>
        {
            if (ev.ExceptionObject is Exception ex)
                ReportUnhandled("Fatal error", ex);
        };
    }

    private void ReportUnhandled(string title, Exception ex)
    {
        try { _toast?.Show(title, ex.Message); }
        catch { /* last-gasp; never let the reporter throw */ }
    }

    private async Task CheckSelfUpdateAsync(SelfUpdater updater, ToastService toast)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            var newer = await updater.CheckForNewVersionAsync();
            if (newer is null) return;

            Dispatcher.Invoke(() =>
            {
                _tray?.ShowSelfUpdateAvailable(newer);
                toast.Show(
                    $"VerseStrings v{newer} is available",
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

    /// <summary>
    /// The installer's `[Run]` line passes `--pack=&lt;id&gt;` matching the user's
    /// choice in the wizard. We pre-select that pack in settings before the
    /// first-run dialog opens, so the user lands on the pack picker already
    /// configured to what they just chose in the installer.
    /// </summary>
    private void ApplyInstallerPackHint(AppSettings settings, string[] args)
    {
        var packId = InstallerArgs.TryGetPackId(args);
        if (packId is null) return;

        settings.SelectedPackId = packId;
        _settingsStore!.Save(settings);
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
