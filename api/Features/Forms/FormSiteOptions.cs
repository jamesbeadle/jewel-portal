using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Forms;

/// <summary>
/// Where the forms' links point and whom their emails go from and to, per Jewel company. Every
/// address is an app setting (Forms__Sender__jps, Forms__OfficeAlert__jbb, Forms__AccidentAlert__jps —
/// several addresses separated by semicolons), so moving an alert or adding a sending domain for
/// Jewel Property Serve is a setting, not a deploy. Unset, mail leaves from the portal's own sender
/// with the company's office address as the Reply-To, and alerts go to that office address.
/// </summary>
public sealed class FormSiteOptions
{
    private const char AddressSeparator = ';';
    private const string PortalAddress = "https://portal.jewelbb.co.uk";
    private const string PortalSender = "DoNotReply@mail.jewelbb.co.uk";

    public string PublicSiteUrl { get; init; } = PortalAddress;
    public string DefaultSender { get; init; } = PortalSender;
    public IReadOnlyDictionary<string, string> SettingsByKey { get; init; } = new Dictionary<string, string>();

    public string SenderFor(JewelCompany company) => Setting("Sender", company) ?? DefaultSender;

    public IReadOnlyList<string> OfficeAlertFor(JewelCompany company) =>
        Addresses(Setting("OfficeAlert", company) ?? JewelCompanies.For(company).Email);

    public IReadOnlyList<string> AccidentAlertFor(JewelCompany company)
    {
        var accidentAddresses = Addresses(Setting("AccidentAlert", company));
        return accidentAddresses.Count > 0 ? accidentAddresses : OfficeAlertFor(company);
    }

    public string FormLink(JewelCompany company, string slug, string token) =>
        $"{Site}/f/{JewelCompanies.For(company).Code}/{slug}?k={Uri.EscapeDataString(token)}";

    public string PackLink(JewelCompany company, string token) =>
        $"{Site}/f/{JewelCompanies.For(company).Code}/pack/{Uri.EscapeDataString(token)}";

    public string OfficeLink(string formSubmissionId) => $"{Site}/forms/received/{formSubmissionId}";

    private string Site => PublicSiteUrl.TrimEnd('/');

    private string? Setting(string name, JewelCompany company) =>
        SettingsByKey.GetValueOrDefault($"{name}:{JewelCompanies.For(company).Code}");

    private static IReadOnlyList<string> Addresses(string? setting) =>
        (setting ?? "").Split(AddressSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static FormSiteOptions FromConfiguration(IConfiguration configuration)
    {
        var settings = configuration.GetSection("Forms").AsEnumerable(makePathsRelative: true)
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value!, StringComparer.OrdinalIgnoreCase);
        var site = configuration["PublicSiteUrl"];
        var sender = configuration["InviteEmailSender"];
        return new FormSiteOptions
        {
            PublicSiteUrl = string.IsNullOrWhiteSpace(site) ? PortalAddress : site,
            DefaultSender = string.IsNullOrWhiteSpace(sender) ? PortalSender : sender,
            SettingsByKey = settings
        };
    }
}
