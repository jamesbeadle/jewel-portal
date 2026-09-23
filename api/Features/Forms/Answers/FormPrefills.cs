namespace Jewel.JPMS.Api.Features.Forms.Answers;

/// <summary>
/// The answers a linked form opens with, left editable on purpose: if the office typed the name
/// wrong the person must be able to correct it, and the evidential record is the invite row, not the
/// box they typed in. Across a pack the person types their details once — a name, mobile, email or
/// address sent on an earlier form of the pack opens filled in on the next. Nothing sensitive is
/// carried, and only questions the form actually asks are filled.
/// </summary>
internal static class FormPrefills
{
    private static readonly string[] WholeNameKeys = { "contact_name", "full_name", "name" };
    private static readonly string[] CarriedKeys = { "mobile", "email", "address", "start_date" };

    public static IReadOnlyDictionary<string, string> For(
        FormDefinition form, JewelCompany company, string personName, string companyName, string email,
        IReadOnlyDictionary<string, string> carried)
    {
        var name = CarriedName(carried) ?? personName.Trim();
        var prefills = new Dictionary<string, string>();
        foreach (var key in WholeNameKeys) prefills[key] = name;
        foreach (var key in CarriedKeys) prefills[key] = carried.GetValueOrDefault(key, "");
        prefills["email"] = email;
        prefills["company"] = CompanyAnswer(form, company, companyName);
        SplitName(name, prefills);
        return prefills
            .Where(pair => pair.Value.Length > 0 && form.QuestionFor(pair.Key) is not null)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private static string? CarriedName(IReadOnlyDictionary<string, string> carried)
    {
        var split = $"{carried.GetValueOrDefault("first_name", "")} {carried.GetValueOrDefault("last_name", "")}".Trim();
        var whole = WholeNameKeys.Select(key => carried.GetValueOrDefault(key, "")).FirstOrDefault(value => value.Length > 0);
        return whole ?? (split.Length > 0 ? split : null);
    }

    private static string CompanyAnswer(FormDefinition form, JewelCompany company, string companyName)
    {
        var question = form.QuestionFor("company");
        var isAJewelCompanyChoice = question is { Kind: FormQuestionKind.Choice };
        if (!isAJewelCompanyChoice) return companyName.Trim();
        var shortName = JewelCompanies.For(company).ShortName;
        return question!.Choices.FirstOrDefault(choice => choice.StartsWith(shortName, StringComparison.OrdinalIgnoreCase)) ?? "";
    }

    private static void SplitName(string name, Dictionary<string, string> prefills)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var hasASurname = parts.Length > 1;
        prefills["first_name"] = hasASurname ? string.Join(' ', parts[..^1]) : parts.FirstOrDefault() ?? "";
        prefills["last_name"] = hasASurname ? parts[^1] : "";
    }
}
