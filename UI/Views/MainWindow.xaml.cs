using PomoTime.Data;
using PomoTime.Models;
using PomoTime.Services;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PomoTime.UI.Views;

public partial class MainWindow : Window
{
    private readonly SessionDatabase database = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly AlarmService alarmService = new();
    private int totalSeconds = 25 * 60, remainingSeconds = 25 * 60;
    private DateTimeOffset? sessionStarted;
    private long? sessionTagId;
    private bool isBreak;

    public MainWindow()
    {
        InitializeComponent(); timer.Tick += Timer_Tick;
        SettingsView.Initialize(database); SettingsView.CloseRequested += (_, _) => CloseSettings(); SettingsView.TagsChanged += (_, _) => RefreshTags();
        SettingsView.ThemeChanged += (_, _) => Clock.InvalidateVisual();
        RefreshTags(); UpdateClock(); RefreshStats();
    }

    private void StartPause_Click(object sender, RoutedEventArgs e)
    {
        alarmService.Stop();
        if (timer.IsEnabled) { timer.Stop(); StartPauseButton.Content="Resume"; return; }
        if (isBreak) { timer.Start(); StartPauseButton.Content="Pause break"; return; }
        if (sessionStarted is null)
        {
            isBreak=false; totalSeconds=ReadMinutes()*60; remainingSeconds=totalSeconds; sessionStarted=DateTimeOffset.UtcNow;
            sessionTagId=(TagSelector.SelectedItem as FocusTag)?.Id; MinutesInput.IsEnabled=false; TagSelector.IsEnabled=false; SessionModeLabel.Text="FOCUS";
        }
        timer.Start(); StartPauseButton.Content="Pause"; UpdateClock();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        remainingSeconds=Math.Max(0,remainingSeconds-1); UpdateClock(); if(remainingSeconds>0) return; timer.Stop();
        if (isBreak) { FinishBreak(); return; }
        database.LogSession(sessionStarted!.Value,totalSeconds,totalSeconds,true,sessionTagId); sessionStarted=null;
        RefreshStats(); alarmService.Play(database.GetAlarm()); Notify("Focus complete", "Nice work. Your focus session has been recorded.");
        if(database.GetBool("auto_break",true)) StartBreak(); else ResetToFocus("Start again");
    }

    private void StartBreak()
    {
        isBreak=true; totalSeconds=Math.Clamp(database.GetInt("break_minutes",5),1,60)*60; remainingSeconds=totalSeconds;
        SessionModeLabel.Text="BREAK"; StartPauseButton.Content="Pause break"; timer.Start(); UpdateClock();
    }
    private void FinishBreak() { alarmService.Play(database.GetAlarm()); Notify("Break complete", "Ready for another focused session?"); ResetToFocus("Start focus"); }
    private void ResetToFocus(string buttonText)
    {
        timer.Stop(); isBreak=false; sessionStarted=null; totalSeconds=ReadMinutes()*60; remainingSeconds=totalSeconds;
        MinutesInput.IsEnabled=true; TagSelector.IsEnabled=true; SessionModeLabel.Text="FOCUS"; StartPauseButton.Content=buttonText; UpdateClock();
    }
    private void Reset_Click(object sender, RoutedEventArgs e) { RecordInterruptedSession(); alarmService.Stop(); ResetToFocus("Start"); }
    private void MinutesInput_LostFocus(object sender, RoutedEventArgs e) { if(sessionStarted is null&&!isBreak) ResetToFocus("Start"); }
    private int ReadMinutes() { int value=int.TryParse(MinutesInput.Text,out int parsed)?Math.Clamp(parsed,1,180):25; MinutesInput.Text=value.ToString(); return value; }

    private void RefreshTags()
    {
        long? selected=(TagSelector.SelectedItem as FocusTag)?.Id; var tags=database.GetTags(); TagSelector.ItemsSource=tags;
        TagSelector.SelectedItem=tags.FirstOrDefault(t=>t.Id==selected) ?? tags.FirstOrDefault();
    }
    private void Notify(string title,string message) { if(database.GetBool("notifications",true)) NotificationService.Show(title,message); }

    private void Settings_Click(object sender,RoutedEventArgs e) { SettingsView.Initialize(database); SettingsOverlay.Visibility=Visibility.Visible; }
    private void CloseSettings() { SettingsOverlay.Visibility=Visibility.Collapsed; RefreshTags(); }
    private void SettingsOverlay_MouseDown(object sender,MouseButtonEventArgs e)=>CloseSettings();
    private void OverlayCard_MouseDown(object sender,MouseButtonEventArgs e)=>e.Handled=true;

    private void Calendar_Click(object sender,RoutedEventArgs e) { ActivityCalendar.SetActivityDates(database.ActivityDates()); CalendarOverlay.Visibility=Visibility.Visible; CalendarButton.Content="\u00D7"; }
    private void CloseCalendar_Click(object sender,RoutedEventArgs e)=>CloseCalendar();
    private void CalendarOverlay_MouseDown(object sender,MouseButtonEventArgs e)=>CloseCalendar();
    private void CloseCalendar() { CalendarOverlay.Visibility=Visibility.Collapsed; CalendarButton.Content="\u25A6"; }

    private void UpdateClock() { Clock.TotalSeconds=totalSeconds; Clock.RemainingSeconds=remainingSeconds; }
    private void RefreshStats()
    {
        var now=DateTimeOffset.Now; var today=new DateTimeOffset(now.Date,now.Offset); int days=((int)now.DayOfWeek+6)%7;
        SetStats(TodayStats,database.StatsSince(today)); SetStats(WeekStats,database.StatsSince(today.AddDays(-days))); SetStats(AllTimeStats,database.StatsSince(null));
        ActivityCalendar.SetActivityDates(database.ActivityDates());
    }
    private static void SetStats(System.Windows.Controls.TextBlock target,SessionStats stats)=>target.Text=$"{stats.Duration}\n{stats.Completed} completed";
    private void RecordInterruptedSession()
    {
        if(sessionStarted is null||isBreak)return; database.LogSession(sessionStarted.Value,totalSeconds,totalSeconds-remainingSeconds,false,sessionTagId); sessionStarted=null; RefreshStats();
    }
    protected override void OnClosed(EventArgs e) { RecordInterruptedSession(); timer.Stop(); SettingsView.Dispose(); alarmService.Dispose(); database.Dispose(); base.OnClosed(e); }
}
