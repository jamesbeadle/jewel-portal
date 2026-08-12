using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpDocumentControlStore : IDocumentControlStore
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpDocumentControlStore(IQueryClient queries, ICommandSender commands)
    {
        this.queries = queries;
        this.commands = commands;
    }

    public Task<IReadOnlyList<DocumentControlItem>> ListAsync(CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListDocumentControlItems(), cancellationToken);

    public Task<DocumentControlItem> FileAsDrawingAsync(
        string documentControlItemId, string projectId, string drawingCode, string title,
        string revisionLabel, CancellationToken cancellationToken = default) =>
        commands.SendAsync(
            new FileDocumentAsDrawing(documentControlItemId, projectId, drawingCode, title, revisionLabel),
            cancellationToken);

    public Task<DocumentControlItem> FileAsPaymentCertificateAsync(
        string documentControlItemId, string projectId, string certificateNumber,
        decimal? certifiedAmount, DateTimeOffset issuedDate, string? valuationClaimId,
        CancellationToken cancellationToken = default) =>
        commands.SendAsync(
            new FileDocumentAsPaymentCertificate(
                documentControlItemId, projectId, certificateNumber, certifiedAmount, issuedDate, valuationClaimId),
            cancellationToken);

    public Task<DocumentControlItem> FileToSubcontractorAsync(
        string documentControlItemId, string subcontractorId, string kind, DateTimeOffset? expiresAt,
        CancellationToken cancellationToken = default) =>
        commands.SendAsync(
            new FileDocumentToSubcontractor(documentControlItemId, subcontractorId, kind, expiresAt),
            cancellationToken);

    public Task<DocumentControlItem> DiscardAsync(string documentControlItemId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new DiscardDocumentControlItem(documentControlItemId), cancellationToken);

    public Task<DocumentControlItem> RestoreAsync(string documentControlItemId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new RestoreDocumentControlItem(documentControlItemId), cancellationToken);
}
