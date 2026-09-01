using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.ProjectContracts;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Commands;

/// <summary>
/// Records a contract amendment after its document has been stored. Always an insert — amendments
/// accumulate, unlike the executed contract document, which supersedes. There is deliberately no
/// upsert here: two uploads are two amendments, and a wrong one is removed, not overwritten.
/// </summary>
public sealed class AttachProjectContractAmendmentHandler
    : ICommandHandler<AttachProjectContractAmendment, ProjectContractAmendment>
{
    private readonly JpmsContext context;

    public AttachProjectContractAmendmentHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<ProjectContractAmendment> HandleAsync(
        AttachProjectContractAmendment command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var entity = new ProjectContractAmendmentEntity
        {
            ProjectContractAmendmentId = command.ProjectContractAmendmentId,
            ProjectId = command.ProjectId,

            Title = command.Title.Trim(),
            AmendmentDate = command.AmendmentDate,
            Notes = Trimmed(command.Notes),

            DocumentBlobRef = command.BlobRef,
            DocumentFileName = command.FileName,
            DocumentContentType = command.ContentType,
            DocumentFileSizeBytes = command.FileSizeBytes,
            DocumentUploadedAt = now,
            DocumentUploadedByEmail = command.UploadedByEmail,

            UpdatedByEmail = command.UploadedByEmail,
            UpdatedAt = now
        };
        context.ProjectContractAmendments.Add(entity);

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
