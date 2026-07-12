using Microsoft.Data.Sqlite;
using System.IO;

namespace PomoTime;

public sealed class SessionDatabase : IDisposable
{
    private readonly SqliteConnection connection;

    public SessionDatabase()
    {
        string directory = Directory.Exists("src")
            ? Path.GetFullPath(Path.Combine("src", "data"))
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PomoTime");
        Directory.CreateDirectory(directory);
        connection = new SqliteConnection($"Data Source={Path.Combine(directory, "pomotime.db")}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS timer_sessions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                started_at TEXT NOT NULL,
                ended_at TEXT NOT NULL,
                planned_seconds INTEGER NOT NULL,
                focused_seconds INTEGER NOT NULL,
                completed INTEGER NOT NULL CHECK (completed IN (0, 1))
            );
            CREATE TABLE IF NOT EXISTS settings (
                setting_key TEXT PRIMARY KEY,
                setting_value TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public void LogSession(DateTimeOffset started, int plannedSeconds, int focusedSeconds, bool completed)
    {
        if (focusedSeconds <= 0) return;
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO timer_sessions(started_at,ended_at,planned_seconds,focused_seconds,completed) VALUES($start,$end,$planned,$focused,$completed)";
        command.Parameters.AddWithValue("$start", started.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$end", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$planned", plannedSeconds);
        command.Parameters.AddWithValue("$focused", focusedSeconds);
        command.Parameters.AddWithValue("$completed", completed ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public SessionStats StatsSince(DateTimeOffset? since)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(SUM(focused_seconds),0), COALESCE(SUM(completed),0) FROM timer_sessions" + (since is null ? "" : " WHERE ended_at >= $since");
        if (since is not null) command.Parameters.AddWithValue("$since", since.Value.UtcDateTime.ToString("O"));
        using var reader = command.ExecuteReader(); reader.Read();
        return new SessionStats(reader.GetInt64(0), reader.GetInt32(1));
    }

    public string GetSetting(string key)
    {
        using var command = connection.CreateCommand(); command.CommandText = "SELECT setting_value FROM settings WHERE setting_key=$key";
        command.Parameters.AddWithValue("$key", key); return command.ExecuteScalar() as string ?? "";
    }

    public void SaveSetting(string key, string value)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO settings(setting_key,setting_value) VALUES($key,$value) ON CONFLICT(setting_key) DO UPDATE SET setting_value=excluded.setting_value";
        command.Parameters.AddWithValue("$key", key); command.Parameters.AddWithValue("$value", value); command.ExecuteNonQuery();
    }

    public void Dispose() => connection.Dispose();
}

public readonly record struct SessionStats(long Seconds, int Completed)
{
    public string Duration => Seconds >= 3600 ? $"{Seconds / 3600}h {(Seconds % 3600) / 60:00}m" : $"{Seconds / 60}m";
}
