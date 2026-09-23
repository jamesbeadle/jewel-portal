using System.Globalization;
using System.Text.RegularExpressions;

namespace Jewel.JPMS.Models;

/// <summary>
/// What filing a questionnaire or an insurance update to the directory proposes, as the dashboard's
/// op:'formrenewal' read it: a questionnaire's insurance certificates are public liability expiring on
/// its public liability date; an insurance update's certificate is the cover it names, expiring on
/// its renewal date. The company's other documents are offered unticked for the office to correct
/// before filing. A photo of someone's ID, a DBS check and a signature are about a person, not a
/// company's standing to work — and a compliance record is read far more widely than a form — so they
/// are never offered and never filed.
/// </summary>
public static class FormDirectoryFilingPlan
{
    private const string PublicLiability = "Public Liability";
    private static readonly string[] AboutAPerson = { "id_photo", "dbs" };
    private const decimal Million = 1_000_000m;
    private static readonly Regex Number = new(@"\d[\d,]*(\.\d+)?");

    public static IReadOnlyList<FormFilingSuggestion> For(FormSubmissionView view)
    {
        var isAQuestionnaire = view.Submission.FormSlug == FormSlugs.SubcontractorQuestionnaire;
        var insuranceKey = isAQuestionnaire ? "insurance_docs" : "certificate";
        var coverType = isAQuestionnaire ? PublicLiability : view.Answers.GetValueOrDefault("cover_type", "");
        var expiresOn = FormDates.Read(view.Answers.GetValueOrDefault(isAQuestionnaire ? "pl_expiry" : "expiry"));
        var cover = coverType == PublicLiability ? PoundsIn(view.Answers.GetValueOrDefault("limit", "")) : null;
        return view.Files
            .Where(file => file.DeletedAt is null && file.QuestionKey == insuranceKey)
            .Select(file => new FormFilingSuggestion(file.FormUploadId, file.FileName, true, KindFor(coverType), expiresOn, cover))
            .Concat(OtherFiles(view, insuranceKey))
            .ToList();
    }

    public static string KindFor(string coverType) => coverType switch
    {
        PublicLiability => "Public liability insurance",
        "Employers Liability" => "Employers liability insurance",
        "Product Liability" => "Product liability insurance",
        "Professional Indemnity" => "Professional indemnity insurance",
        "Contractors All Risks" => "Contractors all risks insurance",
        _ => "Insurance"
    };

    public static decimal? PoundsIn(string limit)
    {
        var numbers = Number.Matches(limit);
        if (numbers.Count != 1) return null;
        var figure = decimal.Parse(numbers[0].Value.Replace(",", ""), CultureInfo.InvariantCulture);
        var isInMillions = limit.Contains('m', StringComparison.OrdinalIgnoreCase);
        return isInMillions ? figure * Million : figure;
    }

    /// <summary>Whether a form's file may go on a company's compliance record: a company document, never a person's.</summary>
    public static bool IsFileable(FormDefinition form, string questionKey) =>
        form.QuestionFor(questionKey) is { Kind: FormQuestionKind.Upload } && !AboutAPerson.Contains(questionKey);

    private static IEnumerable<FormFilingSuggestion> OtherFiles(FormSubmissionView view, string insuranceKey)
    {
        var form = FormCatalogue.For(view.Submission.FormSlug);
        return view.Files
            .Where(file => file.DeletedAt is null && file.QuestionKey != insuranceKey && form is not null && IsFileable(form, file.QuestionKey))
            .Select(file => new FormFilingSuggestion(file.FormUploadId, file.FileName, false, "", null, null));
    }
}

/// <summary>One file a form could put on a directory company's compliance record, with what it is taken to be.</summary>
public sealed record FormFilingSuggestion(
    string FormUploadId, string FileName, bool IsInsurance, string Kind, DateOnly? ExpiresOn, decimal? PublicLiabilityCover);
