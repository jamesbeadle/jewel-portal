using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Forms;

/// <summary>
/// Where the forms' links point and whom their emails go from and to. Every address is an app
/// setting (Forms__Sender, Forms__OfficeAlert, Forms__AccidentAlert — several addresses separated by
/// semicolons), so moving an alert is a setting, not a deploy. Unset, mail leaves from the portal's
/// own sender with the office's address as the Reply-To, and alerts go to that office address.
/// </summary>
public sealed class FormSiteOptions
{
    private const char AddressSeparator = ';';
    private const string PortalAddress = "https://portal.jewelbb.co.uk";
    private const string PortalSender = "DoNotReply@mail.jewelbb.co.uk";

    public string PublicSiteUrl { get; init; } = PortalAddress;
    public string DefaultSender { get; init; } = PortalSender;
    public IReadOnlyDictionary<string, string> SettingsByKey { get; init; } = new Dictionary<string, string>();

    public string Sender => Setting("Sender") ?? DefaultSender;

    public IReadOnlyList<string> OfficeAlert => Addresses(Setting("OfficeAlert") ?? JewelBespokeBuild.Email);

    public IReadOnlyList<string> AccidentAlert
    {
        get
        {
            var accidentAddresses = Addresses(Setting("AccidentAlert"));
            return accidentAddresses.Count > 0 ? accidentAddresses : OfficeAlert;
        }
    }

    public string FormLink(string slug, string token) => $"{Site}/f/{slug}?k={Uri.EscapeDataString(token)}";

    public string PackLink(string token) => $"{Site}/f/pack/{Uri.EscapeDataString(token)}";

    public string OfficeLink(string formSubmissionId) => $"{Site}/forms/received/{formSubmissionId}";

    private string Site => PublicSiteUrl.TrimEnd('/');

    private string? Setting(string name) => SettingsByKey.GetValueOrDefault(name);

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
