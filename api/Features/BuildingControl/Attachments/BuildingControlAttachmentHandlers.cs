using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.BuildingControl;

namespace Jewel.JPMS.Api.Features.BuildingControl.Attachments;

public sealed class SetBuildingControlAttachmentKindHandler
    : ICommandHandler<SetBuildingControlAttachmentKind, BuildingControlAttachment>
{
    private readonly JpmsContext context;
    public SetBuildingControlAttachmentKindHandler(JpmsContext context) { this.context = context; }

    public async Task<BuildingControlAttachment> HandleAsync(
        SetBuildingControlAttachmentKind command, CancellationToken cancellationToken)
    {
        var entity = await context.BuildingControlAttachments.FirstOrDefaultAsync(
                row => row.BuildingControlAttachmentId == command.BuildingControlAttachmentId, cancellationToken)
            ?? throw new InvalidOperationException("That file no longer exists.");
        entity.Kind = (int)command.Kind;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

public sealed class SetBuildingControlAttachmentKindAuthorisation
{
    public bool Allows(SignedInUser user, SetBuildingControlAttachmentKind command) =>
        BuildingControlRoles.Managers.IncludesAny(user.Roles);
}

public sealed class RemoveBuildingControlAttachmentHandler
    : ICommandHandler<RemoveBuildingControlAttachment, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly IBuildingControlAttachmentStore blobStore;

    public RemoveBuildingControlAttachmentHandler(JpmsContext context, IBuildingControlAttachmentStore blobStore)
    {
        this.context = context;
        this.blobStore = blobStore;
    }

    public async Task<Acknowledgement> HandleAsync(RemoveBuildingControlAttachment command, CancellationToken cancellationToken)
    {
        var entity = await context.BuildingControlAttachments.FirstOrDefaultAsync(
                row => row.BuildingControlAttachmentId == command.BuildingControlAttachmentId, cancellationToken)
            ?? throw new InvalidOperationException("That file no longer exists.");

        var blobRef = entity.BlobRef;
        context.BuildingControlAttachments.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        // The row is the record; the bytes go best-effort. An orphaned blob is harmless (private
        // container, never listed), whereas failing the remove over storage noise would leave a
        // row the user has already decided to tidy away — the tender-enquiry rule.
        if (!string.IsNullOrWhiteSpace(blobRef))
        {
            try { await blobStore.DeleteAsync(blobRef, cancellationToken); }
            catch (Exception ex) when (ex is not OperationCanceledException) { }
        }
        return new Acknowledgement(command.BuildingControlAttachmentId);
    }
}

public sealed class RemoveBuildingControlAttachmentAuthorisation
{
    public bool Allows(SignedInUser user, RemoveBuildingControlAttachment command) =>
        BuildingControlRoles.Managers.IncludesAny(user.Roles);
}

/// <summary>
/// Copies files off an email linked to the inspection — the inspector's site report, their
/// photos — into the inspection's store (Source = Email). Downloads run FIRST, before anything
/// persists, so "that attachment isn't there any more" is a clean refusal rather than a
/// half-copied set (the TenderEnquiryEmailAttachmentFetcher stance).
/// </summary>
public sealed class CopyEmailAttachmentsToBuildingControlInspectionHandler
    : ICommandHandler<CopyEmailAttachmentsToBuildingControlInspection, IReadOnlyList<BuildingControlAttachment>>
{
    private readonly JpmsContext context;
    private readonly IIntakeMessageReader reader;
    private readonly BuildingControlAttachmentWriter writer;

    public CopyEmailAttachmentsToBuildingControlInspectionHandler(
        JpmsContext context, IIntakeMessageReader reader, BuildingControlAttachmentWriter writer)
    {
        this.context = context;
        this.reader = reader;
        this.writer = writer;
    }

    public async Task<IReadOnlyList<BuildingControlAttachment>> HandleAsync(
        CopyEmailAttachmentsToBuildingControlInspection command, CancellationToken cancellationToken)
    {
        var inspection = await context.BuildingControlInspections.AsNoTracking().FirstOrDefaultAsync(
                row => row.BuildingControlInspectionId == command.BuildingControlInspectionId, cancellationToken)
            ?? throw new InvalidOperationException("That inspection no longer exists.");

        var wanted = (command.AttachmentIds ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (wanted.Count == 0) throw new InvalidOperationException("Tick at least one attachment to copy.");

        var files = new List<BuildingControlIncomingFile>();
        foreach (var attachmentId in wanted)
        {
            var attachment = await reader.GetAttachmentAsync(command.MessageId, attachmentId, cancellationToken)
                ?? throw new InvalidOperationException(
                    "Couldn't download one of the ticked attachments from the mailbox — it may have "
                    + "been removed, or it isn't a file. Untick it and try again.");
            files.Add(new BuildingControlIncomingFile(attachment.Name, attachment.ContentType, attachment.Content));
        }

        var stored = new List<BuildingControlAttachment>();
        foreach (var file in files)
        {
            var kind = command.Kind ?? BuildingControlRules.InferKind(file.ContentType, file.Name);
            var entity = await writer.StoreAsync(
                inspection.ProjectId, caseId: null, inspection.BuildingControlInspectionId,
                file, kind, BuildingControlAttachmentSource.Email, command.AddedByEmail, cancellationToken);
            stored.Add(entity.ToModel());
        }
        await context.SaveChangesAsync(cancellationToken);
        return stored;
    }
}

public sealed class CopyEmailAttachmentsToBuildingControlInspectionAuthorisation
{
    public bool Allows(SignedInUser user, CopyEmailAttachmentsToBuildingControlInspection command) =>
        BuildingControlRoles.Managers.IncludesAny(user.Roles);
}
