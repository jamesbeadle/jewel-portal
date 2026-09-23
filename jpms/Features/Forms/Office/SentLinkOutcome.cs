using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>
/// What sending a link did, as the office reads it: emailed, or created and not emailed with the
/// link in hand to send by other means — never "failed" while the link works (api/invite.js).
/// </summary>
public sealed record SentLinkOutcome(string Email, bool IsEmailed, string Link, string EmailProblem)
{
    public static SentLinkOutcome Of(SentFormLink sent) =>
        new(sent.Invite.Email, sent.IsEmailed, sent.LinkWhenNotEmailed, sent.EmailProblem);

    public static SentLinkOutcome Of(SentFormPack sent) =>
        new(sent.Pack.Email, sent.IsEmailed, sent.LinkWhenNotEmailed, sent.EmailProblem);
}
