using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Links;
using Jewel.JPMS.Api.Features.Forms.Mail;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Links;

/// <summary>
/// A new starter's pack: the office says who, which company, how they are engaged and four answers,
/// and the portal decides the forms (FormPackPlanner) rather than asking anyone to remember. One
/// link; one invite row per form behind it, so each form keeps its own opened, used and expired state.
/// </summary>
public sealed class SendFormPackHandler : ICommandHandler<SendFormPack, SentFormPack>
{
    private readonly JpmsContext context;
    private readonly IFormMailer mailer;
    private readonly FormSiteOptions options;

    public SendFormPackHandler(JpmsContext context, IFormMailer mailer, FormSiteOptions options)
    {
        this.context = context;
        this.mailer = mailer;
        this.options = options;
    }

    public async Task<SentFormPack> HandleAsync(SendFormPack command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var token = FormTokens.NewSecret();
        var pack = NewPack(command, FormTokens.Hash(token), now);
        var recipient = new FormLinkRecipient(command.Company, command.PersonName, "", command.Email);
        var sender = new FormLinkSender(command.SentByEmail, command.SentByName);
        var invites = FormPackPlanner.FormsFor(command.EngagedAs, command.Answers)
            .Select(slug => FormInviteRows.New(slug, recipient, sender, now, pack.ExpiresAt, "", pack.FormPackId).Invite)
            .ToList();
        context.FormPacks.Add(pack);
        context.FormInvites.AddRange(invites);
        await context.SaveChangesAsync(cancellationToken);
        return await FormLinkMailing.SendPackAsync(mailer, options, pack, invites, token, command.SentByName, false, cancellationToken);
    }

    private static FormPackEntity NewPack(SendFormPack command, string tokenHash, DateTimeOffset now) => new()
    {
        FormPackId = FormIdentifierFactory.NextId(),
        Company = (int)command.Company,
        PersonName = command.PersonName.Trim(),
        Email = command.Email.Trim(),
        EngagedAs = (int)command.EngagedAs,
        HasP45 = command.Answers.HasP45,
        IsWorkingAtAScreen = command.Answers.IsWorkingAtAScreen,
        IsGettingAVehicle = command.Answers.IsGettingAVehicle,
        MustHoldATicket = command.Answers.MustHoldATicket,
        TokenHash = tokenHash,
        ExpiresAt = now + FormLinkLifetimes.PackOnSending,
        SentByEmail = command.SentByEmail,
        SentByName = command.SentByName,
        SentAt = now
    };
}

public sealed class SendFormPackAuthorisation
{
    public bool Allows(SignedInUser user, SendFormPack command) => FormRoleSets.Office.IncludesAny(user.Roles);
}

public sealed class SendFormPackValidation
{
    public ValidationOutcome Check(SendFormPack command)
    {
        var errors = new List<string>();
        if (!Enum.IsDefined(command.Company)) errors.Add("Say which Jewel company they are joining.");
        if (!Enum.IsDefined(command.EngagedAs)) errors.Add("Say whether they are an employee or self-employed.");
        if (string.IsNullOrWhiteSpace(command.PersonName)) errors.Add("Who is it for?");
        if (!FormInviteRows.IsAnEmailAddress(command.Email ?? "")) errors.Add("That email address does not look right.");
        if (command.Answers is null) errors.Add("Answer the four questions about the role.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
