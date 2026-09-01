using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Creates a Draft bid package from a tagged email and links the email to it. The email tag is the only
// association; the package reads its mail back live by tag (RecordEmailReader) for the conversation
// view and the later AI extraction. The package is persisted first because the shared link path
// resolves the record from the database.
public sealed class CreateBidPackageFromMessageHandler
    : ICommandHandler<CreateBidPackageFromMessage, BidPackage>
{
    private readonly JpmsContext context;
    private readonly ICommandHandler<LinkMessageToRecord, Acknowledgement> link;
    private readonly Jewel.JPMS.Api.Features.MailboxIntake.Graph.IMailboxGraphClient graph;

    public CreateBidPackageFromMessageHandler(
        JpmsContext context, ICommandHandler<LinkMessageToRecord, Acknowledgement> link,
        Jewel.JPMS.Api.Features.MailboxIntake.Graph.IMailboxGraphClient graph)
    { this.context = context; this.link = link; this.graph = graph; }

    public async Task<BidPackage> HandleAsync(CreateBidPackageFromMessage command, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project '{command.ProjectId}' not found.");

        // Pre-flight the cross-pathway confirm BEFORE the package persists (CrossPathwayGuard,
        // 2026-08-22): a bid package files the thread under Subcontractor, and rejecting after the
        // create left an orphaned package behind and a red error the triager couldn't confirm past.
        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken)
            ?? throw new InvalidOperationException("The email could not be read from the mailbox.");
        Jewel.JPMS.Api.Features.RecordLinks.CrossPathwayGuard.EnsureConfirmed(
            snapshot.Categories,
            Jewel.JPMS.Api.Features.MailboxIntake.Graph.TriageCategories.BucketFor(RecordType.BidPackageInvite),
            command.AllowCrossPathway, "the new bid package");

        var entity = new BidPackageEntity
        {
            BidPackageId = ProcurementIdentifierFactory.NextBidPackageId(),
            ProjectId = command.ProjectId,
            Title = Clamp(command.Title, 256),
            Trade = Clamp(command.Trade, 64),
            Status = (int)BidPackageStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            OwnerEmail = command.OwnerEmail,
            Number = (await context.BidPackages.MaxAsync(p => (int?)p.Number, cancellationToken) ?? 0) + 1
        };
        context.BidPackages.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        // Tag the originating email to the new package through the shared record-link path (verified by
        // read-back inside the handler). Throws if the email can't be read/tagged.
        await link.HandleAsync(
            new LinkMessageToRecord(
                command.MessageId, RecordType.BidPackageInvite, entity.BidPackageId, command.InternetMessageId,
                AllowCrossPathway: command.AllowCrossPathway,
                Scope: command.Scope),
            cancellationToken);

        return entity.ToModel();
    }

    // Email subjects can exceed the column limits; clamp so a long subject can't throw on save.
    private static string Clamp(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
