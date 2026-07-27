namespace Jewel.JPMS.Services;

/// <summary>
/// A piece of work the user and the assistant are doing together in a dialog beside the chat.
/// Started by the page that owns the dialog; read by the chat panel.
/// </summary>
public sealed record AiTask(
    /// <summary>Also the conversation's CapabilityKey on the server, e.g. "variation-draft".</summary>
    string TaskKey,
    /// <summary>A ModalCatalog key, e.g. "variation_draft".</summary>
    string ModalKey,
    /// <summary>The panel's banner: "Drafting a variation from RFI-049".</summary>
    string BannerLabel,
    /// <summary>Replaces "Thinking…" while a turn is in flight, so the wait says what it is
    /// waiting on: "Reading RFI-049 and the emails tagged to it…".</summary>
    string BusyLabel,
    /// <summary>The first user turn, sent automatically once the cost notice has been accepted.</summary>
    string KickoffMessage,
    string? ProjectId,
    string? RecordType,
    string? RecordId,
    /// <summary>What the user reads the record as — "RFI-049".</summary>
    string? RecordReference);

/// <summary>
/// The one piece of state the chat panel (which lives in MainLayout) and a task dialog (which lives
/// in a page) share. The panel reads <see cref="DraftJson"/> when it assembles each turn's scope, so
/// the model always sees the user's own edits; the dialog listens on <see cref="OnDraftApplied"/>
/// for what the assistant proposes back. Neither component knows the other exists.
///
/// <para>The draft crosses as JSON rather than a typed record on purpose. This is the generic
/// mechanism behind ModalCatalog (docs/ai/00-agent-architecture.md §5); the moment it knows what a
/// variation looks like it stops being reusable, and the page that owns the dialog is the only thing
/// that should own its shape.</para>
///
/// <para>Scoped, alongside <see cref="ChatPanelState"/> and the rest of the session state. It does
/// NOT own <see cref="ChatPanelState.CoexistingModalOpen"/>: that describes whether a dialog is on
/// screen, which is the dialog's business. Ending a conversation must not drop the panel back
/// underneath a form that is still open.</para>
/// </summary>
public sealed class AiTaskState
{
    public AiTask? Active { get; private set; }

    /// <summary>The dialog's field values as they stand right now. Never null — "{}" until the
    /// dialog says otherwise, because the server renders this straight into the prompt.</summary>
    public string DraftJson { get; private set; } = "{}";

    /// <summary>
    /// The kick-off turn, queued until something is willing to send it. The panel takes it as soon
    /// as the billed-usage notice has been accepted — and never if it has not, so opening the dialog
    /// cannot spend money on the Claude API on its own. Taken exactly once.
    /// </summary>
    public string? PendingKickoff { get; private set; }

    /// <summary>
    /// The task started, ended, or changed. For panel and layout chrome only — deliberately NOT
    /// raised by <see cref="UpdateDraft"/>, or every keystroke in the dialog would repaint the chat.
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// The assistant has proposed new field values. The argument is the raw fields JSON exactly as
    /// the model sent it: the dialog validates and MERGES it, never replaces its state wholesale, so
    /// a field the assistant did not mention keeps whatever the user typed.
    /// </summary>
    public event Action<string>? OnDraftApplied;

    /// <summary>
    /// The assistant could not answer at all — no API key on this environment, or the Claude call
    /// failed. The dialog listens so it can fall back to filling itself in from the record: a form
    /// left blank waiting for a draft that is never coming is worse than no assistant at all.
    /// </summary>
    public event Action? OnAssistantUnavailable;

    public void Start(AiTask task, string draftJson)
    {
        Active = task;
        DraftJson = string.IsNullOrWhiteSpace(draftJson) ? "{}" : draftJson;
        PendingKickoff = task.KickoffMessage;
        OnChange?.Invoke();
    }

    /// <summary>From the dialog, on every edit. Silent by design — see <see cref="OnChange"/>.</summary>
    public void UpdateDraft(string draftJson)
    {
        if (Active is null) return;
        DraftJson = string.IsNullOrWhiteSpace(draftJson) ? "{}" : draftJson;
    }

    /// <summary>From the chat panel, when a update_open_modal action comes back from the server.</summary>
    public void ApplyFromAssistant(string fieldsJson)
    {
        if (Active is null || string.IsNullOrWhiteSpace(fieldsJson)) return;
        OnDraftApplied?.Invoke(fieldsJson);
    }

    public void ReportAssistantUnavailable()
    {
        if (Active is null) return;
        OnAssistantUnavailable?.Invoke();
    }

    /// <summary>Returns the queued kick-off and clears it, so it can only ever be sent once.</summary>
    public string? TakePendingKickoff()
    {
        var pending = PendingKickoff;
        PendingKickoff = null;
        return pending;
    }

    /// <summary>
    /// Ends the conversation's remit. Deliberately does NOT touch the panel's coexistence flag —
    /// that belongs to the dialog, which may still be open. Safe to call twice.
    /// </summary>
    public void End()
    {
        if (Active is null) return;
        Active = null;
        DraftJson = "{}";
        PendingKickoff = null;
        OnChange?.Invoke();
    }
}
