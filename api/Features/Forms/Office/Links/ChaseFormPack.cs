using Jewel.JPMS.Api.Features.Forms.Links;
using Jewel.JPMS.Api.Features.Forms.Mail;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Links;

/// <summary>
/// Chasing a pack from the office's one screen of packs: a new link with a fresh fourteen days,
/// emailed as a reminder listing only what is still outstanding. The old link stops working; what is
/// already done stays done.
/// </summary>
public sealed class ChaseFormPackHandler : ICommandHandler<ChaseFormPack, SentFormPack>
{
    private readonly JpmsContext context;
    private readonly IFormMailer mailer;
    private readonly FormSiteOptions options;

    public ChaseFormPackHandler(JpmsContext context, IFormMailer mailer, FormSiteOptions options)
    {
        this.context = context;
        this.mailer = mailer;
        this.options = options;
    }

    public async Task<SentFormPack> HandleAsync(ChaseFormPack command, CancellationToken cancellationToken)
    {
        var pack = await context.FormPacks.FirstOrDefaultAsync(row => row.FormPackId == command.FormPackId, cancellationToken)
            ?? throw new InvalidOperationException("That pack no longer exists.");
        if (pack.CancelledAt is not null) throw new InvalidOperationException("That pack was cancelled.");
        if (pack.CompletedAt is not null) throw new InvalidOperationException("That pack is complete — every form is in.");
        var invites = await context.FormInvites
            .Where(row => row.FormPackId == pack.FormPackId && row.CancelledAt == null)
            .ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var token = FormTokens.NewSecret();
        FormPackLife.Resend(pack, invites, FormTokens.Hash(token), now);
        pack.LastChasedAt = now;
        pack.ChaseCount += 1;
        await context.SaveChangesAsync(cancellationToken);
        return await FormLinkMailing.SendPackAsync(mailer, options, pack, invites, token, command.SentByName, true, cancellationToken);
    }
}

public sealed class ChaseFormPackAuthorisation
{
    public bool Allows(SignedInUser user, ChaseFormPack command) => FormRoleSets.Office.IncludesAny(user.Roles);
}

public sealed class ChaseFormPackValidation
{
    public ValidationOutcome Check(ChaseFormPack command) =>
        string.IsNullOrWhiteSpace(command.FormPackId) ? ValidationOutcome.Failed("Which pack?") : ValidationOutcome.Passed;
}
