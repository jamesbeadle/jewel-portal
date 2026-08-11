using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.ProjectContracts.Storage;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.ProjectContracts;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Commands;

/// <summary>
/// Permanently removes an amendment and its stored document. Row first, blob second on a
/// best-effort basis, mirroring the executed-contract replacement: once the row is gone nothing
/// points at the blob, and an orphan blob is harmless where an orphan row is not.
/// </summary>
public sealed class RemoveProjectContractAmendmentHandler
    : ICommandHandler<RemoveProjectContractAmendment, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly IProjectContractBlobStore blobStore;

    public RemoveProjectContractAmendmentHandler(JpmsContext context, IProjectContractBlobStore blobStore)
    {
        this.context = context;
        this.blobStore = blobStore;
    }

    public async Task<Acknowledgement> HandleAsync(
        RemoveProjectContractAmendment command, CancellationToken cancellationToken)
    {
        // Both ids, not just the key: a stale amendment id from another project must read as "not
        // found", never as a cross-project delete.
        var entity = await context.ProjectContractAmendments
            .FirstOrDefaultAsync(
                row => row.ProjectContractAmendmentId == command.ProjectContractAmendmentId
                    && row.ProjectId == command.ProjectId,
                cancellationToken);
        if (entity is null)
            throw new InvalidOperationException("That amendment no longer exists — it may already have been removed.");

        var blobRef = entity.DocumentBlobRef;

        context.ProjectContractAmendments.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(blobRef))
        {
            try { await blobStore.DeleteAsync(blobRef, cancellationToken); }
            catch { /* best effort — the row is gone; an orphan blob is harmless */ }
        }

        return new Acknowledgement(command.ProjectContractAmendmentId);
    }
}
