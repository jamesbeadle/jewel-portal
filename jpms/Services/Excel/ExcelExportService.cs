using Microsoft.JSInterop;

namespace Jewel.JPMS.Services.Excel;

/// <summary>
/// Builds an .xlsx from an <see cref="ExcelWorkbook"/> and hands it to the browser
/// as a file download. The date is stamped into the filename so repeated exports
/// don't shadow each other in the user's downloads folder.
/// </summary>
public sealed class ExcelExportService
{
    private readonly IJSRuntime js;

    public ExcelExportService(IJSRuntime js) => this.js = js;

    public async Task DownloadAsync(ExcelWorkbook workbook, string baseFileName)
    {
        var bytes = ExcelWorkbookWriter.Write(workbook);
        var fileName = $"{SanitizeFileName(baseFileName)} {DateTime.Now:yyyy-MM-dd}.xlsx";
        await js.InvokeVoidAsync("jpmsExcelExport.download", fileName, Convert.ToBase64String(bytes));
    }

    private static string SanitizeFileName(string name)
    {
        var cleaned = new string(name.Select(ch => char.IsControl(ch) || "\\/:*?\"<>|".Contains(ch) ? ' ' : ch).ToArray()).Trim();
        return cleaned.Length == 0 ? "export" : cleaned;
    }
}
