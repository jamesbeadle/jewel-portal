using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Procurement;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>The composer's persisted draft, read back when the invite dialog opens — saved on the
/// PACKAGE so anyone on the team can pick a half-written invite up later, from any browser.</summary>
public sealed class GetBidPackageInviteComposerDraftHandler
    : IQueryHandler<GetBidPackageInviteComposerDraft, BidPackageInviteComposerDraft?>
{
    private readonly JpmsContext context;
    public GetBidPackageInviteComposerDraftHandler(JpmsContext context) { this.context = context; }

    public async Task<BidPackageInviteComposerDraft?> HandleAsync(
        GetBidPackageInviteComposerDraft query, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.AsNoTracking()
            .FirstOrDefaultAsync(row => row.BidPackageId == query.BidPackageId, cancellationToken);
        if (package is null || package.InviteDraftSavedAt is not { } savedAt) return null;

        return new BidPackageInviteComposerDraft(
            package.InviteDraftSubject ?? "",
            package.InviteDraftBody ?? "",
            package.InviteDraftTo ?? "",
            package.InviteDraftCc ?? "",
            package.InviteDraftBcc ?? "",
            savedAt);
    }
}

/// <summary>Saves (or overwrites) the composer draft. Whole-state on purpose: the draft is one
/// thing, and a partial save would leave a chimera nobody typed.</summary>
public sealed class SaveBidPackageInviteComposerDraftHandler
    : ICommandHandler<SaveBidPackageInviteComposerDraft, Acknowledgement>
{
    private readonly JpmsContext context;
    public SaveBidPackageInviteComposerDraftHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(
        SaveBidPackageInviteComposerDraft command, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken)
            ?? throw new InvalidOperationException($"Bid package {command.BidPackageId} not found.");

        package.InviteDraftSubject = command.Subject;
        package.InviteDraftBody = command.Body;
        package.InviteDraftTo = command.To;
        package.InviteDraftCc = command.Cc;
        package.InviteDraftBcc = command.Bcc;
        package.InviteDraftSavedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.BidPackageId);
    }
}
