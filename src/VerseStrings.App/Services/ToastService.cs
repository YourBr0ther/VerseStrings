using Microsoft.Toolkit.Uwp.Notifications;

namespace VerseStrings.Services;

public sealed class ToastService
{
    /// <summary>
    /// Best-effort toast. Notifications can throw when Focus Assist /
    /// Do Not Disturb is on, when the user has disabled toast notifications
    /// for the app, or when the toast subsystem isn't ready yet at startup.
    /// We never want the act of informing the user to be the thing that
    /// crashes the app, so failures are swallowed.
    /// </summary>
    public void Show(string title, string body)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(body)
                .Show();
        }
        catch
        {
            // Intentionally suppressed — see XML doc.
        }
    }
}
