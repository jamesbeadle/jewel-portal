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
    private const string ConversationKeyPrefix = "jpms.chatConversation";
    private const string GetItem = "localStorage.getItem";
    private const string SetItem = "localStorage.setItem";
    private const string RemoveItem = "localStorage.removeItem";
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
    /// True while the assistant has a turn in flight. Set by the panel, read by chat-aware dialogs
    /// so a form the assistant is filling can SAY it is being worked on — a user watching a blank
    /// form while the model types into it through the wire otherwise has no idea anything is
    /// happening.
    /// </summary>
    public bool AssistantBusy { get; private set; }

    public void SetAssistantBusy(bool value)
    {
        if (AssistantBusy == value) return;
        AssistantBusy = value;
        // Busy ending always ends control too — one switch cannot be left on without the other,
        // whatever path the turn ended by (completion, truncation, an exception, a failed send).
        if (!value) AssistantControlling = false;
        OnChange?.Invoke();
    }

    /// <summary>
    /// True from the moment a turn's first UI action lands (navigate_to, open_modal,
    /// update_open_modal) until the turn ends: the assistant is OPERATING the portal, not just
    /// answering. MainLayout renders the takeover overlay while it is set — the screen dims behind
    /// a green-edged vignette and the app stops taking input, so the user cannot fight the
    /// assistant for the controls mid-action. The chat panel lifts above the overlay and stays
    /// fully usable throughout. Set by ChatPanel when it applies a UI action; cleared by
    /// <see cref="SetAssistantBusy"/>(false) so no error path can strand the screen dimmed.
    /// </summary>
    public bool AssistantControlling { get; private set; }

    public void SetAssistantControlling(bool value)
    {
        if (AssistantControlling == value) return;
        AssistantControlling = value;
        OnChange?.Invoke();
    }

    // ---- the page note -----------------------------------------------------------------------
    // What the OPEN PAGE is showing right now, beyond what the route says — the Control Centre's
    // selected email, a register's active filter. Registered as a provider (a delegate, not a
    // string) so the note is computed at send time from the page's live state instead of the page
    // having to re-publish on every click. The page registers it on init and clears it on Dispose,
    // so navigation away can never leave a stale note behind; same-page query navigation keeps it.
    // Read by ChatPanel.BuildScope into AiScope.PageNote and rendered into the model's volatile
    // "current context" block — it is DATA about the screen, never instructions.

    private Func<string?>? pageNoteProvider;

    /// <summary>The page's live "what I am showing" callback. Last writer wins (one page owns the
    /// middle of the screen at a time).</summary>
    public void SetPageNoteProvider(Func<string?> provider) => pageNoteProvider = provider;

    /// <summary>Clears the provider, but only if it is still this caller's — a page disposing late
    /// (Blazor disposes the old page after the new one initialises) must not wipe its successor's.</summary>
    public void ClearPageNoteProvider(Func<string?> provider)
    {
        if (ReferenceEquals(pageNoteProvider, provider)) pageNoteProvider = null;
    }

    /// <summary>The note as it stands right now, or null. Never throws — a page's broken provider
    /// costs the model one note, not the user their message.</summary>
    public string? CurrentPageNote()
    {
        try { return pageNoteProvider?.Invoke(); }
        catch { return null; }
    }

    // ---- page actions ------------------------------------------------------------------------
    // The reverse of the page note: a UI action the assistant addressed to the OPEN PAGE rather
    // than to the panel (stage_triage_tag → the Control Centre stages the pick in System Tags).
    // Same lifecycle discipline as the note provider: the page registers on init, clears on
    // Dispose with a reference-equality guard, so a late-disposing page cannot wipe its successor.
    // The server only offers page-scoped tools while the user is on the owning route, so a missing
    // handler is a build-skew bug the panel reports loudly, never a silent drop.

    private Func<string, string, Task>? pageActionHandler;

    public void SetPageActionHandler(Func<string, string, Task> handler) => pageActionHandler = handler;

    public void ClearPageActionHandler(Func<string, string, Task> handler)
    {
        if (ReferenceEquals(pageActionHandler, handler)) pageActionHandler = null;
    }

    /// <summary>Hands an action to the open page and WAITS for it. False when no page is listening
    /// — the caller says so out loud. Awaited on purpose: the turn's next hop rebuilds the page
    /// note, and the model must read what actually happened (a staged tag listed, or nothing plus
    /// the page's on-screen refusal) — a fire-and-forget here is how the assistant ends up
    /// narrating a tag that never staged. The page owns its own errors and repaints.</summary>
    public async Task<bool> DispatchPageActionAsync(string tool, string argumentsJson)
    {
        if (pageActionHandler is null) return false;
        try { await pageActionHandler(tool, argumentsJson); }
        catch { /* the page reports its own failures; the dispatch itself never throws */ }
        return true;
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

    // ---- the resumable conversation ----------------------------------------------------------
    // The server holds every transcript; what the browser forgets on a refresh is only WHICH
    // conversation this panel was in. Remembering that one id (same per-user localStorage idiom as
    // the acknowledgement) is what lets the panel pick the thread back up via ListAiConversation.

    /// <summary>The conversation this user's panel was last in, or null. Never a guess: cleared by
    /// New chat and whenever a stored id turns out not to replay.</summary>
    public async Task<string?> LoadStoredConversationIdAsync()
    {
        if (auth.CurrentUser is null) return null;
        try
        {
            var stored = await js.InvokeAsync<string?>(GetItem, ConversationKey);
            return string.IsNullOrWhiteSpace(stored) ? null : stored;
        }
        catch
        {
            return null; // No storage means no resume — the safe direction to fail.
        }
    }

    public async Task RememberConversationAsync(string? conversationId)
    {
        if (auth.CurrentUser is null || string.IsNullOrWhiteSpace(conversationId)) return;
        try { await js.InvokeVoidAsync(SetItem, ConversationKey, conversationId); }
        catch { } // Failing to persist only costs the user one resume.
    }

    public async Task ForgetConversationAsync()
    {
        if (auth.CurrentUser is null) return;
        try { await js.InvokeVoidAsync(RemoveItem, ConversationKey); }
        catch { }
    }

    // ---- the model choice --------------------------------------------------------------------
    // Cheap by default; the user's own pick is remembered per browser and wins from then on. Only
    // the AiModelCatalogue KEY is stored — mapping a key to a real model id is the server's,
    // against config, so nothing in localStorage can name a model.

    public async Task<string?> LoadStoredModelAsync()
    {
        if (auth.CurrentUser is null) return null;
        try
        {
            var stored = await js.InvokeAsync<string?>(GetItem, ModelKey);
            return string.IsNullOrWhiteSpace(stored) ? null : stored;
        }
        catch
        {
            return null; // No storage means the default — the cheap direction to fail.
        }
    }

    public async Task RememberModelAsync(string? modelKey)
    {
        if (auth.CurrentUser is null || string.IsNullOrWhiteSpace(modelKey)) return;
        try { await js.InvokeVoidAsync(SetItem, ModelKey, modelKey); }
        catch { }
    }

    private const string ModelKeyPrefix = "jpms.chatModel";

    private string ModelKey =>
        $"{ModelKeyPrefix}.{auth.CurrentUser?.Email.Trim().ToLowerInvariant() ?? "anonymous"}";

    private string ConversationKey =>
        $"{ConversationKeyPrefix}.{auth.CurrentUser?.Email.Trim().ToLowerInvariant() ?? "anonymous"}";

    private string StorageKey =>
        $"{StorageKeyPrefix}.{auth.CurrentUser?.Email.Trim().ToLowerInvariant() ?? "anonymous"}";
}
