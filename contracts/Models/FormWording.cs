namespace Jewel.JPMS.Models;

/// <summary>Why a link cannot open its form. Unknown and replaced links read the same, so a stranger learns nothing.</summary>
public enum FormLinkProblem
{
    NotValid = 0,
    Used = 1,
    Expired = 2,
    PolicySuperseded = 3
}

public sealed record FormNotice(string Heading, string Body);

/// <summary>
/// The sentences the public forms and their emails say, carried from the dashboard with Jewel Bespoke
/// Build's own name and telephone number in them.
/// </summary>
public static class FormWording
{
    public const string Yes = "Yes";
    public const string No = "No";
    public const string ThankYou = "Thank you";
    public const string SentToTheOffice = "Your form has been sent to the office.";
    public const string ChangeByPhone = "If you need to change anything, ring the office rather than sending the form again.";

    public static readonly string[] WhatHappensNext =
    {
        "The office is alerted straight away and picks it up on the next working day.",
        "We check what you have sent and come back to you only if something is missing.",
        "Nothing else is needed from you in the meantime."
    };

    public static string GeneralPrivacy(string legalName) =>
        "The information and documents you provide are used for onboarding, safety and record keeping by "
        + legalName + ", stored confidentially, and not shared outside the business. Contact the office to "
        + "update or remove your details.";

    public static FormNotice DeadLink(FormLinkProblem problem) => problem switch
    {
        FormLinkProblem.Used => new("This form has already been sent",
            "This link has been used once and cannot be used again. If you need to change something you have "
            + $"already sent us, ring the office on {JewelBespokeBuild.Phone} rather than filling the form in twice."),
        FormLinkProblem.Expired => new("This link has expired",
            $"Links last a few days for security. Reply to the email we sent you, or ring the office on {JewelBespokeBuild.Phone}, "
            + "and we will send you a fresh one straight away."),
        FormLinkProblem.PolicySuperseded => new("This policy has been updated",
            "The policy this link was sent for has been replaced by a newer revision, so it can no longer be signed. "
            + $"Ring the office on {JewelBespokeBuild.Phone} and we will send you the current one."),
        _ => new("This link is not valid",
            "It may have been mistyped, or replaced by a newer one. Check the most recent email we sent you, "
            + $"or ring the office on {JewelBespokeBuild.Phone}.")
    };

    public static string DefaultReason(string formTitle) =>
        $"We need you to complete our {formTitle}. It only takes a few minutes and you can do it on your phone.";

    public static string SentToYou(string email, string sentBy) =>
        $"This form was sent to {email}{(sentBy.Length > 0 ? " by " + sentBy : "")}. We will email you a copy of what you send.";

    public const string CopyLeavesThingsOut =
        "Some details are left out of this copy for your own security, including any reference numbers and "
        + "identity documents. Keep this email. If anything above is wrong, ring the office on " + JewelBespokeBuild.Phone
        + " rather than filling the form in again.";

    public static string PleaseFillIn(string label) => "Please fill in: " + label;

    public static string NeedsAFile(string label) => label + " needs at least one file.";

    public static string PleaseTick(string label) => "Please tick: " + Shortened(label);

    public static string PleaseSign(string label) => "Please type your name and draw your signature for: " + label;

    public static string TooBig(string fileName) => fileName + " is too big (15MB max).";

    public static string TooLargeToSend(string fileName) => fileName + " is too large to send - email it to the office instead.";

    public static string UploadFailed(string fileName) => fileName + " failed - try again.";

    public const string NoSignal = "No signal - try again when you are back in range.";

    public const string TooManyFiles = "That is as many files as one form can take — email the rest to the office.";

    private const int ShortLabelLength = 80;

    private static string Shortened(string label) =>
        label.Length > ShortLabelLength ? label[..ShortLabelLength] + "…" : label;
}
