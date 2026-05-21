using Microsoft.Toolkit.Uwp.Notifications;

namespace VerseStrings.Services;

public sealed class ToastService
{
    public void ShowInfo(string title, string body) =>
        BuildToast(title, body).Show();

    public void ShowSuccess(string title, string body) =>
        BuildToast(title, body).Show();

    public void ShowWarning(string title, string body) =>
        BuildToast(title, body).Show();

    public void ShowError(string title, string body) =>
        BuildToast(title, body).Show();

    private static ToastContentBuilder BuildToast(string title, string body) =>
        new ToastContentBuilder()
            .AddText(title)
            .AddText(body);
}
