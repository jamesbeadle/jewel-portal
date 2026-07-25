namespace Jewel.JPMS.Services;

/// <summary>
/// Small helpers for the panel-level loading rule: a panel that reads several stores holds its
/// whole body behind one indicator until <em>every</em> one of them has landed, rather than
/// revealing itself in pieces (or, worse, revealing zeroes that quietly change a moment later).
///
/// Written as "is it still loading?" because that is what the components take:
/// <c>&lt;Panel IsLoading="@LoadState.UntilAll(Directory.IsLoaded, Requests.IsLoaded)"&gt;</c>
/// </summary>
public static class LoadState
{
    /// <summary>True while any of the given "has loaded" flags is still false.</summary>
    public static bool UntilAll(params bool[] loaded) => loaded.Any(isLoaded => !isLoaded);

    /// <summary>True while any of the given read-model snapshots is still null. Read models expose
    /// their data as a nullable Current, so null is the canonical "no fetch has landed yet".
    /// Named apart from <see cref="UntilAll(bool[])"/> rather than overloaded: two params arrays
    /// that differ only by element type are an overload-resolution trap waiting to be sprung by a
    /// caller passing a mix.</summary>
    public static bool UntilAllPresent(params object?[] snapshots) => snapshots.Any(snapshot => snapshot is null);
}
