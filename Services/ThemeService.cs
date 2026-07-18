using System.Windows;
using System.Windows.Media;

namespace PomoTime.Services;

public static class ThemeService
{
    public static void Apply(string mode, Color accent)
    {
        bool light = mode.Equals("Light", StringComparison.OrdinalIgnoreCase);
        Set("BackgroundBrush", light ? Color.FromRgb(244, 246, 250) : Color.FromRgb(24, 27, 34));
        Set("PanelBrush", light ? Colors.White : Color.FromRgb(32, 36, 45));
        Set("TextBrush", light ? Color.FromRgb(28, 31, 38) : Color.FromRgb(240, 242, 247));
        Set("MutedBrush", light ? Color.FromRgb(96, 104, 120) : Color.FromRgb(160, 166, 180));
        Set("ControlBrush", light ? Color.FromRgb(224, 228, 236) : Color.FromRgb(58, 63, 76));
        Set("AccentBrush", accent);
    }

    private static void Set(string key, Color color)
        => Application.Current.Resources[key] = new SolidColorBrush(color);
}
