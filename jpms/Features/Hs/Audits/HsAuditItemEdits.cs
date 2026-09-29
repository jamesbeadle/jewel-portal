using Jewel.JPMS.Contracts.Hs;
using System.Globalization;

namespace Jewel.JPMS.Features.Hs.Audits;

/// <summary>The date rectified as the date input reads and writes it — shared by the table row and
/// the phone card of an audit item.</summary>
public static class HsAuditItemEdits
{
    private const string DateInputFormat = "yyyy-MM-dd";

    public static string DateInput(DateTimeOffset? value) =>
        value is { } date ? date.ToString(DateInputFormat) : "";

    public static DateTimeOffset? ParseDate(ChangeEventArgs e)
    {
        var isADate = DateTime.TryParseExact(e.Value?.ToString(), DateInputFormat, null, DateTimeStyles.None, out var value);
        return isADate ? new DateTimeOffset(value, TimeSpan.Zero) : null;
    }

    public static string Rate(HsAuditRate rate) => ((int)rate).ToString();
}
