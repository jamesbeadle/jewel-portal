using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Subcontractors.Documents;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>
/// Emails the statement of account to the subcontractor from the shared projects mailbox, the
/// statement rendered from the live register and attached as a PDF so its figures match the
/// register at the moment it goes. Statement mail is subcontractor correspondence, so the thread is
/// born on that pathway. Staging, sending, the degrade to a draft and the audit row are the
/// dispatcher's (OutboundEmailDispatcher), shared with every other record's email.
/// </summary>
public sealed class SendSubcontractorStatementEmailHandler
    : ICommandHandler<SendSubcontractorStatementEmail, SubcontractorStatementEmailOutcome>
{
    private const string StagingRefused =
        "The statement email couldn't be staged in the shared mailbox, so nothing was sent. "
        + "Check the mailbox connection and try again.";

    private readonly IQueryHandler<GetSubcontractorStatement, SubcontractorStatement> statements;
    private readonly OutboundEmailDispatcher dispatcher;

    public SendSubcontractorStatementEmailHandler(
        IQueryHandler<GetSubcontractorStatement, SubcontractorStatement> statements,
        OutboundEmailDispatcher dispatcher)
    {
        this.statements = statements; this.dispatcher = dispatcher;
    }

    public async Task<SubcontractorStatementEmailOutcome> HandleAsync(
        SendSubcontractorStatementEmail command, CancellationToken cancellationToken)
    {
        var statement = await statements.HandleAsync(
            new GetSubcontractorStatement(command.SubcontractorId), cancellationToken);

        if (string.IsNullOrWhiteSpace(statement.ContactEmail))
            throw new InvalidOperationException(
                "The subcontractor has no email address in the directory — add one before emailing the statement.");

        var message = new MailboxDraftMessage(
            To: new[] { new MailboxDraftRecipient(statement.ContactEmail, statement.CompanyName) },
            Subject: command.Subject,
            HtmlBody: command.HtmlBody,
            Attachments: new[] { StatementPdf(statement) },
            Categories: new List<string> { TriageCategories.Marker, TriageCategories.Subcontractor });

        // A statement belongs to a company, not to a record with a tag of its own, so the audit row
        // carries the pathway and the company's name and no record.
        var filing = new OutboundEmailFiling(
            AuditTrail.PathwayLabel(TriageCategories.Subcontractor),
            StagingRefused,
            RecordReference: statement.CompanyName);

        var dispatch = await dispatcher.DispatchAsync(message, filing, command.SaveAsDraftOnly, cancellationToken);
        return new SubcontractorStatementEmailOutcome(
            statement.SubcontractorId, statement.CompanyName, command.Subject,
            statement.ContactEmail, dispatch.WebLink, dispatch.Sent, dispatch.FailureNote);
    }

    private static MailboxDraftAttachment StatementPdf(SubcontractorStatement statement) =>
        new(SanitiseFileName($"{statement.CompanyName} - Statement of account - {statement.GeneratedAt:yyyy-MM-dd}.pdf"),
            "application/pdf",
            SubcontractorStatementRenderer.Render(statement));

    private static string SanitiseFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }
}
