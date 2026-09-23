using Jewel.JPMS.Api.Features.Forms.Mapping;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Links;

/// <summary>Kills a pack's link and every form still outstanding on it — a start that fell through. What was sent stays on file.</summary>
public sealed class CancelFormPackHandler : ICommandHandler<CancelFormPack, FormPack>
{
    private readonly JpmsContext context;

    public CancelFormPackHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<FormPack> HandleAsync(CancelFormPack command, CancellationToken cancellationToken)
    {
        var pack = await context.FormPacks.FirstOrDefaultAsync(row => row.FormPackId == command.FormPackId, cancellationToken)
            ?? throw new InvalidOperationException("That pack no longer exists.");
        var invites = await context.FormInvites.Where(row => row.FormPackId == pack.FormPackId).ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        pack.CancelledAt ??= now;
        foreach (var invite in invites.Where(invite => invite.UsedAt is null)) invite.CancelledAt ??= now;
        await context.SaveChangesAsync(cancellationToken);
        return pack.ToModel(invites);
    }
}

public sealed class CancelFormPackAuthorisation
{
    public bool Allows(SignedInUser user, CancelFormPack command) => FormRoleSets.Office.IncludesAny(user.Roles);
}

public sealed class CancelFormPackValidation
{
    public ValidationOutcome Check(CancelFormPack command) =>
        string.IsNullOrWhiteSpace(command.FormPackId) ? ValidationOutcome.Failed("Which pack?") : ValidationOutcome.Passed;
}
