using Microsoft.Win32;
using VerseStrings.Core;

namespace VerseStrings.Services;

public sealed class AutostartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = Branding.AppName;

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(ValueName) is not null;
    }

    public void Enable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKey);
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath)) return;
        key.SetValue(ValueName, $"\"{exePath}\"");
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    public void Sync(bool desired)
    {
        if (desired && !IsEnabled()) Enable();
        else if (!desired && IsEnabled()) Disable();
    }
}
