using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// The resend is the send. Until 2026-09-18 it enqueued a mailbox action and a background worker
/// carried its own copy of the outbound sequence — rendering the document, staging a draft and
/// stopping there — so the command promised a send it never performed and the assistant had no
/// outcome to read back. It now hands the same request and the same optional ad-hoc address to
/// <see cref="SendRequestEmailHandler"/>, which renders, stages, sends, degrades to a reviewed
/// draft if the mailbox refuses, and writes the audit row.
/// </summary>
public sealed class ResendRequestDocumentHandler : ICommandHandler<ResendRequestDocument, RequestEmailOutcome>
{
    private readonly ICommandHandler<SendRequestEmail, RequestEmailOutcome> send;

    public ResendRequestDocumentHandler(ICommandHandler<SendRequestEmail, RequestEmailOutcome> send) =>
        this.send = send;

    public Task<RequestEmailOutcome> HandleAsync(ResendRequestDocument command, CancellationToken cancellationToken) =>
        send.HandleAsync(
            new SendRequestEmail(
                command.RequestId,
                string.IsNullOrWhiteSpace(command.RecipientOverride) ? null : command.RecipientOverride.Trim()),
            cancellationToken);
}
