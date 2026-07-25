namespace Jewel.JPMS.Api.Auth;

/// <summary>
/// Builds the "reset your password" email. Deliberately distinct from <see cref="InviteEmailBody"/>:
/// a reset lands in the inbox of somebody who already has an account, often because they asked for
/// it themselves, so the copy has to say who asked and what to do if it wasn't them. The link is
/// single-use and short-lived, which is the whole reason it is safe to email at all.
/// </summary>
public static class PasswordResetEmailBody
{
    public const string Subject = "Reset your Jewel JPMS password";

    public static string PlainText(string displayName, string resetLink) =>
        $"Hello {displayName},\n\n" +
        "Someone asked to reset the password on your Jewel Project Management System account. " +
        "Open the link below to choose a new one:\n\n" +
        $"{resetLink}\n\n" +
        $"This link can only be used once and expires in {ExpiryDescription}.\n\n" +
        "If you didn't ask for this, you can ignore this email — your current password still works.\n";

    public static string Html(string displayName, string resetLink) =>
        $"""
        <div style="font-family:Arial,Helvetica,sans-serif;font-size:15px;color:#0f172a;line-height:1.6">
          <p>Hello {displayName},</p>
          <p>Someone asked to reset the password on your <strong>Jewel Project Management System</strong> account. Choose a new one here:</p>
          <p style="margin:24px 0">
            <a href="{resetLink}" style="background:#0f172a;color:#ffffff;text-decoration:none;padding:12px 20px;border-radius:8px;display:inline-block">Reset your password</a>
          </p>
          <p style="font-size:13px;color:#475569">This link can only be used once and expires in {ExpiryDescription}.</p>
          <p style="font-size:13px;color:#475569">If you didn't ask for this, you can ignore this email — your current password still works.</p>
        </div>
        """;

    private static string ExpiryDescription
    {
        get
        {
            var lifetime = ResetSettings.ResetLifetime;
            return lifetime.TotalHours >= 2
                ? $"{(int)lifetime.TotalHours} hours"
                : $"{(int)lifetime.TotalMinutes} minutes";
        }
    }
}
