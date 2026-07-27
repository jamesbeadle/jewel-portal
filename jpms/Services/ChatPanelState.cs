using Microsoft.JSInterop;

namespace Jewel.JPMS.Services;

/// <summary>
/// Open/closed state for the assistant chat panel, plus the one-off acknowledgement of its Claude
/// API cost warning. Scoped alongside the rest of the session state so the header launcher, the
/// mobile bubble and the panel itself all read the same flag rather than passing it down the tree.
/// The acknowledgement is remembered per browser, per user (same localStorage idiom as
/// <see cref="AllocationTabStorage"/> and <see cref="CurrentProjectService"/>): a director accepts
/// the cost notice once, not on every visit, but a new machine asks again on purpose.
/// </summary>
public sealed class ChatPanelState
{
    private const string StorageKeyPrefix = "jpms.chatCostAcknowledged";
    private const string GetItem = "localStorage.getItem";
    private const string SetItem = "localStorage.setItem";
    private const string AcknowledgedValue = "true";

    private readonly IJSRuntime js;
    private readonly AuthService auth;
    private string? loadedForKey;

    public ChatPanelState(IJSRuntime js, AuthService auth)
    {
        this.js = js;
        this.auth = auth;
    }

    public event Action? OnChange;

    public bool IsOpen { get; private set; }

    /// <summary>
    /// True once the user has accepted the "every message is billed" notice. Until then the panel
    /// shows the acknowledgement gate in place of the transcript and composer, so nobody can spend
    /// on the API by opening the panel out of curiosity.
    /// </summary>
    public bool HasAcknowledgedCost { get; private set; }

    /// <summary>
    /// True while a chat-aware dialog is being worked alongside the panel (raised and cleared by
    /// <see cref="AiTaskState"/>). The panel lifts itself above the modal overlay's z-50 while it is
    /// set, so the conversation stays live beside the form instead of being covered by it.
    /// </summary>
    public bool CoexistingModalOpen { get; private set; }

    public void Toggle()
    {
        IsOpen = !IsOpen;
        OnChange?.Invoke();
    }

    /// <summary>Opens the panel. Distinct from <see cref="Toggle"/> on purpose: a caller that means
    /// "show the assistant" must not close it because it happened to be open already.</summary>
    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        OnChange?.Invoke();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        OnChange?.Invoke();
    }

    public void SetCoexistence(bool value)
    {
        if (CoexistingModalOpen == value) return;
        CoexistingModalOpen = value;
        OnChange?.Invoke();
    }

    /// <summary>
    /// Reads the stored acknowledgement for the signed-in user, once per user. Deliberately a
    /// no-op while the user is unknown: the panel is instantiated by MainLayout on the very first
    /// render, before /api/auth/me has answered, and latching on a key derived from "anonymous"
    /// would mean writing the acceptance under one key and reading it under another — the gate
    /// would then reappear on every load. Callers re-invoke it when the session changes.
    /// </summary>
    public async Task EnsureAcknowledgementLoadedAsync()
    {
        if (auth.CurrentUser is null) return;
        var key = StorageKey;
        if (loadedForKey == key) return;
        loadedForKey = key;
        try
        {
            var stored = await js.InvokeAsync<string?>(GetItem, key);
            HasAcknowledgedCost = string.Equals(stored, AcknowledgedValue, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // No storage (private mode, storage disabled) means the notice is shown again — the
            // safe direction to fail for a warning about spending money.
            HasAcknowledgedCost = false;
        }
        OnChange?.Invoke();
    }

    public async Task AcknowledgeCostAsync()
    {
        if (HasAcknowledgedCost) return;
        HasAcknowledgedCost = true;
        var key = StorageKey;
        loadedForKey = key;
        OnChange?.Invoke();
        try { await js.InvokeVoidAsync(SetItem, key, AcknowledgedValue); }
        catch { } // Failing to persist only costs the user one extra acknowledgement.
    }

    private string StorageKey =>
        $"{StorageKeyPrefix}.{auth.CurrentUser?.Email.Trim().ToLowerInvariant() ?? "anonymous"}";
}
