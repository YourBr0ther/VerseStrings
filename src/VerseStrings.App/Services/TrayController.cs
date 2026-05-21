using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using VerseStrings.Core;
using VerseStrings.Views;

namespace VerseStrings.Services;

public sealed class TrayController : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly SettingsStore _settingsStore;
    private readonly UpdateOrchestrator _orchestrator;
    private readonly Installer _installer;
    private readonly ToastService _toast;
    private readonly AutostartService _autostart;
    private readonly Action _shutdown;

    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _checkNowItem;
    private readonly ToolStripMenuItem _autostartItem;
    private ToolStripMenuItem? _selfUpdateItem;

    public TrayController(
        SettingsStore settingsStore,
        UpdateOrchestrator orchestrator,
        Installer installer,
        ToastService toast,
        AutostartService autostart,
        Action shutdown)
    {
        _settingsStore = settingsStore;
        _orchestrator = orchestrator;
        _installer = installer;
        _toast = toast;
        _autostart = autostart;
        _shutdown = shutdown;

        _statusItem = new ToolStripMenuItem("Idle") { Enabled = false };
        _checkNowItem = new ToolStripMenuItem("Check for updates now", null, async (_, _) => await OnCheckNow());
        _autostartItem = new ToolStripMenuItem("Start with Windows", null, (_, _) => OnToggleAutostart())
        {
            CheckOnClick = true,
            Checked = _autostart.IsEnabled(),
        };

        _icon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = Branding.AppName,
            Visible = false,
            ContextMenuStrip = BuildMenu(),
        };

        _orchestrator.StatusChanged += (_, _) => Application.Current.Dispatcher.Invoke(RefreshStatus);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_checkNowItem);
        menu.Items.Add(new ToolStripMenuItem("Settings…", null, (_, _) => OnOpenSettings()));
        menu.Items.Add(new ToolStripMenuItem("Restore previous version", null, (_, _) => OnRestoreBackup()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_autostartItem);
        menu.Items.Add(new ToolStripMenuItem("Open backups folder", null, (_, _) => OnOpenBackupsFolder()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Quit", null, (_, _) => _shutdown()));
        return menu;
    }

    public void Show()
    {
        _icon.Visible = true;
        RefreshStatus();
    }

    public void ShowSelfUpdateAvailable(Version newVersion)
    {
        var menu = _icon.ContextMenuStrip;
        if (menu is null || _selfUpdateItem is not null) return;

        _selfUpdateItem = new ToolStripMenuItem(
            $"⬇ Update VerseStrings to v{newVersion}",
            null,
            (_, _) => Process.Start(new ProcessStartInfo { FileName = SelfUpdater.ReleasesPageUrl, UseShellExecute = true }));

        menu.Items.Insert(0, _selfUpdateItem);
        menu.Items.Insert(1, new ToolStripSeparator());
    }

    private async Task OnCheckNow()
    {
        _checkNowItem.Enabled = false;
        try
        {
            _statusItem.Text = "Checking…";
            await _orchestrator.CheckNowAsync();
        }
        finally
        {
            _checkNowItem.Enabled = true;
            RefreshStatus();
        }
    }

    private void OnOpenSettings()
    {
        var window = new SettingsWindow(_settingsStore, hint: null, isFirstRun: false);
        window.ShowDialog();
        _autostart.Sync(_settingsStore.Load().AutostartEnabled);
        _autostartItem.Checked = _autostart.IsEnabled();
        RefreshStatus();
    }

    private void OnRestoreBackup()
    {
        var settings = _settingsStore.Load();
        if (string.IsNullOrWhiteSpace(settings.LiveFolderPath))
        {
            _toast.Show("Cannot restore", "Set your LIVE folder in Settings first.");
            return;
        }
        var backup = _installer.FindMostRecentBackup();
        if (backup is null)
        {
            _toast.Show("No backups", "There is nothing to restore yet.");
            return;
        }

        try
        {
            _installer.RestoreBackup(backup, settings.LiveFolderPath!);
            settings.LastAppliedSha256 = null;
            settings.LastAppliedReleaseName = "(restored from backup)";
            _settingsStore.Save(settings);
            _toast.Show("Restored", $"Restored backup from {Path.GetFileName(backup)}.");
        }
        catch (Exception ex)
        {
            _toast.Show("Restore failed", ex.Message);
        }
    }

    private void OnToggleAutostart()
    {
        var settings = _settingsStore.Load();
        settings.AutostartEnabled = _autostartItem.Checked;
        _settingsStore.Save(settings);
        _autostart.Sync(settings.AutostartEnabled);
    }

    private void OnOpenBackupsFolder()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Branding.AppName, "backups");
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });
    }

    private void RefreshStatus()
    {
        var settings = _settingsStore.Load();
        var label = settings.LastAppliedReleaseName is { Length: > 0 } name
            ? $"Last installed: {name}"
            : "No installs yet";
        _statusItem.Text = label;
    }

    private static Icon LoadIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
        if (File.Exists(iconPath))
            return new Icon(iconPath);
        return SystemIcons.Information;
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
