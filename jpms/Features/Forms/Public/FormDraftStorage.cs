using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Jewel.JPMS.Features.Forms.Public;

/// <summary>
/// Keeps a form's draft in the browser until it is sent. A phone that leaves the page to take a
/// photograph may reload it on the way back, and a stranger who loses twenty answers to that does
/// not fill them in twice; the draft goes the moment the form is sent. A shared phone is the risk,
/// so the sensitive answers are never kept, a month-old draft is not picked up, and the key holds
/// a fingerprint of the link rather than the link. A private window or a full store simply keeps
/// nothing — the form still works, it just starts again.
/// </summary>
public sealed class FormDraftStorage
{
    private const string KeyPrefix = "jpms.formDraft";
    private const string OpenLink = "open";
    private const int FingerprintLength = 16;
    private static readonly TimeSpan KeptFor = TimeSpan.FromDays(30);
    private readonly IJSRuntime js;

    public FormDraftStorage(IJSRuntime js)
    {
        this.js = js;
    }

    public static string KeyFor(string slug, string? token) =>
        $"{KeyPrefix}.{slug}.{(string.IsNullOrEmpty(token) ? OpenLink : FingerprintOf(token))}";

    private static string FingerprintOf(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))[..FingerprintLength];

    public async Task<FormDraftRecord?> ReadAsync(string key)
    {
        try
        {
            var stored = await js.InvokeAsync<string?>("localStorage.getItem", key);
            var record = string.IsNullOrEmpty(stored) ? null : JsonSerializer.Deserialize<FormDraftRecord>(stored);
            return record is { } kept && kept.KeptAt > DateTimeOffset.UtcNow - KeptFor ? kept : null;
        }
        catch (Exception unreadable) when (unreadable is JSException or JsonException or NotSupportedException) { return null; }
    }

    public async Task KeepAsync(string key, FormDraft draft, FormDefinition form)
    {
        try { await js.InvokeVoidAsync("localStorage.setItem", key, JsonSerializer.Serialize(draft.ToRecord(form, DateTimeOffset.UtcNow))); }
        catch (JSException) { }
    }

    public async Task ForgetAsync(string key)
    {
        try { await js.InvokeVoidAsync("localStorage.removeItem", key); }
        catch (JSException) { }
    }
}
