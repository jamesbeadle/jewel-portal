namespace Jewel.JPMS.Api.Features.Forms.Filing;

/// <summary>
/// Who a form is from and what it is filed under. With a one-time link the person it was sent to is
/// the identity, not whatever was typed in the name box; without one it is the name the form's own
/// filing questions give — first and last names joined for the starter checklist, the company for a
/// questionnaire. A company is filed under the company, a person under the name they gave.
/// </summary>
internal static class FormFilingNames
{
    public const string Unknown = "Unknown";
    private const string FirstNameKey = "first_name";
    private const string LastNameKey = "last_name";

    public static string SubmitterName(FormDefinition form, IReadOnlyDictionary<string, string> answers, string? invitedPerson) =>
        Named(invitedPerson ?? "", TypedName(form, answers));

    public static string FilingName(FormDefinition form, IReadOnlyDictionary<string, string> answers, string? invitedPerson) =>
        Named(TypedName(form, answers), invitedPerson ?? "");

    public static string Normalised(string name) =>
        string.Join(' ', name.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string TypedName(FormDefinition form, IReadOnlyDictionary<string, string> answers)
    {
        var isSplitName = form.FilingKeys.Contains(FirstNameKey);
        if (isSplitName) return $"{answers.GetValueOrDefault(FirstNameKey, "")} {answers.GetValueOrDefault(LastNameKey, "")}".Trim();
        return form.FilingKeys.Select(key => answers.GetValueOrDefault(key, "").Trim()).FirstOrDefault(value => value.Length > 0) ?? "";
    }

    private static string Named(string preferred, string fallback)
    {
        var chosen = preferred.Trim().Length > 0 ? preferred.Trim() : fallback.Trim();
        return chosen.Length > 0 ? chosen : Unknown;
    }
}
