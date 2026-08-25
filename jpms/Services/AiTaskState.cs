namespace Jewel.JPMS.Services;

/// <summary>
/// A piece of work the user and the assistant are doing together in a dialog beside the chat.
/// Started by the page that owns the dialog; read by the chat panel.
/// </summary>
public sealed record AiTask(
    /// <summary>Names the task flow, e.g. "variation-draft". No longer the conversation's
    /// CapabilityKey — since the agent registry (2026-08-12) the server picks the agent from the
    /// route and switch_agent; this key only labels the dialog work.</summary>
    string TaskKey,
    /// <summary>A ModalCatalog key, e.g. "variation_draft".</summary>
    string ModalKey,
    /// <summary>The panel's banner: "Drafting a variation from RFI-049".</summary>
    string BannerLabel,
    /// <summary>Replaces "Thinking…" while a turn is in flight, so the wait says what it is
    /// waiting on: "Reading RFI-049 and the emails tagged to it…".</summary>
    string BusyLabel,
    /// <summary>The first user turn, sent automatically once the cost notice has been accepted.
    /// Null or blank means NO kick-off: the task rides along silently — the open dialog's contents
    /// and the update tool reach the model, but no billed turn starts until the user speaks. Used
    /// for dialogs the user opened by hand (the Control Centre's reply composer).</summary>
    string? KickoffMessage,
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

    /// <summary>
    /// True once the dialog has published its real state since the task started. The chat panel
    /// waits briefly on this before sending a kick-off, so the first hop's scope carries the
    /// form as it actually stands (work_order_edit's pre-filled lines above all) rather than the
    /// "{}" the task was started with while the dialog was still rendering.
    /// </summary>
    public bool DraftPublishedSinceStart { get; private set; }

    public void Start(AiTask task, string draftJson)
    {
        // The SAME task started again — the same dialog for the same record — must never queue a
        // second kick-off. Each kick-off is a fresh billed conversation, and a model that re-opens
        // the dialog it is sitting in (live, 2026-08-25: the V2 build-up, three conversations a
        // minute apart) would otherwise restart itself for as long as the credits last. The task
        // simply carries on: the dialog is still open, the conversation that is already about it
        // keeps its remit. The server refuses the open_modal too; this is the last line. A page
        // that WANTS a restart (the user pressing "Draft with AI" again) ends the task first.
        var sameTask = Active is not null
            && string.Equals(Active.TaskKey, task.TaskKey, StringComparison.Ordinal)
            && string.Equals(Active.RecordId, task.RecordId, StringComparison.Ordinal);

        if (sameTask)
        {
            Active = task;
            // A real state re-seeded by the page is honoured; "{}" keeps the live draft, because
            // the dialog did not close and its contents did not change.
            if (!string.IsNullOrWhiteSpace(draftJson) && draftJson.Trim() != "{}")
            {
                DraftJson = draftJson;
                DraftPublishedSinceStart = true;
            }
            OnChange?.Invoke();
            return;
        }

        Active = task;
        DraftJson = string.IsNullOrWhiteSpace(draftJson) ? "{}" : draftJson;
        // A real initial state counts as published — only "{}" leaves the panel waiting for the
        // dialog's first publish.
        DraftPublishedSinceStart = !string.IsNullOrWhiteSpace(draftJson) && draftJson.Trim() != "{}";
        PendingKickoff = string.IsNullOrWhiteSpace(task.KickoffMessage) ? null : task.KickoffMessage;
        OnChange?.Invoke();
    }

    /// <summary>From the dialog, on every edit. Silent by design — see <see cref="OnChange"/>.</summary>
    public void UpdateDraft(string draftJson)
    {
        if (Active is null) return;
        DraftJson = string.IsNullOrWhiteSpace(draftJson) ? "{}" : draftJson;
        DraftPublishedSinceStart = true;
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
        DraftPublishedSinceStart = false;
        OnChange?.Invoke();
    }
}
