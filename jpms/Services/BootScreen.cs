namespace Jewel.JPMS.Services;

/// <summary>
/// The app's side of the boot overlay in wwwroot/index.html (js/boot-screen.js): the jewel that
/// covers the window from the first byte of HTML until the app has something real to show.
/// </summary>
public static class BootScreen
{
    private static bool isDown;
    private static bool isHeldThroughRedirect;

    /// <summary>
    /// Takes the boot overlay down. Idempotent per document — the overlay exists once, so after
    /// the first successful call every later one (a page gate on each in-app navigation) is free.
    ///
    /// Best effort, never throws: the .NET assemblies and index.html are fetched separately, so a
    /// client can be running today's code against yesterday's shell — one that predates
    /// <c>jpmsBoot</c>, or a future one that renames it. A missing dismiss must not take the page
    /// render down with it (JPMS-4BF13E, 2026-09-08): the old shell kept its boot mark inside #app,
    /// which Blazor has already wiped, and the new shell's failsafe removes the overlay anyway.
    /// It is also called from the error boundary, where a second failure must not mask the first.
    /// </summary>
    public static async Task DismissAsync(IJSRuntime js)
    {
        if (isDown) return;
        isDown = true;
        isHeldThroughRedirect = false;
        try
        {
            await js.InvokeVoidAsync("jpmsBoot.dismiss");
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// A page that only bounces the visitor somewhere else (the sign-in door for someone already
    /// signed in) says so before it navigates, so the landing layout leaves the overlay up for
    /// the page that will actually draw. Consumed by the next <see cref="DismissUnlessHeldAsync"/>.
    /// </summary>
    public static void HoldThroughRedirect() => isHeldThroughRedirect = true;

    /// <summary>The landing layout's dismiss: down once its page has drawn a real screen, left
    /// up when that page is redirecting.</summary>
    public static Task DismissUnlessHeldAsync(IJSRuntime js)
    {
        if (!isHeldThroughRedirect) return DismissAsync(js);
        isHeldThroughRedirect = false;
        return Task.CompletedTask;
    }
}
