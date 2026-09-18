using Jewel.JPMS.Api.Features.Commercial.Documents;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// Writes the valuation statement's email. The subject and the covering note used to be composed
/// in the modal (ValuationStatementEmailModal), which meant the portal held two spellings of one
/// email and only one of them could ever be previewed. They are written here now; the modal seeds
/// itself from the preview and a person edits from there.
/// </summary>
public sealed partial class ValuationStatementEmailComposer : IComposesRecordEmail
{
    private readonly ValuationStatementPdfBuilder builder;
    private readonly JpmsContext context;

    public ValuationStatementEmailComposer(ValuationStatementPdfBuilder builder, JpmsContext context)
    {
        this.builder = builder;
        this.context = context;
    }

    public RecordType Record => RecordType.ValuationClaim;

    public RoleSet RolesThatMaySend =>
        SendValuationStatementEmailAuthorisation.RolesThatMayEmailStatements;

    public async Task<ComposedRecordEmail> ComposeAsync(RecordEmailDraft draft, CancellationToken cancellationToken)
    {
        var pdf = await builder.BuildAsync(draft.RecordId, cancellationToken);
        if (pdf.Claim.Status == ValuationClaimStatus.Draft)
            throw new InvalidOperationException(
                $"{pdf.Claim.DisplayName} is still a draft — its statement is a working copy, not a record. Lock it (\"We're claiming this\") before emailing it to the client.");
        var recipients = await ClientSideRecipientsAsync(pdf.ProjectId, cancellationToken);
        var projectName = await ProjectNameAsync(pdf.ProjectId, cancellationToken);

        // The valuation's own tag, spelt exactly as the register spells it, so the sent copy and
        // the client's reply to it file under this valuation rather than only under Client.
        var recordTag = await ValuationClaimTags.StemAsync(
            context, pdf.ProjectId, pdf.Claim.ClaimNumber, cancellationToken);

        var message = new MailboxDraftMessage(
            To: recipients,
            Subject: string.IsNullOrWhiteSpace(draft.Subject)
                ? DefaultSubject(pdf.Claim, projectName)
                : draft.Subject.Trim(),
            HtmlBody: string.IsNullOrWhiteSpace(draft.BodyHtml)
                ? DefaultBody(pdf.Claim, projectName)
                : draft.BodyHtml,
            Attachments: new[] { new MailboxDraftAttachment(pdf.FileName, "application/pdf", pdf.Content) },
            Categories: new List<string>
            {
                TriageCategories.Marker,
                TriageCategories.ForRecord(recordTag),
                TriageCategories.Client
            });

        return new ComposedRecordEmail(
            pdf.Claim.DisplayName, pdf.ProjectId, message, Array.Empty<string>());
    }

    /// <summary>The client side of the correspondence profile, read through the shared rule the
    /// variation order's email reads too.</summary>
    private async Task<List<MailboxDraftRecipient>> ClientSideRecipientsAsync(
        string projectId, CancellationToken cancellationToken)
    {
        var recipients = await ClientSideRecipients.ForProjectAsync(context, projectId, cancellationToken);
        if (recipients.Count > 0) return recipients;
        throw new InvalidOperationException(
            "The project has no client or architect contact with an email address — add one to the "
            + "project's contacts before emailing the valuation.");
    }

    private Task<string?> ProjectNameAsync(string projectId, CancellationToken cancellationToken) =>
        context.Projects.AsNoTracking()
            .Where(project => project.ProjectId == projectId)
            .Select(project => (string?)project.Name)
            .FirstOrDefaultAsync(cancellationToken);
}
