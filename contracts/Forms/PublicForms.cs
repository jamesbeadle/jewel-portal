using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Forms;

/// <summary>Who a one-time link was sent to, and the answers the form starts with (left editable on purpose).</summary>
public sealed record PublicFormInvitation(
    string PersonName,
    string CompanyName,
    string Email,
    string SentByName,
    IReadOnlyDictionary<string, string> Prefills);

/// <summary>A public form as its page opens it: the form, who it is for when a link names them, or why the link is dead.</summary>
public sealed record PublicFormView(
    string FormSlug,
    PublicFormInvitation? Invitation,
    FormLinkProblem? Problem);

public sealed record PublicPackForm(string FormSlug, bool IsDone);

/// <summary>A new starter's pack behind its one link: what is done and what is left, or why the link is dead.</summary>
public sealed record PublicPackView(
    string PersonName,
    IReadOnlyList<PublicPackForm> Forms,
    FormLinkProblem? Problem);

/// <summary>One file, posted on its own before the form is sent, so a big photo never holds the answers hostage.</summary>
public sealed record PublicFormUpload(string SessionId, string QuestionKey, string FileName, string Base64);

public sealed record PublicFormUploadReceipt(string FormUploadId, string FileName);

/// <summary>The answers by question key (a signature's typed name under its own key), and the uploads by question key.</summary>
public sealed record PublicFormSubmission(
    string SessionId,
    IReadOnlyDictionary<string, string> Answers,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Uploads,
    string? InviteToken,
    string? PackToken);

public sealed record PublicFormReceipt(string FormSubmissionId, bool IsVerified);
