using Microsoft.Win32;
using PomoTime.Data;
using PomoTime.Models;
using PomoTime.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PomoTime.UI.Controls;

public partial class SettingsView : UserControl, IDisposable
{
    private SessionDatabase? database;
    private readonly AlarmService ambientPlayer = new();
    public event EventHandler? CloseRequested;
    public event EventHandler? TagsChanged;
    public event EventHandler? ThemeChanged;

    public SettingsView() => InitializeComponent();

    public void Initialize(SessionDatabase db)
    {
        database = db;
        AutoBreakCheck.IsChecked = db.GetBool("auto_break", true);
        NotificationsCheck.IsChecked = db.GetBool("notifications", true);
        BreakMinutesInput.Text = db.GetInt("break_minutes", 5).ToString();
        string accent = db.GetSetting("accent", "#FF6978");
        if (ColorConverter.ConvertFromString(accent) is Color color) { RedSlider.Value=color.R; GreenSlider.Value=color.G; BlueSlider.Value=color.B; }
        ThemeService.Apply(db.GetSetting("theme", "Dark"), color);
        RefreshTags(); RefreshAudio();
    }

    private void DarkTheme_Click(object sender, RoutedEventArgs e) => SaveTheme("Dark", CurrentColor());
    private void LightTheme_Click(object sender, RoutedEventArgs e) => SaveTheme("Light", CurrentColor());
    private void CustomTheme_Click(object sender, RoutedEventArgs e) => SaveTheme("Custom", CurrentColor());
    private Color CurrentColor() => Color.FromRgb((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value);
    private void SaveTheme(string mode, Color color) { database!.SaveSetting("theme", mode); database.SaveSetting("accent", color.ToString()); ThemeService.Apply(mode, color); ThemeChanged?.Invoke(this, EventArgs.Empty); }

    private void SettingChanged(object sender, RoutedEventArgs e)
    {
        database!.SaveSetting("auto_break", (AutoBreakCheck.IsChecked == true).ToString());
        database.SaveSetting("notifications", (NotificationsCheck.IsChecked == true).ToString());
    }
    private void BreakMinutes_LostFocus(object sender, RoutedEventArgs e)
    {
        int value = int.TryParse(BreakMinutesInput.Text, out int parsed) ? Math.Clamp(parsed, 1, 60) : 5;
        BreakMinutesInput.Text=value.ToString(); database!.SaveSetting("break_minutes", value.ToString());
    }

    private void ImportAlarm_Click(object sender, RoutedEventArgs e)
    {
        var dialog = AudioDialog(false); if (dialog.ShowDialog() != true) return;
        var file = new FileInfo(dialog.FileName); database!.SaveAudio("alarm", file.Name, file.Extension, File.ReadAllBytes(file.FullName), true); RefreshAudio();
    }

    private void ImportSongs_Click(object sender, RoutedEventArgs e)
    {
        var dialog = AudioDialog(true); if (dialog.ShowDialog() != true) return;
        foreach (string path in dialog.FileNames)
        {
            var file = new FileInfo(path);
            if (file.Length > 100 * 1024 * 1024) { MessageBox.Show($"{file.Name} exceeds the 100 MB per-song limit."); continue; }
            database!.SaveAudio("playlist", file.Name, file.Extension, File.ReadAllBytes(path), false);
        }
        RefreshAudio();
    }
    private static OpenFileDialog AudioDialog(bool multiple) => new() { Multiselect=multiple, Filter="Audio files|*.wav;*.mp3;*.wma;*.m4a|All files|*.*" };
    private void PlaySong_Click(object sender, RoutedEventArgs e) { if (Playlist.SelectedItem is AudioAsset asset) ambientPlayer.Play(asset, true); }
    private void StopSong_Click(object sender, RoutedEventArgs e) => ambientPlayer.Stop();
    private void RemoveSong_Click(object sender, RoutedEventArgs e) { if (Playlist.SelectedItem is AudioAsset asset) { ambientPlayer.Stop(); database!.DeleteAudio(asset.Id); RefreshAudio(); } }

    private void AddTag_Click(object sender, RoutedEventArgs e) { database!.AddTag(NewTagInput.Text); NewTagInput.Clear(); RefreshTags(); TagsChanged?.Invoke(this, EventArgs.Empty); }
    private void RemoveTag_Click(object sender, RoutedEventArgs e) { if (TagsList.SelectedItem is FocusTag tag) { database!.DeleteTag(tag.Id); RefreshTags(); TagsChanged?.Invoke(this, EventArgs.Empty); } }
    private void RefreshTags() { if (database is not null) TagsList.ItemsSource = database.GetTags(); }
    private void RefreshAudio() { if (database is null) return; AlarmName.Text = database.GetAlarm()?.Name ?? "Default system chime"; Playlist.ItemsSource = database.GetPlaylist(); }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { FileName=$"pomotime-export-{DateTime.Now:yyyy-MM-dd}", Filter="CSV file|*.csv|JSON file|*.json" };
        if (dialog.ShowDialog() == true) { database!.Export(dialog.FileName, dialog.FilterIndex == 2); MessageBox.Show("Your PomoTime data was exported successfully.", "PomoTime"); }
    }
    private void Close_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);
    public void Dispose() => ambientPlayer.Dispose();
}
