namespace Jewel.JPMS.Components;

/// <summary>One entry in a TabRow (a link) or a FilterChips row (a local choice).</summary>
public sealed record TabItem(string Key, string Label, string Href = "", int? Count = null, string? Title = null);
