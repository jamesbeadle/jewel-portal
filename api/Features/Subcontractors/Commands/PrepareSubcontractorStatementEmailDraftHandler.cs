using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Subcontractors.Documents;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

// Drafts the statement-of-account email in the shared mailbox — nothing is sent (same convention
// as PrepareWorkOrderEmailDraft: the human sends from Outlook). Addressed To the subcontractor's
// directory email, with the statement rendered from the live register and attached as a PDF, so
// the attachment always matches the register at the moment of drafting. Statement mail is
// subcontractor correspondence, so the thread is born on that pathway.
public sealed class PrepareSubcontractorStatementEmailDraftHandler
    : ICommandHandler<PrepareSubcontractorStatementEmailDraft, SubcontractorStatementEmailDraft>
{
    private readonly IQueryHandler<GetSubcontractorStatement, SubcontractorStatement> statements;
    private readonly IMailboxGraphClient mailbox;

    public PrepareSubcontractorStatementEmailDraftHandler(
        IQueryHandler<GetSubcontractorStatement, SubcontractorStatement> statements,
        IMailboxGraphClient mailbox)
    {
        this.statements = statements; this.mailbox = mailbox;
    }

    public async Task<SubcontractorStatementEmailDraft> HandleAsync(
        PrepareSubcontractorStatementEmailDraft command, CancellationToken cancellationToken)
    {
        var statement = await statements.HandleAsync(
            new GetSubcontractorStatement(command.SubcontractorId), cancellationToken);

        if (string.IsNullOrWhiteSpace(statement.ContactEmail))
            throw new InvalidOperationException(
                "The subcontractor has no email address in the directory — add one before drafting the statement email.");

        var pdf = SubcontractorStatementRenderer.Render(statement);
        var fileName = SanitiseFileName(
            $"{statement.CompanyName} - Statement of account - {statement.GeneratedAt:yyyy-MM-dd}.pdf");

        var message = new MailboxDraftMessage(
            To: new[] { new MailboxDraftRecipient(statement.ContactEmail, statement.CompanyName) },
            Subject: command.Subject,
            HtmlBody: command.HtmlBody,
            Attachments: new[] { new MailboxDraftAttachment(fileName, "application/pdf", pdf) },
            Categories: new List<string> { TriageCategories.Marker, TriageCategories.Subcontractor });

        var draft = await mailbox.CreateDraftAsync(message, cancellationToken);
        if (draft is null)
            throw new InvalidOperationException(
                "The draft couldn't be created in the shared mailbox. Check the mailbox connection and try again.");

        return new SubcontractorStatementEmailDraft(
            statement.SubcontractorId,
            statement.CompanyName,
            command.Subject,
            statement.ContactEmail,
            draft.WebLink);
    }

    private static string SanitiseFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }
}
