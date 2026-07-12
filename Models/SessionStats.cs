namespace PomoTime.Models;

public readonly record struct SessionStats(long Seconds, int Completed)
{
    public string Duration => Seconds >= 3600
        ? $"{Seconds / 3600}h {(Seconds % 3600) / 60:00}m"
        : $"{Seconds / 60}m";
}
