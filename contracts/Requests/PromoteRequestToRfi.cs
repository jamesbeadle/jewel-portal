using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// Promotes a request to an RFI: mints the RFI reference and unlocks the official RFI document.
/// No email is drafted by promotion — a draft is only created when explicitly requested
/// (PrepareRequestEmailDraft for a fresh email, PrepareRequestReplyDraft to append the RFI to a
/// tagged email chain). A General request is the default state; this is the first rung of the
/// ladder General -> RFI -> (RFQ).
/// </summary>
public sealed record PromoteRequestToRfi(string RequestId) : ICommand<Request>;
