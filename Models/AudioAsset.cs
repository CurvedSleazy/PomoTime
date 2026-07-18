namespace PomoTime.Models;
public sealed record AudioAsset(long Id, string Name, string Extension, byte[] Content) { public override string ToString() => Name; }
