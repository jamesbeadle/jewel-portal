using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// Re-issue a request's document to its recipients. With no <see cref="RecipientOverride"/> it goes to
/// the project's flagged contacts; with one, it is an ad-hoc resend to that single email address.
/// </summary>
public sealed record ResendRequestDocument(string RequestId, string? RecipientOverride)
    : ICommand<Acknowledgement>;
