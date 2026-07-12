using System.IO;
using System.Media;
using System.Windows.Media;

namespace PomoTime.Services;

public sealed class AlarmService : IDisposable
{
    private readonly MediaPlayer player = new();

    public void Play(string audioPath)
    {
        Stop();
        if (!File.Exists(audioPath))
        {
            SystemSounds.Exclamation.Play();
            return;
        }

        try
        {
            player.Open(new Uri(audioPath));
            player.Position = TimeSpan.Zero;
            player.Play();
        }
        catch
        {
            SystemSounds.Exclamation.Play();
        }
    }

    public void Stop() => player.Stop();

    public void Dispose() => player.Close();
}
