using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.Variations.Documents;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Writes the variation order's email: who it goes to and the covering note that states the figure
/// (.CoverNote). The door that sends it and the read that previews it both come through here — the
/// modal deliberately does not compose a copy of the note to show, because two spellings of one
/// email is what put the project name in the subject twice.
/// </summary>
public sealed partial class VariationOrderEmailComposer : IComposesRecordEmail
{
    private readonly JpmsContext context;

    public VariationOrderEmailComposer(JpmsContext context) => this.context = context;

    public RecordType Record => RecordType.Variation;

    public RoleSet RolesThatMaySend => VariationRoles.AllowedToManageVariations;

    /// <summary>A variation's subject and note are always the portal's own words — the document is
    /// the message — so a draft's Subject and BodyHtml are not read here.</summary>
    public async Task<ComposedRecordEmail> ComposeAsync(RecordEmailDraft draft, CancellationToken cancellationToken)
    {
        var variation = await EmailableVariationAsync(draft.RecordId, cancellationToken);
        var recipients = await RecipientsForAsync(variation.ProjectId, draft.RecipientOverride, cancellationToken);

        var model = await VariationDocumentBuilder.BuildAsync(context, draft.RecordId, cancellationToken)
            ?? throw new InvalidOperationException($"Variation '{draft.RecordId}' not found.");

        return new ComposedRecordEmail(
            model.DocumentReference,
            variation.ProjectId,
            await StagedMessageAsync(variation, model, recipients, cancellationToken),
            Array.Empty<string>());
    }

    /// <summary>EMAIL POLICY: only an Issued, Awaiting AI or Approved variation is a document to
    /// send — a quoting-stage price has not been put, and a rejected one is terminal.</summary>
    private async Task<VariationOrderEntity> EmailableVariationAsync(
        string variationOrderId, CancellationToken cancellationToken)
    {
        var variation = await context.VariationOrders
            .FirstOrDefaultAsync(row => row.VariationOrderId == variationOrderId, cancellationToken);
        if (variation is null) throw new InvalidOperationException($"Variation '{variationOrderId}' not found.");

        var status = (VariationOrderStatus)variation.Status;
        if (status.IsEmailable()) return variation;

        throw new InvalidOperationException(
            $"A {status.DisplayName()} variation is not emailed — issue it to the client first. "
            + "Only an issued, awaiting-AI or approved variation order goes out as a document.");
    }
}
