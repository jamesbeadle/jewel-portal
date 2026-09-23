using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Forms;

/// <summary>Sends one form to one named person as a link that is theirs alone. SentByEmail and SentByName are stamped server-side.</summary>
public sealed record SendFormInvite(
    string FormSlug,
    string PersonName,
    string Email,
    string CompanyName,
    int Days,
    string Reason,
    string SentByEmail = "",
    string SentByName = "") : ICommand<SentFormLink>;

/// <summary>A fresh link and a fresh expiry for one form; the old link stops working, so a forwarded email cannot be used later.</summary>
public sealed record ResendFormInvite(
    string FormInviteId,
    string Email,
    string SentByEmail = "",
    string SentByName = "") : ICommand<SentFormLink>;

public sealed record CancelFormInvite(string FormInviteId) : ICommand<FormInvite>;

/// <summary>Issues a new starter's pack: the portal decides the forms from how they are engaged and the four answers.</summary>
public sealed record SendFormPack(
    string PersonName,
    string Email,
    Engagement EngagedAs,
    FormPackAnswers Answers,
    string SentByEmail = "",
    string SentByName = "") : ICommand<SentFormPack>;

/// <summary>Chases a pack: a new link with a fresh fourteen days, emailed again; what is already done stays done.</summary>
public sealed record ChaseFormPack(string FormPackId, string SentByEmail = "", string SentByName = "") : ICommand<SentFormPack>;

public sealed record CancelFormPack(string FormPackId) : ICommand<FormPack>;

/// <summary>
/// What sending did. The link exists and works whether or not the email went; when it did not,
/// the link comes back so the office can send it themselves rather than being told it failed.
/// </summary>
public sealed record SentFormLink(FormInvite Invite, bool IsEmailed, string LinkWhenNotEmailed, string EmailProblem);

public sealed record SentFormPack(FormPack Pack, bool IsEmailed, string LinkWhenNotEmailed, string EmailProblem);
