using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace PomoTime.UI.Controls;

public sealed class TimerRing : FrameworkElement
{
    public static readonly DependencyProperty RemainingSecondsProperty = DependencyProperty.Register(
        nameof(RemainingSeconds), typeof(int), typeof(TimerRing), new FrameworkPropertyMetadata(1500, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty TotalSecondsProperty = DependencyProperty.Register(
        nameof(TotalSeconds), typeof(int), typeof(TimerRing), new FrameworkPropertyMetadata(1500, FrameworkPropertyMetadataOptions.AffectsRender));

    public int RemainingSeconds { get => (int)GetValue(RemainingSecondsProperty); set => SetValue(RemainingSecondsProperty, value); }
    public int TotalSeconds { get => (int)GetValue(TotalSecondsProperty); set => SetValue(TotalSecondsProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double size = Math.Min(ActualWidth, ActualHeight) - 28;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        double radius = size / 2;
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromRgb(48, 52, 63)), 13), center, radius, radius);
        double progress = Math.Clamp(RemainingSeconds / (double)Math.Max(1, TotalSeconds), 0, 0.999999);
        if (RemainingSeconds >= TotalSeconds) progress = 0.999999;
        DrawArc(dc, center, radius, progress, new Pen(new SolidColorBrush(Color.FromRgb(255, 105, 120)), 13) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round });
        string time = $"{RemainingSeconds / 60:00}:{RemainingSeconds % 60:00}";
        var text = new FormattedText(time, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"), 54, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2));
    }

    private static void DrawArc(DrawingContext dc, Point center, double radius, double progress, Pen pen)
    {
        double start = -Math.PI / 2, end = start + Math.PI * 2 * progress;
        var startPoint = new Point(center.X + radius * Math.Cos(start), center.Y + radius * Math.Sin(start));
        var endPoint = new Point(center.X + radius * Math.Cos(end), center.Y + radius * Math.Sin(end));
        var figure = new PathFigure { StartPoint = startPoint, IsClosed = false };
        figure.Segments.Add(new ArcSegment(endPoint, new Size(radius, radius), 0, progress > .5, SweepDirection.Clockwise, true));
        dc.DrawGeometry(null, pen, new PathGeometry([figure]));
    }
}
