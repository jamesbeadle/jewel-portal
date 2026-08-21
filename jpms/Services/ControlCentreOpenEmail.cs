using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

// A one-shot handoff into the Control Centre: "open with THIS email selected". Set by surfaces
// that find an email elsewhere (the to-do searches' tagged-email results) just before navigating
// to /control-centre; the page takes it exactly once on arrival and selects the email — full
// reading pane, tag management, record creation, the assistant's page context, all of it —
// instead of leaving the finder at a dead end with an email it can see but not open. A scoped
// service rather than a query parameter because Graph message ids are long and path-hostile, and
// the finder already holds the whole MailboxMessage the selection needs.
public sealed class ControlCentreOpenEmail
{
    private MailboxMessage? pending;

    public void Set(MailboxMessage email) => pending = email;

    /// <summary>Read-and-clear: a later refresh of the Control Centre must not re-select a stale
    /// handoff, so whatever is taken is gone.</summary>
    public MailboxMessage? Take()
    {
        var email = pending;
        pending = null;
        return email;
    }
}
