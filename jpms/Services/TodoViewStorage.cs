using Microsoft.JSInterop;

namespace Jewel.JPMS.Services;

/// <summary>
/// Remembers whether a user last viewed their to-dos as the status BOARD (the default — Open and
/// Done columns, cards dragged between them) or as the flat LIST (per browser, per user). One
/// preference for every to-do surface — the browser page, the project tab and the dashboard
/// panel — so the app doesn't flip idiom from one screen to the next. Stored value is "board" or
/// "list".
/// </summary>
public sealed class TodoViewStorage
{
    private const string BoardValue = "board";
    private const string ListValue = "list";

    private const string StorageKeyPrefix = "jpms.todoView";
    private const string GetItem = "localStorage.getItem";
    private const string SetItem = "localStorage.setItem";

    private readonly IJSRuntime js;

    public TodoViewStorage(IJSRuntime js)
    {
        this.js = js;
    }

    public async Task<bool> ReadBoardAsync(string email)
    {
        try { return await js.InvokeAsync<string?>(GetItem, StorageKeyFor(email)) != ListValue; }
        catch { return true; }
    }

    public async Task WriteAsync(string email, bool board)
    {
        try { await js.InvokeVoidAsync(SetItem, StorageKeyFor(email), board ? BoardValue : ListValue); }
        catch { }
    }

    private static string StorageKeyFor(string email) =>
        $"{StorageKeyPrefix}.{email.Trim().ToLowerInvariant()}";
}
