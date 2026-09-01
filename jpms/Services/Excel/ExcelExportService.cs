
namespace Jewel.JPMS.Services.Excel;

/// <summary>
/// Builds an .xlsx from an <see cref="ExcelWorkbook"/> and hands it to the browser
/// as a file download. A date is stamped into the filename so repeated exports don't
/// shadow each other in the user's downloads folder — today's by default, or the date the
/// caller supplies when the file must match a companion document (a snapshot's PDF carries
/// the day the snapshot was taken, so its spreadsheet does too).
/// </summary>
public sealed class ExcelExportService
{
    private readonly IJSRuntime js;

    public ExcelExportService(IJSRuntime js) => this.js = js;

    private const string DateStampFormat = "yyyy-MM-dd";

    public async Task DownloadAsync(ExcelWorkbook workbook, string baseFileName, DateTimeOffset? stampedOn = null)
    {
        var bytes = ExcelWorkbookWriter.Write(workbook);
        var stamp = (stampedOn ?? DateTimeOffset.Now).ToString(DateStampFormat);
        var fileName = $"{SanitizeFileName(baseFileName)} {stamp}.xlsx";
        await js.InvokeVoidAsync("jpmsExcelExport.download", fileName, Convert.ToBase64String(bytes));
    }

    private static string SanitizeFileName(string name)
    {
        var cleaned = new string(name.Select(ch => char.IsControl(ch) || "\\/:*?\"<>|".Contains(ch) ? ' ' : ch).ToArray()).Trim();
        return cleaned.Length == 0 ? "export" : cleaned;
    }
}
