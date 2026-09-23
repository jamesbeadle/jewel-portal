using Jewel.JPMS.Api.Features.Forms.Mapping;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Links;

/// <summary>Kills a link (api/invite.js op:'cancel'). A form already sent back cannot be cancelled — it is done.</summary>
public sealed class CancelFormInviteHandler : ICommandHandler<CancelFormInvite, FormInvite>
{
    private readonly JpmsContext context;

    public CancelFormInviteHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<FormInvite> HandleAsync(CancelFormInvite command, CancellationToken cancellationToken)
    {
        var invite = await context.FormInvites.FirstOrDefaultAsync(row => row.FormInviteId == command.FormInviteId, cancellationToken)
            ?? throw new InvalidOperationException("That link no longer exists.");
        if (invite.UsedAt is not null) throw new InvalidOperationException("That form has already been sent back.");
        invite.CancelledAt ??= DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return invite.ToModel();
    }
}

public sealed class CancelFormInviteAuthorisation
{
    public bool Allows(SignedInUser user, CancelFormInvite command) => FormRoleSets.Office.IncludesAny(user.Roles);
}

public sealed class CancelFormInviteValidation
{
    public ValidationOutcome Check(CancelFormInvite command) =>
        string.IsNullOrWhiteSpace(command.FormInviteId) ? ValidationOutcome.Failed("Which link?") : ValidationOutcome.Passed;
}
