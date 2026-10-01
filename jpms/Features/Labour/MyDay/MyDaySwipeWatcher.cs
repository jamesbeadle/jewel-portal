namespace Jewel.JPMS.Features.Labour.MyDay;

/// <summary>Hears a horizontal swipe on one element (my-day-swipe.js) and tells its owner which
/// way: the week panel pages with it. Disposed with the panel so the listeners go too.</summary>
public sealed class MyDaySwipeWatcher : IAsyncDisposable
{
    private const string Watch = "jpmsSwipe.watch";
    private const string Unwatch = "jpmsSwipe.unwatch";
    private const string Left = "left";

    private readonly IJSRuntime js;
    private readonly Func<bool, Task> onSwiped;
    private DotNetObjectReference<MyDaySwipeWatcher>? self;

    public MyDaySwipeWatcher(IJSRuntime js, Func<bool, Task> onSwiped) { this.js = js; this.onSwiped = onSwiped; }

    public async Task WatchAsync(ElementReference element)
    {
        self ??= DotNetObjectReference.Create(this);
        try { await js.InvokeVoidAsync(Watch, element, self); }
        catch (JSException) { }
    }

    [JSInvokable]
    public Task OnSwiped(string direction) => onSwiped(direction == Left);

    public async ValueTask DisposeAsync()
    {
        if (self is null) return;
        try { await js.InvokeVoidAsync(Unwatch, self); }
        catch (Exception) { }
        self.Dispose();
    }
}
