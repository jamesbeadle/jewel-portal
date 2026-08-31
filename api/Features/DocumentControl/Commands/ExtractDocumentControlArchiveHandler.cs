using System.IO.Compression;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.DocumentControl.Storage;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

// Splits a pending zip into per-file queue items. Children copy the parent's email envelope and
// project hint and take an AttachmentId of "{parent}!{entry path}", so the send-handler's
// (MessageId, AttachmentId) read-then-insert dedupe also protects a re-run here: entries already
// extracted by a partially-failed earlier attempt are skipped, not duplicated. Blobs upload before
// the single save, mirroring FileDocumentAsDrawingHandler's copy-out-first ordering.
public sealed class ExtractDocumentControlArchiveHandler
    : ICommandHandler<ExtractDocumentControlArchive, IReadOnlyList<DocumentControlItem>>
{
    private const int AttachmentIdMaximumLength = 512;

    private readonly JpmsContext context;
    private readonly IDocumentControlBlobStore documentBlobs;
    private readonly AuditActor actor;
    private readonly AuditTrail auditTrail;

    public ExtractDocumentControlArchiveHandler(
        JpmsContext context, IDocumentControlBlobStore documentBlobs, AuditActor actor, AuditTrail auditTrail)
    {
        this.context = context; this.documentBlobs = documentBlobs;
        this.actor = actor; this.auditTrail = auditTrail;
    }

    public async Task<IReadOnlyList<DocumentControlItem>> HandleAsync(
        ExtractDocumentControlArchive command, CancellationToken cancellationToken)
    {
        var item = await context.DocumentControlItems
            .FirstOrDefaultAsync(row => row.DocumentControlItemId == command.DocumentControlItemId, cancellationToken)
            ?? throw new InvalidOperationException("That document is no longer in Document Triage.");
        if (item.Status != (int)DocumentControlStatus.Pending)
            throw new InvalidOperationException("That document has already been filed or discarded — restore it to the queue first.");
        if (!ArchiveEntryScreen.LooksLikeZip(item.FileName, item.ContentType))
            throw new InvalidOperationException("Only zip archives can be extracted.");

        using var archive = await OpenArchiveAsync(item.BlobRef, cancellationToken);
        var entries = archive.Entries.Where(ArchiveEntryScreen.IsExtractable).ToList();
        ArchiveEntryScreen.GuardLimits(entries);

        var alreadyExtracted = await context.DocumentControlItems
            .Where(row => row.MessageId == item.MessageId && row.SourceDocumentControlItemId == item.DocumentControlItemId)
            .Select(row => row.AttachmentId)
            .ToListAsync(cancellationToken);

        var created = new List<DocumentControlItemEntity>();
        long totalBytesExtracted = 0;
        foreach (var entry in entries)
        {
            var childAttachmentId = Truncate($"{item.AttachmentId}!{entry.FullName}", AttachmentIdMaximumLength);
            if (alreadyExtracted.Contains(childAttachmentId)) continue;
            var child = await CreateChildAsync(item, entry, childAttachmentId, cancellationToken);
            totalBytesExtracted += child.FileSizeBytes;
            if (totalBytesExtracted > ArchiveEntryScreen.MaximumTotalBytes)
                throw new InvalidOperationException("The archive unpacks to more than 500 MB — extract it locally instead.");
            created.Add(child);
        }

        item.Status = (int)DocumentControlStatus.Filed;
        item.ResolvedBy = actor.Email;
        item.ResolvedAt = DateTimeOffset.UtcNow;
        item.FiledAsKind = (int)DocumentFiledAs.ArchiveExtracted;
        item.FiledLabel = created.Count == 1
            ? "Extracted 1 file into the queue"
            : $"Extracted {created.Count} files into the queue";

        await context.SaveChangesAsync(cancellationToken);

        await auditTrail.WriteAsync(
            AuditEventType.DocumentArchiveExtracted,
            $"Extracted {created.Count} file(s) from \"{item.FileName}\" into Document Triage",
            projectId: item.ProjectIdHint,
            emailMessageId: item.MessageId,
            internetMessageId: item.InternetMessageId,
            cancellationToken: cancellationToken);

        return created.Select(entity => entity.ToModel()).ToList();
    }

    private async Task<ZipArchive> OpenArchiveAsync(string blobRef, CancellationToken cancellationToken)
    {
        var blob = await documentBlobs.OpenAsync(blobRef, cancellationToken)
            ?? throw new InvalidOperationException("The stored file could not be found in Document Triage's storage.");
        await using var content = blob.Content;
        var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        try { return new ZipArchive(buffer, ZipArchiveMode.Read); }
        catch (InvalidDataException) { throw new InvalidOperationException("That file is not a readable zip archive."); }
    }

    private async Task<DocumentControlItemEntity> CreateChildAsync(
        DocumentControlItemEntity parent, ZipArchiveEntry entry, string childAttachmentId,
        CancellationToken cancellationToken)
    {
        var child = ExtractedArchiveChildFactory.Build(parent, entry, childAttachmentId, actor.Email);
        // Bounded decompression — the declared-size guard above can be lied to by the header.
        using var buffer = await ArchiveEntryScreen.ReadEntryBoundedAsync(entry, cancellationToken);
        child.FileSizeBytes = buffer.Length;
        child.BlobRef = await documentBlobs.UploadItemAsync(
            child.DocumentControlItemId, child.FileName, child.ContentType, buffer, cancellationToken);
        context.DocumentControlItems.Add(child);
        return child;
    }

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];
}
