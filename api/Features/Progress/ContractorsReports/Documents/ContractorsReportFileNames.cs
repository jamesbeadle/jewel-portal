using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

/// <summary>"JBB-2026-001 - Contractor's Report No. 30 - w-e 10 Sep 2026.pdf".</summary>
public static class ContractorsReportFileNames
{
    public const string PdfContentType = "application/pdf";

    public static string Pdf(ContractorsReportHeader header) =>
        Sanitised($"{header.ProjectReference} - {header.DocumentTitle} - w-e {ContractorsReportText.Date(header.PeriodEnd)}.pdf");

    private static string Sanitised(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }
}
