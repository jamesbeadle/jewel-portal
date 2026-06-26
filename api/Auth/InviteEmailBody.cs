namespace Jewel.JPMS.Api.Auth;

/// <summary>
/// Builds the invite email. The link is the only call to action; it is single-use and expires
/// after the invite window, so the copy keeps the recipient moving without extra steps.
/// </summary>
public static class InviteEmailBody
{
    public const string Subject = "Set your password for Jewel JPMS";

    public static string PlainText(string displayName, string inviteLink) =>
        $"Hello {displayName},\n\n" +
        "An administrator has invited you to the Jewel Project Management System. " +
        "Open the link below to choose your password and sign in:\n\n" +
        $"{inviteLink}\n\n" +
        $"This link can only be used once and expires in {ExpiryInDays} days.\n";

    public static string Html(string displayName, string inviteLink) =>
        $"""
        <div style="font-family:Arial,Helvetica,sans-serif;font-size:15px;color:#0f172a;line-height:1.6">
          <p>Hello {displayName},</p>
          <p>An administrator has invited you to the <strong>Jewel Project Management System</strong>. Choose your password to sign in:</p>
          <p style="margin:24px 0">
            <a href="{inviteLink}" style="background:#0f172a;color:#ffffff;text-decoration:none;padding:12px 20px;border-radius:8px;display:inline-block">Set your password</a>
          </p>
          <p style="font-size:13px;color:#475569">This link can only be used once and expires in {ExpiryInDays} days.</p>
        </div>
        """;

    private static int ExpiryInDays => (int)InviteSettings.InviteLifetime.TotalDays;
}
