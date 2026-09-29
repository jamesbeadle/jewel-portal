namespace Jewel.JPMS.Api.Features.Manual.Commands;

/// <summary>The column checks the manual's commands share: a length, a required value, an email, the form slugs.</summary>
internal static class ManualFieldChecks
{
    public static void Text(List<string> errors, string field, string value, int limit, bool isRequired)
    {
        var text = value ?? "";
        if (isRequired && string.IsNullOrWhiteSpace(text)) errors.Add($"{field} is required.");
        if (text.Length > limit) errors.Add($"{field} is at most {limit} characters.");
    }

    public static void Identifier(List<string> errors, string manualModuleId)
    {
        if (string.IsNullOrWhiteSpace(manualModuleId)) errors.Add("ManualModuleId is required.");
    }

    public static ValidationOutcome IdentifierOnly(string manualModuleId)
    {
        var errors = new List<string>();
        Identifier(errors, manualModuleId);
        return Outcome(errors);
    }

    public static void Controls(List<string> errors, string ownerEmail, string approverEmail, IReadOnlyList<string> linkedFormSlugs, string linkedStandards)
    {
        Email(errors, "OwnerEmail", ownerEmail);
        Email(errors, "ApproverEmail", approverEmail);
        Text(errors, "LinkedStandards", linkedStandards, ManualLimits.LinkedStandards, isRequired: false);
        FormSlugs(errors, linkedFormSlugs);
    }

    public static ValidationOutcome Outcome(List<string> errors)
    {
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }

    private static void Email(List<string> errors, string field, string value)
    {
        var email = (value ?? "").Trim();
        if (email.Length == 0) return;
        if (email.Length > ManualLimits.Email) errors.Add($"{field} is at most {ManualLimits.Email} characters.");
        var hasOneAt = email.Count(character => character == '@') == 1;
        if (!hasOneAt) errors.Add($"{field} must be an email address.");
    }

    private static void FormSlugs(List<string> errors, IReadOnlyList<string> slugs)
    {
        var joined = ManualMapping.JoinSlugs(slugs ?? Array.Empty<string>());
        if (joined.Length > ManualLimits.LinkedFormSlugs) errors.Add($"LinkedFormSlugs is at most {ManualLimits.LinkedFormSlugs} characters.");
        foreach (var slug in ManualMapping.SplitSlugs(joined))
        {
            var isKnown = FormCatalogue.For(slug) is not null;
            if (!isKnown) errors.Add($"There is no form called {slug} — the form slugs are the portal's own.");
        }
    }
}
