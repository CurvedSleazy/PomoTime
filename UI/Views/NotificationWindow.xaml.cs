using System.Windows;
using System.Windows.Threading;

namespace PomoTime.UI.Views;
public partial class NotificationWindow : Window
{
    public NotificationWindow(string title, string message)
    {
        InitializeComponent(); TitleText.Text = title; MessageText.Text = message;
        Loaded += (_, _) => { Left = SystemParameters.WorkArea.Right - Width - 18; Top = SystemParameters.WorkArea.Bottom - Height - 18; };
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) }; timer.Tick += (_, _) => { timer.Stop(); Close(); }; timer.Start();
    }
}
