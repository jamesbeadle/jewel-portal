using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Subcontractors.Documents;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>
/// Writes the statement of account's email. The subject and the covering note used to be composed
/// in the modal (SubcontractorStatementModal), so the portal held two spellings of one email and
/// only one of them could be previewed. They are written here now; the modal seeds itself from the
/// preview and a person edits from there.
/// </summary>
public sealed partial class SubcontractorStatementEmailComposer : IComposesRecordEmail
{
    private readonly IQueryHandler<GetSubcontractorStatement, SubcontractorStatement> statements;

    public SubcontractorStatementEmailComposer(
        IQueryHandler<GetSubcontractorStatement, SubcontractorStatement> statements) =>
        this.statements = statements;

    /// <summary>A statement belongs to a company, not to a project or a record with a tag of its
    /// own — SubcontractorComms is the record-less family the correspondence files under.</summary>
    public RecordType Record => RecordType.SubcontractorComms;

    public RoleSet RolesThatMaySend =>
        SendSubcontractorStatementEmailAuthorisation.RolesThatMayEmailStatements;

    public async Task<ComposedRecordEmail> ComposeAsync(RecordEmailDraft draft, CancellationToken cancellationToken)
    {
        var statement = await statements.HandleAsync(
            new GetSubcontractorStatement(draft.RecordId), cancellationToken);

        if (string.IsNullOrWhiteSpace(statement.ContactEmail))
            throw new InvalidOperationException(
                "The subcontractor has no email address in the directory — add one before emailing the statement.");

        var message = new MailboxDraftMessage(
            To: new[] { new MailboxDraftRecipient(statement.ContactEmail, statement.CompanyName) },
            Subject: string.IsNullOrWhiteSpace(draft.Subject) ? DefaultSubject(statement) : draft.Subject.Trim(),
            HtmlBody: string.IsNullOrWhiteSpace(draft.BodyHtml) ? DefaultBody(statement) : draft.BodyHtml,
            Attachments: new[] { StatementPdf(statement) },
            Categories: new List<string> { TriageCategories.Marker, TriageCategories.Subcontractor });

        return new ComposedRecordEmail(
            statement.CompanyName, ProjectId: null, message, Array.Empty<string>());
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
