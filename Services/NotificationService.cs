using PomoTime.UI.Views;

namespace PomoTime.Services;
public static class NotificationService
{
    public static void Show(string title, string message) => new NotificationWindow(title, message).Show();
}
