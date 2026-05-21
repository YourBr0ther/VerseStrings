using Microsoft.Toolkit.Uwp.Notifications;

namespace VerseStrings.Services;

public sealed class ToastService
{
    public void Show(string title, string body) =>
        new ToastContentBuilder()
            .AddText(title)
            .AddText(body)
            .Show();
}
