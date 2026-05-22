using System.Windows;
using Microsoft.Win32;
using VerseStrings.Core;
using VerseStrings.Services;

namespace VerseStrings.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsStore _settingsStore;
    private readonly bool _isFirstRun;

    public SettingsWindow(SettingsStore settingsStore, string? hint, bool isFirstRun)
    {
        _settingsStore = settingsStore;
        _isFirstRun = isFirstRun;
        InitializeComponent();

        var settings = _settingsStore.Load();

        if (isFirstRun)
        {
            HeaderText.Text = "Welcome — let's set up VerseStrings";
            StatusText.Text = "Confirm your LIVE folder and choose whether to start with Windows.";
        }
        else
        {
            HeaderText.Text = "VerseStrings Settings";
            if (settings.LastAppliedAt is { } at)
            {
                StatusText.Text = $"Last installed {settings.LastAppliedReleaseName} at {at.ToLocalTime():g}";
            }
        }

        LiveFolderBox.Text = settings.LiveFolderPath ?? hint ?? string.Empty;
        IntervalBox.Text = settings.CheckIntervalMinutes.ToString();
        AutostartBox.IsChecked = isFirstRun ? true : settings.AutostartEnabled;

        foreach (var pack in Packs.All)
            PackBox.Items.Add(pack.DisplayName);
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
        if (_isFirstRun && string.IsNullOrWhiteSpace(_settingsStore.Load().LiveFolderPath))
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
        var path = LiveFolderBox.Text?.Trim() ?? string.Empty;
        if (!GameLocator.LooksLikeLiveFolder(path))
        {
            MessageBox.Show(
                "That doesn't look like your Star Citizen LIVE folder. " +
                "It should be the folder that contains Bin64\\StarCitizen.exe.",
                "Folder not recognized",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(IntervalBox.Text, out var minutes) || minutes < 1)
        {
            MessageBox.Show("Check interval must be a positive number of minutes.",
                "Invalid interval", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var settings = _settingsStore.Load();
        settings.LiveFolderPath = path;
        settings.CheckIntervalMinutes = minutes;
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
            return;
        }

        DialogResult = true;
        Close();
    }
}
