using System.Text.Json;

namespace Jewel.JPMS.Features.Labour.MyDay;

/// <summary>Keeps a day's unsent draft in the phone's own storage, one per project per working
/// day, the way the "Viewing as" choice is kept: written as the worker types, read back when the
/// card opens, cleared once the day is saved. Storage that is missing or refused is silently
/// nothing — the form works without it, it just cannot survive a refresh.</summary>
public sealed class MyDayDraftStore
{
    private const string StorageKeyPrefix = "jpms.myday.draft";
    private const string DayFormat = "yyyy-MM-dd";
    private const string GetItem = "localStorage.getItem";
    private const string SetItem = "localStorage.setItem";
    private const string RemoveItem = "localStorage.removeItem";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IJSRuntime js;
    public MyDayDraftStore(IJSRuntime js) { this.js = js; }

    public async Task<MyDayDraft?> ReadAsync(string projectId)
    {
        try
        {
            var stored = await js.InvokeAsync<string?>(GetItem, KeyFor(projectId));
            return string.IsNullOrWhiteSpace(stored) ? null : JsonSerializer.Deserialize<MyDayDraft>(stored, Json);
        }
        catch { return null; }
    }

    public async Task WriteAsync(string projectId, MyDayDraft draft)
    {
        try { await js.InvokeVoidAsync(SetItem, KeyFor(projectId), JsonSerializer.Serialize(draft, Json)); }
        catch { }
    }

    public async Task ClearAsync(string projectId)
    {
        try { await js.InvokeVoidAsync(RemoveItem, KeyFor(projectId)); }
        catch { }
    }

    private static string KeyFor(string projectId) => $"{StorageKeyPrefix}.{projectId}.{DateTime.Today.ToString(DayFormat)}";
}
