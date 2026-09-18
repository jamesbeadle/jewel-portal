using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// Re-issue a request's document to its recipients. With no <see cref="RecipientOverride"/> it goes to
/// the project's flagged contacts; with one, it is an ad-hoc resend to that single email address.
///
/// It is <see cref="SendRequestEmail"/> under an older name, and since 2026-09-18 it is that
/// command: the resend used to hand the work to a background worker that rendered the document,
/// staged a draft and stopped, so the command's name said "send" and its behaviour said "draft".
/// It now sends through the one outbound sequence and reports what happened, like every other
/// portal email.
/// </summary>
public sealed record ResendRequestDocument(string RequestId, string? RecipientOverride)
    : ICommand<RequestEmailOutcome>;
