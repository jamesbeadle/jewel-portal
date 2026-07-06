using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// Creates an Outlook draft in the connected projects mailbox carrying the request's official
/// document (the RFI PDF) so staff can review, adjust and send it from the mailbox itself.
/// Recipients (To/CC/BCC) resolve through the shared correspondence profile — the request's
/// linked party first, then the project's party, then the project profile's To rows, with the
/// profile supplying the copied recipients.
/// Pass <see cref="RecipientOverride"/> to address the draft to one ad-hoc email instead (no
/// CC/BCC). Nothing is sent — the draft sits in the mailbox's Drafts folder until a person sends it.
/// </summary>
public sealed record PrepareRequestEmailDraft(
    string RequestId,
    string? RecipientOverride = null) : ICommand<RequestEmailDraft>;
