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
    private readonly ToolStripMenuItem _packMenu;
    private readonly Dictionary<string, ToolStripMenuItem> _packItems = new();
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
        _checkNowItem = new ToolStripMenuItem(
            "Check for updates now",
            null,
            async (_, _) => await SafeInvokeAsync("Update check failed", OnCheckNow));
        _autostartItem = new ToolStripMenuItem(
            "Start with Windows",
            null,
            (_, _) => SafeInvoke("Couldn't update autostart", OnToggleAutostart))
        {
            CheckOnClick = true,
            Checked = _autostart.IsEnabled(),
        };

        _packMenu = BuildPackMenu();

        _icon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = Branding.AppName,
            Visible = false,
            ContextMenuStrip = BuildMenu(),
        };

        _orchestrator.StatusChanged += (_, _) =>
            Application.Current.Dispatcher.Invoke(() => SafeInvoke("Status refresh failed", RefreshStatus));
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_checkNowItem);
        menu.Items.Add(new ToolStripMenuItem(
            "Settings…", null,
            (_, _) => SafeInvoke("Couldn't open Settings", OnOpenSettings)));
        menu.Items.Add(new ToolStripMenuItem(
            "Restore previous version", null,
            (_, _) => SafeInvoke("Restore failed", OnRestoreBackup)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_packMenu);
        menu.Items.Add(_autostartItem);
        menu.Items.Add(new ToolStripMenuItem(
            "Open backups folder", null,
            (_, _) => SafeInvoke("Couldn't open backups folder", OnOpenBackupsFolder)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem(
            "Quit", null,
            (_, _) => SafeInvoke("Shutdown failed", _shutdown)));
        return menu;
    }

    private ToolStripMenuItem BuildPackMenu()
    {
        var parent = new ToolStripMenuItem("Localization pack");
        var selectedId = _settingsStore.Load().SelectedPackId;

        foreach (var pack in Packs.All)
        {
            var capturedPack = pack;
            var item = new ToolStripMenuItem(
                pack.DisplayName,
                null,
                async (_, _) => await SafeInvokeAsync(
                    "Couldn't switch pack",
                    () => OnSelectPack(capturedPack)))
            {
                Checked = string.Equals(pack.Id, selectedId, StringComparison.Ordinal),
            };
            _packItems[pack.Id] = item;
            parent.DropDownItems.Add(item);
        }

        return parent;
    }

    private async Task OnSelectPack(Pack pack)
    {
        var settings = _settingsStore.Load();
        if (string.Equals(settings.SelectedPackId, pack.Id, StringComparison.Ordinal))
            return; // already on this pack — no-op rather than reinstall

        _toast.Show("Switching pack", $"Switching to {pack.DisplayName}…");
        SyncPackChecks(pack.Id);

        await _orchestrator.SwitchPackAsync(pack);
        SyncPackChecks(_settingsStore.Load().SelectedPackId);
    }

    private void SyncPackChecks(string activeId)
    {
        foreach (var (id, item) in _packItems)
            item.Checked = string.Equals(id, activeId, StringComparison.Ordinal);
    }

    public void Show()
    {
        _icon.Visible = true;
        SafeInvoke("Status refresh failed", RefreshStatus);
    }

    public void ShowSelfUpdateAvailable(Version newVersion)
    {
        var menu = _icon.ContextMenuStrip;
        if (menu is null || _selfUpdateItem is not null) return;

        _selfUpdateItem = new ToolStripMenuItem(
            $"⬇ Update VerseStrings to v{newVersion}",
            null,
            (_, _) => SafeInvoke("Couldn't open releases page", OpenReleasesPage));

        menu.Items.Insert(0, _selfUpdateItem);
        menu.Items.Insert(1, new ToolStripSeparator());
    }

    private static void OpenReleasesPage() =>
        Process.Start(new ProcessStartInfo
        {
            FileName = SelfUpdater.ReleasesPageUrl,
            UseShellExecute = true,
        });

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
            SafeInvoke("Status refresh failed", RefreshStatus);
        }
    }

    private void OnOpenSettings()
    {
        var window = new SettingsWindow(_settingsStore, hint: null, isFirstRun: false);
        window.ShowDialog();

        var desired = _settingsStore.Load().AutostartEnabled;
        if (!_autostart.Sync(desired))
        {
            _toast.Show("Couldn't update autostart",
                "Windows refused the registry change. Try toggling it from the tray menu.");
        }
        _autostartItem.Checked = _autostart.IsEnabled();
        SafeInvoke("Status refresh failed", RefreshStatus);
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

        var result = _installer.RestoreBackup(backup, settings.LiveFolderPath!);

        settings.LastAppliedSha256 = null;
        settings.LastAppliedReleaseName = "(restored from backup)";
        _settingsStore.Save(settings);

        if (result.FailedFiles.Count == 0)
        {
            _toast.Show("Restored",
                $"Restored {result.FilesRestored} file(s) from {Path.GetFileName(backup)}.");
        }
        else
        {
            _toast.Show("Restored with errors",
                $"Restored {result.FilesRestored} file(s) from {Path.GetFileName(backup)}; " +
                $"{result.FailedFiles.Count} could not be written (game running or files locked?). " +
                "Try again with Star Citizen closed.");
        }
    }

    private void OnToggleAutostart()
    {
        var settings = _settingsStore.Load();
        settings.AutostartEnabled = _autostartItem.Checked;
        _settingsStore.Save(settings);
        if (!_autostart.Sync(settings.AutostartEnabled))
        {
            // Revert visible state so the menu doesn't lie about what's actually set.
            _autostartItem.Checked = _autostart.IsEnabled();
            _toast.Show("Couldn't update autostart",
                "Windows refused the registry change. The setting was not applied.");
        }
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

    private void SafeInvoke(string failureTitle, Action body)
    {
        try { body(); }
        catch (Exception ex)
        {
            _toast.Show(failureTitle, ex.Message);
        }
    }

    private async Task SafeInvokeAsync(string failureTitle, Func<Task> body)
    {
        try { await body(); }
        catch (Exception ex)
        {
            _toast.Show(failureTitle, ex.Message);
        }
    }

    private static Icon LoadIcon()
    {
        // Why a stream rather than a file path: the published exe is single-
        // file and the installer doesn't ship a sidecar Assets folder, so a
        // disk-based load was silently falling back to SystemIcons.Information
        // on installed machines. The icon is now an EmbeddedResource.
        using var stream = typeof(TrayController).Assembly
            .GetManifestResourceStream("VerseStrings.icon.ico");
        return stream is not null ? new Icon(stream) : SystemIcons.Information;
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
