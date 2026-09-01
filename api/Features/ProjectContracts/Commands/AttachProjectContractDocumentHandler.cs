using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Api.Features.ProjectContracts.Storage;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Commands;

/// <summary>
/// Records the uploaded document against the project's contract, creating the contract row if the
/// terms have not been keyed yet — a user can upload the PDF first and fill the terms in afterwards.
///
/// <para>Replacing a document deletes the previous blob on a best-effort basis after the row is
/// committed. Unlike compliance documents, contract documents are not versioned: there is one
/// executed contract, and a replacement means the wrong file was uploaded. If contract amendments
/// need their own history, that is a separate record type, not a version chain here.</para>
/// </summary>
public sealed class AttachProjectContractDocumentHandler
    : ICommandHandler<AttachProjectContractDocument, ProjectContract>
{
    private readonly JpmsContext context;
    private readonly IProjectContractBlobStore blobStore;

    public AttachProjectContractDocumentHandler(JpmsContext context, IProjectContractBlobStore blobStore)
    {
        this.context = context;
        this.blobStore = blobStore;
    }

    public async Task<ProjectContract> HandleAsync(
        AttachProjectContractDocument command, CancellationToken cancellationToken)
    {
        var entity = await context.ProjectContracts
            .FirstOrDefaultAsync(row => row.ProjectId == command.ProjectId, cancellationToken);

        if (entity is null)
        {
            entity = new ProjectContractEntity
            {
                // Matches the id segment the endpoint already used to build the blob ref.
                ProjectContractId = ProjectContractIdFrom(command.BlobRef),
                ProjectId = command.ProjectId
            };
            context.ProjectContracts.Add(entity);
        }

        var previousBlobRef = entity.DocumentBlobRef;

        entity.DocumentBlobRef = command.BlobRef;
        entity.DocumentFileName = command.FileName;
        entity.DocumentContentType = command.ContentType;
        entity.DocumentFileSizeBytes = command.FileSizeBytes;
        entity.DocumentUploadedAt = DateTimeOffset.UtcNow;
        entity.DocumentUploadedByEmail = command.UploadedByEmail;

        entity.UpdatedByEmail = command.UploadedByEmail;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousBlobRef) && previousBlobRef != command.BlobRef)
        {
            try { await blobStore.DeleteAsync(previousBlobRef, cancellationToken); }
            catch { /* best effort — the row points at the new blob; an orphan is harmless */ }
        }

        return entity.ToModel();
    }

    // The endpoint mints the contract id, uses it as the middle path segment, then hands the ref
    // here. Recovering it from the ref keeps the row id and the blob path in step without adding a
    // field to the command.
    private static string ProjectContractIdFrom(string blobRef)
    {
        var parts = blobRef.Split('/');
        return parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1])
            ? parts[1]
            : ProjectContractsIdentifierFactory.NextProjectContractId();
    }
}
