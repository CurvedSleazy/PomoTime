using Microsoft.Win32;
using PomoTime.Data;
using PomoTime.Models;
using PomoTime.Services;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PomoTime.UI.Views;

public partial class MainWindow : Window
{
    private readonly SessionDatabase database = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly AlarmService alarmService = new();
    private int totalSeconds = 25 * 60;
    private int remainingSeconds = 25 * 60;
    private DateTimeOffset? sessionStarted;
    private string alarmPath = "";

    public MainWindow()
    {
        InitializeComponent();
        timer.Tick += Timer_Tick;
        alarmPath = database.GetSetting("alarm_path");
        if (File.Exists(alarmPath)) AlarmName.Text = Path.GetFileName(alarmPath);
        UpdateClock(); RefreshStats();
    }

    private void StartPause_Click(object sender, RoutedEventArgs e)
    {
        alarmService.Stop();
        if (timer.IsEnabled) { timer.Stop(); StartPauseButton.Content = "Resume"; return; }
        if (sessionStarted is null)
        {
            totalSeconds = ReadMinutes() * 60; remainingSeconds = totalSeconds; sessionStarted = DateTimeOffset.UtcNow;
            MinutesInput.IsEnabled = false;
        }
        timer.Start(); StartPauseButton.Content = "Pause"; UpdateClock();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        remainingSeconds = Math.Max(0, remainingSeconds - 1); UpdateClock();
        if (remainingSeconds > 0) return;
        timer.Stop();
        database.LogSession(sessionStarted!.Value, totalSeconds, totalSeconds, true);
        sessionStarted = null; MinutesInput.IsEnabled = true; StartPauseButton.Content = "Start again";
        RefreshStats(); PlayAlarm();
    }

    private void Reset_Click(object sender, RoutedEventArgs e) => ResetTimer(true);

    private void ResetTimer(bool record)
    {
        if (record) RecordInterruptedSession();
        timer.Stop(); alarmService.Stop(); sessionStarted = null;
        totalSeconds = ReadMinutes() * 60; remainingSeconds = totalSeconds;
        MinutesInput.IsEnabled = true; StartPauseButton.Content = "Start"; UpdateClock();
    }

    private void MinutesInput_LostFocus(object sender, RoutedEventArgs e) { if (sessionStarted is null) ResetTimer(false); }

    private int ReadMinutes()
    {
        if (!int.TryParse(MinutesInput.Text, out int value)) value = 25;
        value = Math.Clamp(value, 1, 180); MinutesInput.Text = value.ToString(); return value;
    }

    private void ChooseAlarm_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Choose an alarm sound", Filter = "Audio files|*.wav;*.mp3|WAV files|*.wav|MP3 files|*.mp3" };
        if (dialog.ShowDialog(this) != true) return;
        alarmPath = dialog.FileName; AlarmName.Text = Path.GetFileName(alarmPath); database.SaveSetting("alarm_path", alarmPath);
    }

    private void Calendar_Click(object sender, RoutedEventArgs e)
    {
        OpenCalendar();
    }

    private void OpenCalendar()
    {
        ActivityCalendar.SetActivityDates(database.ActivityDates());
        CalendarOverlay.Visibility = Visibility.Visible;
        CalendarButton.Content = "\u00D7";
        CalendarButton.ToolTip = "Close activity calendar";
    }

    private void CloseCalendar()
    {
        CalendarOverlay.Visibility = Visibility.Collapsed;
        CalendarButton.Content = "\u25A6";
        CalendarButton.ToolTip = "Show activity calendar";
    }

    private void CloseCalendar_Click(object sender, RoutedEventArgs e) => CloseCalendar();

    private void CalendarOverlay_MouseDown(object sender, MouseButtonEventArgs e) => CloseCalendar();

    private void CalendarCard_MouseDown(object sender, MouseButtonEventArgs e) => e.Handled = true;

    private void PlayAlarm() => alarmService.Play(alarmPath);

    private void UpdateClock() { Clock.TotalSeconds = totalSeconds; Clock.RemainingSeconds = remainingSeconds; }

    private void RefreshStats()
    {
        var now = DateTimeOffset.Now;
        var todayStart = new DateTimeOffset(now.Date, now.Offset);
        int daysSinceMonday = ((int)now.DayOfWeek + 6) % 7;
        SetStats(TodayStats, database.StatsSince(todayStart));
        SetStats(WeekStats, database.StatsSince(todayStart.AddDays(-daysSinceMonday)));
        SetStats(AllTimeStats, database.StatsSince(null));
        ActivityCalendar.SetActivityDates(database.ActivityDates());
    }

    private static void SetStats(System.Windows.Controls.TextBlock target, SessionStats stats) =>
        target.Text = $"{stats.Duration}\n{stats.Completed} completed";

    private void RecordInterruptedSession()
    {
        if (sessionStarted is null) return;
        database.LogSession(sessionStarted.Value, totalSeconds, totalSeconds - remainingSeconds, false); RefreshStats();
    }

    protected override void OnClosed(EventArgs e)
    {
        RecordInterruptedSession(); timer.Stop(); alarmService.Dispose(); database.Dispose(); base.OnClosed(e);
    }
}
