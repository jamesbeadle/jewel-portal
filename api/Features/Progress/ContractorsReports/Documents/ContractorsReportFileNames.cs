using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

/// <summary>"JBB-2026-001 - Contractor's Report No. 30 - w-e 10 Sep 2026.pdf".</summary>
public static class ContractorsReportFileNames
{
    public const string PdfContentType = "application/pdf";
    public const string WordContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public static string Pdf(ContractorsReportHeader header) => Named(header, "pdf");
    public static string Word(ContractorsReportHeader header) => Named(header, "docx");

    private static string Named(ContractorsReportHeader header, string extension) =>
        Sanitised($"{header.ProjectReference} - {header.DocumentTitle} - w-e {ContractorsReportText.Date(header.PeriodEnd)}.{extension}");

    private static string Sanitised(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }
}
