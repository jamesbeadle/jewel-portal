using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.ArchitectInstructions.Storage;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.ArchitectInstructions;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ArchitectInstructions;

// Query and command handlers for the Architect's Instruction register. Bundled the way the mailbox
// triage handlers are: each is a few lines over one table, and keeping them together makes the
// shared reference-minting and link-loading rules visible in one read.

public sealed class ListArchitectInstructionsForProjectHandler
    : IQueryHandler<ListArchitectInstructionsForProject, IReadOnlyList<ArchitectInstruction>>
{
    private readonly JpmsContext context;
    public ListArchitectInstructionsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ArchitectInstruction>> HandleAsync(
        ListArchitectInstructionsForProject query, CancellationToken cancellationToken)
    {
        var rows = await context.ArchitectInstructions
            .AsNoTracking()
            .Where(row => row.ProjectId == query.ProjectId)
            // Newest first, and the instruction's own date wins over when we happened to file it:
            // a January instruction keyed in during March still belongs in January's place.
            .OrderByDescending(row => row.InstructedAt ?? row.ReceivedAt)
            .ThenByDescending(row => row.Number)
            .ToListAsync(cancellationToken);

        var links = await ArchitectInstructionMapping.LoadLinksAsync(
            context, rows.Select(row => row.ArchitectInstructionId).ToList(), cancellationToken);

        return rows
            .Select(row => row.ToModel(
                links.TryGetValue(row.ArchitectInstructionId, out var forRow)
                    ? forRow
                    : (IReadOnlyList<ArchitectInstructionVariationLink>)Array.Empty<ArchitectInstructionVariationLink>()))
            .ToList();
    }
}

public sealed class GetArchitectInstructionByIdHandler
    : IQueryHandler<GetArchitectInstructionById, ArchitectInstruction?>
{
    private readonly JpmsContext context;
    public GetArchitectInstructionByIdHandler(JpmsContext context) { this.context = context; }

    public async Task<ArchitectInstruction?> HandleAsync(
        GetArchitectInstructionById query, CancellationToken cancellationToken)
    {
        var row = await context.ArchitectInstructions
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.ArchitectInstructionId == query.ArchitectInstructionId, cancellationToken);
        if (row is null) return null;

        var links = await ArchitectInstructionMapping.LoadLinksForAsync(
            context, row.ArchitectInstructionId, cancellationToken);
        return row.ToModel(links);
    }
}

public sealed class RecordArchitectInstructionHandler
    : ICommandHandler<RecordArchitectInstruction, ArchitectInstruction>
{
    private readonly JpmsContext context;
    public RecordArchitectInstructionHandler(JpmsContext context) { this.context = context; }

    public async Task<ArchitectInstruction> HandleAsync(
        RecordArchitectInstruction command, CancellationToken cancellationToken)
    {
        // Per-project numbering, like variations and requests: AI-0001 means "the first instruction
        // on this project", not the first in the company.
        var nextNumber = (await context.ArchitectInstructions
            .Where(row => row.ProjectId == command.ProjectId)
            .MaxAsync(row => (int?)row.Number, cancellationToken) ?? 0) + 1;

        var title = string.IsNullOrWhiteSpace(command.Title)
            ? (string.IsNullOrWhiteSpace(command.InstructionRef)
                ? ArchitectInstructionIdentifierFactory.Reference(nextNumber)
                : command.InstructionRef.Trim())
            : command.Title.Trim();

        var entity = new ArchitectInstructionEntity
        {
            ArchitectInstructionId = command.ArchitectInstructionId,
            ProjectId = command.ProjectId,
            Number = nextNumber,
            Reference = ArchitectInstructionIdentifierFactory.Reference(nextNumber),
            InstructionRef = ClampRequired(command.InstructionRef?.Trim() ?? "", 128),
            Title = ClampRequired(title, 256),
            Notes = Clamp(command.Notes?.Trim(), 2048),
            InstructedAt = command.InstructedAt,
            ReceivedAt = DateTimeOffset.UtcNow,
            IssuedByEmail = ClampRequired(command.IssuedByEmail?.Trim() ?? "", 256),
            FiledByEmail = ClampRequired(command.FiledByEmail?.Trim() ?? "", 256),
            Source = (int)command.Source,
            FileName = Clamp(command.FileName, 256),
            ContentType = Clamp(command.ContentType, 128),
            FileSizeBytes = command.FileSizeBytes,
            BlobRef = command.BlobRef
        };
        context.ArchitectInstructions.Add(entity);

        foreach (var variationOrderId in Distinct(command.VariationOrderIds))
            context.ArchitectInstructionVariations.Add(new ArchitectInstructionVariationEntity
            {
                ArchitectInstructionVariationId = ArchitectInstructionIdentifierFactory.NextLinkId(),
                ArchitectInstructionId = entity.ArchitectInstructionId,
                VariationOrderId = variationOrderId,
                LinkedAt = DateTimeOffset.UtcNow,
                LinkedByEmail = entity.FiledByEmail
            });

        await context.SaveChangesAsync(cancellationToken);

        var links = await ArchitectInstructionMapping.LoadLinksForAsync(
            context, entity.ArchitectInstructionId, cancellationToken);
        return entity.ToModel(links);
    }

    internal static IEnumerable<string> Distinct(IReadOnlyList<string>? ids) =>
        (ids ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

    // Two names, not two overloads: nullable annotations are not part of a method signature, so a
    // string/string? pair would be the same member twice.
    internal static string ClampRequired(string value, int max) => value.Length <= max ? value : value[..max];
    internal static string? Clamp(string? value, int max) => value is null || value.Length <= max ? value : value[..max];
}

public sealed class ImportArchitectInstructionFromMessageHandler
    : ICommandHandler<ImportArchitectInstructionFromMessage, ArchitectInstruction>
{
    private readonly IIntakeMessageReader reader;
    private readonly IMailboxGraphClient mailbox;
    private readonly IArchitectInstructionBlobStore blobStore;
    // All persistence goes through the record handler, so the numbering and link rules live in
    // exactly one place rather than being repeated for the email path.
    private readonly ICommandHandler<RecordArchitectInstruction, ArchitectInstruction> record;

    public ImportArchitectInstructionFromMessageHandler(
        IIntakeMessageReader reader,
        IMailboxGraphClient mailbox,
        IArchitectInstructionBlobStore blobStore,
        ICommandHandler<RecordArchitectInstruction, ArchitectInstruction> record)
    {
        this.reader = reader;
        this.mailbox = mailbox;
        this.blobStore = blobStore;
        this.record = record;
    }

    public async Task<ArchitectInstruction> HandleAsync(
        ImportArchitectInstructionFromMessage command, CancellationToken cancellationToken)
    {
        var attachment = await reader.GetAttachmentAsync(command.MessageId, command.AttachmentId, cancellationToken);
        if (attachment is null)
            throw new InvalidOperationException(
                "Couldn't download that attachment from the mailbox — it may have been removed, or it isn't a file.");

        // The id is minted before the upload so the blob path and the persisted row agree, the same
        // way a drawing revision does it.
        var instructionId = ArchitectInstructionIdentifierFactory.NextArchitectInstructionId();
        string blobRef;
        using (var stream = new MemoryStream(attachment.Content, writable: false))
        {
            blobRef = await blobStore.UploadAsync(
                command.ProjectId, instructionId, attachment.Name, attachment.ContentType, stream, cancellationToken);
        }

        // The instruction was issued by whoever sent the email — the architect — not by the person
        // at Jewel who triaged it. Same rule as ImportDrawingFromMessageHandler.
        var snapshot = await mailbox.GetSnapshotAsync(command.MessageId, null, cancellationToken);

        return await record.HandleAsync(
            new RecordArchitectInstruction(
                instructionId,
                command.ProjectId,
                command.InstructionRef,
                command.Title,
                Notes: null,
                InstructedAt: command.InstructedAt ?? snapshot?.ReceivedAt,
                IssuedByEmail: snapshot?.FromEmail ?? "",
                FiledByEmail: "",
                Source: ArchitectInstructionSource.Email,
                FileName: attachment.Name,
                ContentType: attachment.ContentType,
                FileSizeBytes: attachment.Content.LongLength,
                BlobRef: blobRef,
                VariationOrderIds: command.VariationOrderIds),
            cancellationToken);
    }
}

public sealed class UpdateArchitectInstructionHandler
    : ICommandHandler<UpdateArchitectInstruction, ArchitectInstruction>
{
    private readonly JpmsContext context;
    public UpdateArchitectInstructionHandler(JpmsContext context) { this.context = context; }

    public async Task<ArchitectInstruction> HandleAsync(
        UpdateArchitectInstruction command, CancellationToken cancellationToken)
    {
        var entity = await context.ArchitectInstructions
            .FirstOrDefaultAsync(row => row.ArchitectInstructionId == command.ArchitectInstructionId, cancellationToken);
        if (entity is null)
            throw new InvalidOperationException($"Architect's Instruction '{command.ArchitectInstructionId}' not found.");

        entity.InstructionRef = RecordArchitectInstructionHandler.ClampRequired(command.InstructionRef?.Trim() ?? "", 128);
        entity.Title = RecordArchitectInstructionHandler.ClampRequired(
            string.IsNullOrWhiteSpace(command.Title) ? entity.Reference : command.Title.Trim(), 256);
        entity.Notes = RecordArchitectInstructionHandler.Clamp(command.Notes?.Trim(), 2048);
        entity.InstructedAt = command.InstructedAt;
        await context.SaveChangesAsync(cancellationToken);

        var links = await ArchitectInstructionMapping.LoadLinksForAsync(
            context, entity.ArchitectInstructionId, cancellationToken);
        return entity.ToModel(links);
    }
}

public sealed class LinkArchitectInstructionToVariationHandler
    : ICommandHandler<LinkArchitectInstructionToVariation, ArchitectInstruction>
{
    private readonly JpmsContext context;
    public LinkArchitectInstructionToVariationHandler(JpmsContext context) { this.context = context; }

    public async Task<ArchitectInstruction> HandleAsync(
        LinkArchitectInstructionToVariation command, CancellationToken cancellationToken)
    {
        var entity = await context.ArchitectInstructions
            .FirstOrDefaultAsync(row => row.ArchitectInstructionId == command.ArchitectInstructionId, cancellationToken);
        if (entity is null)
            throw new InvalidOperationException($"Architect's Instruction '{command.ArchitectInstructionId}' not found.");

        var variation = await context.VariationOrders
            .FirstOrDefaultAsync(row => row.VariationOrderId == command.VariationOrderId, cancellationToken);
        if (variation is null)
            throw new InvalidOperationException($"Variation '{command.VariationOrderId}' not found.");
        if (variation.ProjectId != entity.ProjectId)
            throw new InvalidOperationException("An instruction can only be linked to variations on the same project.");

        var already = await context.ArchitectInstructionVariations.AnyAsync(
            link => link.ArchitectInstructionId == command.ArchitectInstructionId
                 && link.VariationOrderId == command.VariationOrderId,
            cancellationToken);

        // Linking twice is a no-op rather than an error: two people reaching the same conclusion
        // about the same instruction is not a mistake worth stopping.
        if (!already)
        {
            context.ArchitectInstructionVariations.Add(new ArchitectInstructionVariationEntity
            {
                ArchitectInstructionVariationId = ArchitectInstructionIdentifierFactory.NextLinkId(),
                ArchitectInstructionId = command.ArchitectInstructionId,
                VariationOrderId = command.VariationOrderId,
                LinkedAt = DateTimeOffset.UtcNow,
                LinkedByEmail = ""
            });
            await context.SaveChangesAsync(cancellationToken);
        }

        // The variation is deliberately NOT moved off Awaiting AI here. Approving writes the
        // contract figures and needs a cost code and a value, so it stays a human decision on the
        // variation itself — the link is what tells that person the instruction has landed.
        var links = await ArchitectInstructionMapping.LoadLinksForAsync(
            context, entity.ArchitectInstructionId, cancellationToken);
        return entity.ToModel(links);
    }
}

public sealed class UnlinkArchitectInstructionFromVariationHandler
    : ICommandHandler<UnlinkArchitectInstructionFromVariation, ArchitectInstruction>
{
    private readonly JpmsContext context;
    public UnlinkArchitectInstructionFromVariationHandler(JpmsContext context) { this.context = context; }

    public async Task<ArchitectInstruction> HandleAsync(
        UnlinkArchitectInstructionFromVariation command, CancellationToken cancellationToken)
    {
        var entity = await context.ArchitectInstructions
            .FirstOrDefaultAsync(row => row.ArchitectInstructionId == command.ArchitectInstructionId, cancellationToken);
        if (entity is null)
            throw new InvalidOperationException($"Architect's Instruction '{command.ArchitectInstructionId}' not found.");

        var links = await context.ArchitectInstructionVariations
            .Where(link => link.ArchitectInstructionId == command.ArchitectInstructionId
                        && link.VariationOrderId == command.VariationOrderId)
            .ToListAsync(cancellationToken);
        if (links.Count > 0)
        {
            context.ArchitectInstructionVariations.RemoveRange(links);
            await context.SaveChangesAsync(cancellationToken);
        }

        var remaining = await ArchitectInstructionMapping.LoadLinksForAsync(
            context, entity.ArchitectInstructionId, cancellationToken);
        return entity.ToModel(remaining);
    }
}

public sealed class DeleteArchitectInstructionHandler
    : ICommandHandler<DeleteArchitectInstruction, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly IArchitectInstructionBlobStore blobStore;

    public DeleteArchitectInstructionHandler(JpmsContext context, IArchitectInstructionBlobStore blobStore)
    {
        this.context = context;
        this.blobStore = blobStore;
    }

    public async Task<Acknowledgement> HandleAsync(
        DeleteArchitectInstruction command, CancellationToken cancellationToken)
    {
        var entity = await context.ArchitectInstructions
            .FirstOrDefaultAsync(row => row.ArchitectInstructionId == command.ArchitectInstructionId, cancellationToken);
        if (entity is null) return new Acknowledgement(command.ArchitectInstructionId);

        var links = await context.ArchitectInstructionVariations
            .Where(link => link.ArchitectInstructionId == command.ArchitectInstructionId)
            .ToListAsync(cancellationToken);
        context.ArchitectInstructionVariations.RemoveRange(links);
        context.ArchitectInstructions.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        // The row is gone either way — a stranded blob is tidier than a register row pointing at a
        // file nobody can reach, so a storage failure here is swallowed rather than surfaced.
        if (!string.IsNullOrWhiteSpace(entity.BlobRef))
        {
            try { await blobStore.DeleteAsync(entity.BlobRef, cancellationToken); }
            catch (Exception ex) when (ex is not OperationCanceledException) { }
        }

        return new Acknowledgement(command.ArchitectInstructionId);
    }
}
