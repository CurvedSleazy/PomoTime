using Microsoft.Data.Sqlite;
using PomoTime.Models;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace PomoTime.Data;

public sealed class SessionDatabase : IDisposable
{
    private readonly SqliteConnection connection;

    public SessionDatabase()
    {
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PomoTime");
        Directory.CreateDirectory(directory);
        connection = new SqliteConnection($"Data Source={Path.Combine(directory, "pomotime.db")}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS timer_sessions (
                id INTEGER PRIMARY KEY AUTOINCREMENT, started_at TEXT NOT NULL, ended_at TEXT NOT NULL,
                planned_seconds INTEGER NOT NULL, focused_seconds INTEGER NOT NULL,
                completed INTEGER NOT NULL CHECK (completed IN (0, 1)), tag_id INTEGER NULL
            );
            CREATE TABLE IF NOT EXISTS settings (setting_key TEXT PRIMARY KEY, setting_value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS tags (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL UNIQUE COLLATE NOCASE);
            CREATE TABLE IF NOT EXISTS audio_assets (
                id INTEGER PRIMARY KEY AUTOINCREMENT, kind TEXT NOT NULL, name TEXT NOT NULL,
                extension TEXT NOT NULL, content BLOB NOT NULL, imported_at TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
        EnsureTagColumn();
        SeedTags();
        MigrateLegacyDevelopmentDatabase();
    }

    private void EnsureTagColumn()
    {
        using var check = connection.CreateCommand(); check.CommandText = "PRAGMA table_info(timer_sessions)";
        using var reader = check.ExecuteReader(); bool exists = false;
        while (reader.Read()) if (reader.GetString(1) == "tag_id") exists = true;
        reader.Close();
        if (!exists) { using var alter = connection.CreateCommand(); alter.CommandText = "ALTER TABLE timer_sessions ADD COLUMN tag_id INTEGER NULL"; alter.ExecuteNonQuery(); }
    }

    private void SeedTags()
    {
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO tags(name) VALUES('Study'),('Work')"; command.ExecuteNonQuery();
    }

    private void MigrateLegacyDevelopmentDatabase()
    {
        string legacy = Path.GetFullPath(Path.Combine("src", "data", "pomotime.db"));
        if (!File.Exists(legacy)) return;
        using var command = connection.CreateCommand();
        command.CommandText = """
            ATTACH DATABASE $legacy AS legacy;
            INSERT INTO main.timer_sessions(started_at,ended_at,planned_seconds,focused_seconds,completed)
            SELECT s.started_at,s.ended_at,s.planned_seconds,s.focused_seconds,s.completed FROM legacy.timer_sessions s
            WHERE NOT EXISTS (SELECT 1 FROM main.timer_sessions d WHERE d.started_at=s.started_at AND d.ended_at=s.ended_at);
            INSERT INTO main.settings(setting_key,setting_value) SELECT setting_key,setting_value FROM legacy.settings WHERE true
            ON CONFLICT(setting_key) DO NOTHING;
            DETACH DATABASE legacy;
            """;
        command.Parameters.AddWithValue("$legacy", legacy); command.ExecuteNonQuery();
    }

    public void LogSession(DateTimeOffset started, int plannedSeconds, int focusedSeconds, bool completed, long? tagId)
    {
        if (focusedSeconds <= 0) return;
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO timer_sessions(started_at,ended_at,planned_seconds,focused_seconds,completed,tag_id) VALUES($start,$end,$planned,$focused,$completed,$tag)";
        command.Parameters.AddWithValue("$start", started.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$end", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$planned", plannedSeconds); command.Parameters.AddWithValue("$focused", focusedSeconds);
        command.Parameters.AddWithValue("$completed", completed ? 1 : 0); command.Parameters.AddWithValue("$tag", tagId is null ? DBNull.Value : tagId.Value);
        command.ExecuteNonQuery();
    }

    public SessionStats StatsSince(DateTimeOffset? since)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(SUM(focused_seconds),0),COALESCE(SUM(completed),0) FROM timer_sessions" + (since is null ? "" : " WHERE ended_at >= $since");
        if (since is not null) command.Parameters.AddWithValue("$since", since.Value.UtcDateTime.ToString("O"));
        using var reader = command.ExecuteReader(); reader.Read(); return new(reader.GetInt64(0), reader.GetInt32(1));
    }

    public HashSet<DateOnly> ActivityDates()
    {
        var dates = new HashSet<DateOnly>(); using var command = connection.CreateCommand();
        command.CommandText = "SELECT ended_at FROM timer_sessions WHERE focused_seconds > 0"; using var reader = command.ExecuteReader();
        while (reader.Read()) if (DateTimeOffset.TryParse(reader.GetString(0), out var timestamp)) dates.Add(DateOnly.FromDateTime(timestamp.ToLocalTime().DateTime));
        return dates;
    }

    public IReadOnlyList<FocusTag> GetTags()
    {
        var tags = new List<FocusTag>(); using var command = connection.CreateCommand(); command.CommandText = "SELECT id,name FROM tags ORDER BY name";
        using var reader = command.ExecuteReader(); while (reader.Read()) tags.Add(new(reader.GetInt64(0), reader.GetString(1))); return tags;
    }

    public void AddTag(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return; using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO tags(name) VALUES($name)"; command.Parameters.AddWithValue("$name", name.Trim()); command.ExecuteNonQuery();
    }

    public void DeleteTag(long id) { using var command = connection.CreateCommand(); command.CommandText = "DELETE FROM tags WHERE id=$id"; command.Parameters.AddWithValue("$id", id); command.ExecuteNonQuery(); }

    public void SaveAudio(string kind, string name, string extension, byte[] content, bool replaceKind)
    {
        using var transaction = connection.BeginTransaction();
        if (replaceKind) { using var delete = connection.CreateCommand(); delete.Transaction = transaction; delete.CommandText = "DELETE FROM audio_assets WHERE kind=$kind"; delete.Parameters.AddWithValue("$kind", kind); delete.ExecuteNonQuery(); }
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "INSERT INTO audio_assets(kind,name,extension,content,imported_at) VALUES($kind,$name,$extension,$content,$at)";
        command.Parameters.AddWithValue("$kind", kind); command.Parameters.AddWithValue("$name", name); command.Parameters.AddWithValue("$extension", extension);
        command.Parameters.AddWithValue("$content", content); command.Parameters.AddWithValue("$at", DateTime.UtcNow.ToString("O")); command.ExecuteNonQuery(); transaction.Commit();
    }

    public AudioAsset? GetAlarm() => GetAudioAssets("alarm").FirstOrDefault();
    public IReadOnlyList<AudioAsset> GetPlaylist() => GetAudioAssets("playlist");
    private List<AudioAsset> GetAudioAssets(string kind)
    {
        var assets = new List<AudioAsset>(); using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,name,extension,content FROM audio_assets WHERE kind=$kind ORDER BY imported_at"; command.Parameters.AddWithValue("$kind", kind);
        using var reader = command.ExecuteReader(); while (reader.Read()) assets.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), (byte[])reader[3])); return assets;
    }
    public void DeleteAudio(long id) { using var command = connection.CreateCommand(); command.CommandText = "DELETE FROM audio_assets WHERE id=$id"; command.Parameters.AddWithValue("$id", id); command.ExecuteNonQuery(); }

    public string GetSetting(string key, string fallback = "")
    {
        using var command = connection.CreateCommand(); command.CommandText = "SELECT setting_value FROM settings WHERE setting_key=$key";
        command.Parameters.AddWithValue("$key", key); return command.ExecuteScalar() as string ?? fallback;
    }
    public bool GetBool(string key, bool fallback = false) => bool.TryParse(GetSetting(key), out bool value) ? value : fallback;
    public int GetInt(string key, int fallback) => int.TryParse(GetSetting(key), out int value) ? value : fallback;
    public void SaveSetting(string key, string value)
    {
        using var command = connection.CreateCommand(); command.CommandText = "INSERT INTO settings(setting_key,setting_value) VALUES($key,$value) ON CONFLICT(setting_key) DO UPDATE SET setting_value=excluded.setting_value";
        command.Parameters.AddWithValue("$key", key); command.Parameters.AddWithValue("$value", value); command.ExecuteNonQuery();
    }

    public void Export(string path, bool json)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT s.started_at,s.ended_at,s.planned_seconds,s.focused_seconds,s.completed,COALESCE(t.name,'') FROM timer_sessions s LEFT JOIN tags t ON t.id=s.tag_id ORDER BY s.started_at";
        using var reader = command.ExecuteReader(); var rows = new List<object>(); var csv = new StringBuilder("started_at,ended_at,planned_seconds,focused_seconds,completed,tag\r\n");
        while (reader.Read())
        {
            var row = new { started_at=reader.GetString(0), ended_at=reader.GetString(1), planned_seconds=reader.GetInt32(2), focused_seconds=reader.GetInt32(3), completed=reader.GetInt32(4)==1, tag=reader.GetString(5) };
            rows.Add(row); csv.AppendLine(string.Join(',', Csv(row.started_at), Csv(row.ended_at), row.planned_seconds, row.focused_seconds, row.completed, Csv(row.tag)));
        }
        File.WriteAllText(path, json ? JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented=true }) : csv.ToString());
    }
    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    public void Dispose() => connection.Dispose();
}
