using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// Creates an Outlook draft in the connected projects mailbox carrying the request's official
/// document (the RFI PDF) so staff can review, adjust and send it from the mailbox itself.
/// Recipients resolve from the request's linked party — a client's primary contact or an
/// architect's contact email (falling back to the project's party, then to the project contacts
/// flagged ReceivesRequests).
/// Pass <see cref="RecipientOverride"/> to address the draft to one ad-hoc email instead.
/// Nothing is sent — the draft sits in the mailbox's Drafts folder until a person sends it.
/// </summary>
public sealed record PrepareRequestEmailDraft(
    string RequestId,
    string? RecipientOverride = null) : ICommand<RequestEmailDraft>;
