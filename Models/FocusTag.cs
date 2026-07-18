namespace PomoTime.Models;
public sealed record FocusTag(long Id, string Name) { public override string ToString() => Name; }
