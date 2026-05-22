using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;
using VerseStrings.Core;
using VerseStrings.Services;

namespace VerseStrings.Views;

public enum SettingsWindowMode
{
    /// <summary>Initial setup wizard. User hasn't completed first-run yet.</summary>
    FirstRun,

    /// <summary>Settings dialog opened from the tray menu. ShowDialog'd, modal.</summary>
    FromTrayMenu,

    /// <summary>The main app window in standalone mode. Window owns the app
    /// lifetime — closing it shuts the process down.</summary>
    Standalone,
}

public partial class SettingsWindow : Window
{
    private readonly SettingsStore _settingsStore;
    private readonly UpdateOrchestrator _orchestrator;
    private readonly ToastService _toast;
    private readonly SettingsWindowMode _mode;

    private Task? _syncTask;

    public SettingsWindow(
        SettingsStore settingsStore,
        string? hint,
        SettingsWindowMode mode,
        UpdateOrchestrator orchestrator,
        ToastService toast)
    {
        _settingsStore = settingsStore;
        _mode = mode;
        _orchestrator = orchestrator;
        _toast = toast;
        InitializeComponent();

        var settings = _settingsStore.Load();
        ApplyMode(settings);
        PopulateFields(settings, hint);
    }

    private void ApplyMode(AppSettings settings)
    {
        switch (_mode)
        {
            case SettingsWindowMode.FirstRun:
                Title = "VerseStrings — Setup";
                HeaderText.Text = "Welcome — let's set up VerseStrings";
                StatusText.Text = "Confirm your LIVE folder and choose whether to start with Windows.";
                break;

            case SettingsWindowMode.FromTrayMenu:
                Title = "VerseStrings Settings";
                HeaderText.Text = "VerseStrings Settings";
                if (settings.LastAppliedAt is { } at)
                {
                    StatusText.Text = $"Last installed {settings.LastAppliedReleaseName} at {at.ToLocalTime():g}";
                }
                break;

            case SettingsWindowMode.Standalone:
                Title = "VerseStrings";
                HeaderText.Text = "VerseStrings";
                StatusText.Text = settings.LastAppliedAt is { } sAt
                    ? $"Last installed {settings.LastAppliedReleaseName} at {sAt.ToLocalTime():g}"
                    : "No installs yet — click Sync now to install your selected pack.";
                // Interval-row and autostart-checkbox are tray-mode features.
                IntervalPanel.Visibility = Visibility.Collapsed;
                AutostartBox.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Collapsed;
                SaveButton.Visibility = Visibility.Collapsed;
                CloseButton.Visibility = Visibility.Visible;
                SyncButton.Visibility = Visibility.Visible;
                Closing += OnWindowClosing;
                Closed += OnWindowClosed;
                break;
        }
    }

    private void PopulateFields(AppSettings settings, string? hint)
    {
        LiveFolderBox.Text = settings.LiveFolderPath ?? hint ?? string.Empty;
        IntervalBox.Text = settings.CheckIntervalMinutes.ToString();
        AutostartBox.IsChecked = _mode == SettingsWindowMode.FirstRun || settings.AutostartEnabled;

        foreach (var pack in Packs.All)
            PackBox.Items.Add(pack.Label);
        var currentId = (Packs.ById(settings.SelectedPackId) ?? Packs.Default).Id;
        for (var i = 0; i < Packs.All.Count; i++)
        {
            if (Packs.All[i].Id == currentId) { PackBox.SelectedIndex = i; break; }
        }
    }

    private void OnBrowseClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select your Star Citizen LIVE folder",
            InitialDirectory = LiveFolderBox.Text is { Length: > 0 } existing
                ? existing
                : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        };
        if (dialog.ShowDialog() == true)
        {
            LiveFolderBox.Text = dialog.FolderName;
        }
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        if (_mode == SettingsWindowMode.FirstRun && string.IsNullOrWhiteSpace(_settingsStore.Load().LiveFolderPath))
        {
            var result = MessageBox.Show(
                "VerseStrings needs your LIVE folder to work. Quit without setting it up?",
                "Cancel setup?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.No) return;
        }
        DialogResult = false;
        Close();
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        if (!ValidateAndCommit()) return;
        DialogResult = true;
        Close();
    }

    private async void OnSyncClicked(object sender, RoutedEventArgs e)
    {
        if (_syncTask is { IsCompleted: false }) return;
        if (!ValidateAndCommit()) return;

        SyncButton.IsEnabled = false;
        SyncButton.Content = "Syncing…";
        StatusText.Text = "Syncing…";

        try
        {
            var task = _orchestrator.SyncNowAsync();
            _syncTask = task;
            var outcome = await task;
            ToastForOutcome(outcome);
            RefreshStatusFromSettings();
        }
        catch (Exception ex)
        {
            // Defensive: SyncNowAsync swallows install-side exceptions and
            // returns SyncOutcome.Failed, but a hard crash deeper in the
            // orchestrator shouldn't take down the window.
            _toast.Show("Sync failed", ex.Message);
        }
        finally
        {
            SyncButton.IsEnabled = true;
            SyncButton.Content = "Sync now";
        }
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e) => Close();

    private async void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_syncTask is null || _syncTask.IsCompleted) return;

        // Hold the close until the in-flight sync settles, so we don't kill
        // a download mid-stream or leave the LIVE folder in a half-applied state.
        e.Cancel = true;
        StatusText.Text = "Finishing install, please wait…";
        SyncButton.IsEnabled = false;
        CloseButton.IsEnabled = false;
        try { await _syncTask; } catch { /* sync's own catch will have toasted */ }
        Close();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        // Standalone mode owns the app lifetime. App.xaml sets
        // ShutdownMode=OnExplicitShutdown for the tray case; for standalone,
        // we trigger the shutdown here when the main window goes away.
        Application.Current.Shutdown();
    }

    /// <summary>
    /// Validates LIVE folder and interval, persists settings to disk.
    /// Returns true on success; false if validation failed (user already
    /// saw a MessageBox) or save threw (user saw a MessageBox).
    /// </summary>
    private bool ValidateAndCommit()
    {
        var path = LiveFolderBox.Text?.Trim() ?? string.Empty;
        if (!GameLocator.LooksLikeLiveFolder(path))
        {
            MessageBox.Show(
                "That doesn't look like your Star Citizen LIVE folder. " +
                "It should be the folder that contains Bin64\\StarCitizen.exe.",
                "Folder not recognized",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        var minutes = _settingsStore.Load().CheckIntervalMinutes;
        if (_mode != SettingsWindowMode.Standalone)
        {
            if (!int.TryParse(IntervalBox.Text, out minutes) || minutes < 1)
            {
                MessageBox.Show("Check interval must be a positive number of minutes.",
                    "Invalid interval", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        var settings = _settingsStore.Load();
        settings.LiveFolderPath = path;
        settings.CheckIntervalMinutes = minutes;
        if (_mode != SettingsWindowMode.Standalone)
            settings.AutostartEnabled = AutostartBox.IsChecked == true;
        settings.FirstRunCompleted = true;

        var selectedIndex = PackBox.SelectedIndex;
        if (selectedIndex >= 0 && selectedIndex < Packs.All.Count)
            settings.SelectedPackId = Packs.All[selectedIndex].Id;

        try
        {
            _settingsStore.Save(settings);
        }
        catch (Exception ex)
        {
            // Why surface this: the dialog closes on success, so a silent
            // throw here would close-and-look-saved while nothing was written.
            // Disk-full, AV holding the file, and read-only %APPDATA% are all
            // plausible failures we want the user to see.
            MessageBox.Show(
                $"Couldn't save settings: {ex.Message}\n\n" +
                "Check that %APPDATA%\\VerseStrings is writable and try again.",
                "Save failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }

        return true;
    }

    private void ToastForOutcome(SyncOutcome outcome)
    {
        // Installed and Failed cases already toasted from inside RunCheckAsync —
        // we only need to surface the two outcomes the orchestrator stays silent on.
        switch (outcome)
        {
            case SyncOutcome.NoChange:
                var pack = Packs.ById(_settingsStore.Load().SelectedPackId) ?? Packs.Default;
                _toast.Show("Already up to date",
                    $"Latest {pack.DisplayName} release is already installed.");
                break;
            case SyncOutcome.GameRunning:
                _toast.Show("Star Citizen is running",
                    "Close the game and click Sync again.");
                break;
        }
    }

    private void RefreshStatusFromSettings()
    {
        var settings = _settingsStore.Load();
        StatusText.Text = settings.LastAppliedAt is { } at
            ? $"Last installed {settings.LastAppliedReleaseName} at {at.ToLocalTime():g}"
            : "No installs yet.";
    }
}
