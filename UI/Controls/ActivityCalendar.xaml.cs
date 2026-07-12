using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PomoTime.UI.Controls;

public partial class ActivityCalendar : UserControl
{
    private DateOnly displayedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private HashSet<DateOnly> activityDates = [];
    private DateOnly? selectedDate;

    public ActivityCalendar()
    {
        InitializeComponent();
        RenderMonth();
    }

    public void SetActivityDates(IEnumerable<DateOnly> dates)
    {
        activityDates = dates.ToHashSet();
        RenderMonth();
    }

    private void PreviousMonth_Click(object sender, RoutedEventArgs e)
    {
        displayedMonth = displayedMonth.AddMonths(-1); RenderMonth();
    }

    private void NextMonth_Click(object sender, RoutedEventArgs e)
    {
        displayedMonth = displayedMonth.AddMonths(1); RenderMonth();
    }

    private void Today_Click(object sender, RoutedEventArgs e)
    {
        displayedMonth = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1); RenderMonth();
    }

    private void SelectDay_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DateOnly date }) { selectedDate = date; RenderMonth(); }
    }

    private void RenderMonth()
    {
        if (!IsInitialized) return;
        MonthTitle.Content = displayedMonth.ToDateTime(TimeOnly.MinValue).ToString("MMMM yyyy", CultureInfo.CurrentCulture);
        DaysGrid.Children.Clear();
        int mondayOffset = ((int)displayedMonth.DayOfWeek + 6) % 7;
        DateOnly firstVisible = displayedMonth.AddDays(-mondayOffset);
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        for (int index = 0; index < 42; index++)
        {
            DateOnly date = firstVisible.AddDays(index);
            bool isToday = date == today;
            bool hasActivity = activityDates.Contains(date);
            bool inMonth = date.Month == displayedMonth.Month;
            bool isSelected = selectedDate == date;
            var content = new Grid();
            content.Children.Add(new TextBlock
            {
                Text = date.Day.ToString(), HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(
                    inMonth ? Color.FromRgb(240, 242, 247) : Color.FromRgb(103, 109, 123))
            });
            if (hasActivity)
            {
                content.Children.Add(new TextBlock
                {
                    Text = "\u2713", FontSize = 10, FontWeight = FontWeights.Bold,
                    Foreground = (Brush)FindResource("AccentBrush"),
                    HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 3, 1)
                });
            }
            var button = new Button
            {
                Content = content, Tag = date, Margin = new Thickness(2), Padding = new Thickness(3),
                MinHeight = 38, BorderThickness = new Thickness(isToday ? 2 : 0),
                BorderBrush = (Brush)FindResource("AccentBrush"),
                Background = isToday ? new SolidColorBrush(Color.FromRgb(68, 46, 55))
                    : isSelected ? new SolidColorBrush(Color.FromRgb(58, 63, 76)) : Brushes.Transparent
            };
            button.Click += SelectDay_Click;
            DaysGrid.Children.Add(button);
        }
    }
}
