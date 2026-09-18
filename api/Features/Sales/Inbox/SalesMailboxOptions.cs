using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Sales.Inbox;

/// <summary>
/// The sales mailbox — sales@jewelbb.co.uk by default, app setting <c>SalesMailbox__Address</c>
/// to change it — read live through the SAME Graph app registration and credentials the projects
/// mailbox uses (MailboxIntake:TenantId / ClientId / ClientSecret), pointed at a second mailbox.
/// Nothing is tagged, moved or stored here: it is a window onto the Inbox, a thread view, a reply,
/// and a way to log an email on a lead. NOTE for the admin: the app registration is restricted by
/// an Exchange ApplicationAccessPolicy to the projects mailbox; the sales mailbox must be added to
/// that policy (docs/Requests-Mailbox-Setup-Checklist.md §3) or every read here answers 403 — the
/// page says so.
/// </summary>
public sealed class SalesMailboxOptions
{
    public string Address { get; set; } = "sales@jewelbb.co.uk";
    public bool Enabled { get; set; } = true;

    public static SalesMailboxOptions FromConfiguration(IConfiguration configuration)
    {
        var options = new SalesMailboxOptions();
        var address = configuration["SalesMailbox:Address"];
        if (!string.IsNullOrWhiteSpace(address)) options.Address = address.Trim();
        if (bool.TryParse(configuration["SalesMailbox:Enabled"], out var enabled)) options.Enabled = enabled;
        return options;
    }
}
