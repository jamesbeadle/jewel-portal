using System.Globalization;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>One file in the File to the directory dialog: whether it goes, what it is, when it expires and, for public liability, the cover.</summary>
public sealed class DirectoryFilingRow
{
    public DirectoryFilingRow(FormFilingSuggestion suggestion)
    {
        FormUploadId = suggestion.FormUploadId;
        FileName = suggestion.FileName;
        IsIncluded = suggestion.IsInsurance;
        Kind = suggestion.Kind;
        ExpiresOn = FormDates.Write(suggestion.ExpiresOn);
        Cover = suggestion.PublicLiabilityCover?.ToString(CultureInfo.InvariantCulture) ?? "";
    }

    public string FormUploadId { get; }
    public string FileName { get; }
    public bool IsIncluded { get; set; }
    public string Kind { get; set; }
    public string ExpiresOn { get; set; }
    public string Cover { get; set; }

    public FormUploadToFile ToFile() => new(FormUploadId, Kind.Trim(), FormDates.Read(ExpiresOn), CoverInPounds);

    private decimal? CoverInPounds =>
        decimal.TryParse(Cover, NumberStyles.Number, CultureInfo.InvariantCulture, out var pounds) ? pounds : null;
}
