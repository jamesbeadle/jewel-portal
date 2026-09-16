namespace Jewel.JPMS.Api.Features.Progress.WhatsApp;

/// <summary>Whoever may record progress may bring a week of WhatsApp in — the same gate as the
/// Progress page's form.</summary>
public sealed class WhatsAppWeekAuthorisation
{
    public bool Allows(SignedInUser user) => ProgressRoles.Contributors.IncludesAny(user.Roles);
}
