using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Undo a discard: the email was dismissed but actually does need triaging. It goes back to
// NeedsTriage so it reappears in the live queue, and the mailbox copy is moved back to the Inbox
// (best-effort) so the platform and the mailbox stay mirrored. Only acts on Discarded emails.
public sealed record RestoreIntakeEmail(
    string IntakeId) : ICommand<IntakeEmail>;
