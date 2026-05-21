using Microsoft.Win32;
using VerseStrings.Core;

namespace VerseStrings.Services;

public sealed class AutostartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = Branding.AppName;

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            return key?.GetValue(ValueName) is not null;
        }
        catch
        {
            // Why swallow: a registry-access failure here means we report
            // "autostart not enabled" — which matches what the user observes
            // anyway when the Run key is unreadable.
            return false;
        }
    }

    /// <summary>
    /// Brings the Run-key entry in line with <paramref name="desired"/>.
    /// Returns true on success; false if the registry write failed (locked
    /// by AV, permission denied, hive offline). Callers should surface a
    /// "couldn't update Start-with-Windows setting" message on false.
    /// </summary>
    public bool Sync(bool desired)
    {
        try
        {
            if (desired && !IsEnabled()) Enable();
            else if (!desired && IsEnabled()) Disable();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void Enable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKey);
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath)) return;
        key.SetValue(ValueName, $"\"{exePath}\"");
    }

    private static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
