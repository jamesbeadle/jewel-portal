using System.Text.RegularExpressions;

namespace Jewel.JPMS.Models;

/// <summary>
/// The answers never echoed back — in the copy emailed to the person or in the alert to the office.
/// Emailing someone their own UTR, NI number, date of birth or share code adds a copy of it to a
/// mailbox Jewel does not control, for no benefit; conviction data is Article 10 data and does not
/// belong in an unencrypted email, even the person's own. The rule is the dashboard's SENSITIVE
/// pattern on the question key, word for word, so a new question named like one of these is covered
/// the day it is added, plus any question marked special category.
/// </summary>
public static class SensitiveAnswers
{
    private static readonly Regex SensitiveKey = new(
        "utr|ni_|_ni$|^ni$|nino|crn|dob|birth|passport|licence|license|share_?code|dvla|points|disqual|tacho|unspent"
            + "|conviction|eyesight|medical|health|disab|bank|sort|account_no|^sex$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool IsSensitiveKey(string key) => SensitiveKey.IsMatch(key);

    public static bool IsSensitive(FormQuestion question) => question.IsSpecialCategory || IsSensitiveKey(question.Key);
}
