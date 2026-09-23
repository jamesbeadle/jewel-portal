using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiFormsTools
{
    private static AiTool PacksTool() => new(
        "list_form_packs",
        "Every new starter's pack, newest first — the one screen of done and outstanding: formPackId, company, the person and "
        + "their email, how they are engaged, who sent it and when, when the link expires, how often it has been chased, and "
        + "each form in it with its state (NotOpened, Opened, Done, Expired, Replaced) and the formSubmissionId once sent. "
        + "chase_form_pack and cancel_form_pack take the formPackId.",
        AiToolSchema.Object(
            ("outstandingOnly", "boolean", "true lists only packs still owed a form (not complete, not cancelled).", false)),
        AiToolKind.Read,
        FormRoleSets.Office,
        PacksAsync);

    private static async Task<string> PacksAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var isOutstandingOnly = AiToolSchema.Flag(input, "outstandingOnly") == true;
        var packs = await Query<ListFormPacks, IReadOnlyList<FormPack>>(context, new ListFormPacks(), ct);
        var now = DateTimeOffset.UtcNow;
        var shown = packs.Where(pack => !isOutstandingOnly || pack.CancelledAt is null && pack.CompletedAt is null);
        return Serialise(new { ok = true, packs = shown.Select(pack => PackRow(pack, now)) });
    }

    private static object PackRow(FormPack pack, DateTimeOffset now) => new
    {
        pack.FormPackId, company = pack.Company.ToString(), pack.PersonName, pack.Email, engagedAs = pack.EngagedAs.ToString(),
        pack.SentByName, pack.SentAt, pack.ExpiresAt, pack.OpenedAt, pack.CompletedAt, pack.LastChasedAt, pack.ChaseCount,
        pack.CancelledAt, outstanding = pack.OutstandingForms.Count,
        forms = pack.CurrentForms.Select(form => InviteRow(form, now))
    };

    private static AiTool InvitesTool() => new(
        "list_form_invites",
        "Forms sent on their own to one named person, newest first (the Sent out screen): formInviteId, the form, company, "
        + "the person, their company and email, who sent it and when, the expiry, and its state (NotOpened, Opened, Done, "
        + "Expired, Replaced) with the formSubmissionId once used. resend_form_invite and cancel_form_invite take the formInviteId.",
        AiToolSchema.Empty(),
        AiToolKind.Read,
        FormRoleSets.Office,
        InvitesAsync);

    private static async Task<string> InvitesAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var invites = await Query<ListFormInvites, IReadOnlyList<FormInvite>>(context, new ListFormInvites(), ct);
        var now = DateTimeOffset.UtcNow;
        return Serialise(new { ok = true, invites = invites.Select(invite => InviteRow(invite, now)) });
    }

    private static object InviteRow(FormInvite invite, DateTimeOffset now) => new
    {
        invite.FormInviteId, invite.FormSlug, title = FormCatalogue.TitleOf(invite.FormSlug, invite.Company),
        company = invite.Company.ToString(), invite.PersonName, invite.CompanyName, invite.Email, invite.SentByName,
        invite.SentAt, invite.ExpiresAt, invite.OpenedAt, invite.UsedAt, invite.FormSubmissionId,
        state = invite.StateAt(now).ToString()
    };
}
