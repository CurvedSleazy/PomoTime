using PomoTime.Models;
using System.IO;
using System.Media;
using System.Windows.Media;

namespace PomoTime.Services;

public sealed class AlarmService : IDisposable
{
    private readonly MediaPlayer player = new();
    private string? temporaryFile;
    private bool loop;

    public AlarmService() => player.MediaEnded += (_, _) => { if (loop) { player.Position = TimeSpan.Zero; player.Play(); } };

    public void Play(AudioAsset? asset, bool repeat = false)
    {
        Stop();
        if (asset is null) { SystemSounds.Exclamation.Play(); return; }
        try
        {
            string directory = Path.Combine(Path.GetTempPath(), "PomoTime"); Directory.CreateDirectory(directory);
            temporaryFile = Path.Combine(directory, $"{asset.Id}-{Guid.NewGuid():N}{asset.Extension}");
            File.WriteAllBytes(temporaryFile, asset.Content); loop = repeat;
            player.Open(new Uri(temporaryFile)); player.Position = TimeSpan.Zero; player.Play();
        }
        catch { SystemSounds.Exclamation.Play(); }
    }

    public void Stop()
    {
        loop = false; player.Stop(); player.Close();
        if (temporaryFile is not null) { try { File.Delete(temporaryFile); } catch { } temporaryFile = null; }
    }

    public void Dispose() => Stop();
}
