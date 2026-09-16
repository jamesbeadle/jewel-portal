namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>The Valuation No. the report defaults to: the highest payment certificate number on
/// the project's register — numerically when every number is one, else as text.</summary>
internal static class ContractorsReportCertificates
{
    public static async Task<string?> LastNumberAsync(JpmsContext context, string projectId, CancellationToken cancellationToken)
    {
        var numbers = await context.PaymentCertificates.AsNoTracking()
            .Where(row => row.ProjectId == projectId)
            .Select(row => row.CertificateNumber)
            .ToListAsync(cancellationToken);
        return Highest(numbers);
    }

    public static string? Highest(IReadOnlyList<string> numbers)
    {
        var named = numbers.Where(number => !string.IsNullOrWhiteSpace(number)).Select(number => number.Trim()).ToList();
        if (named.Count == 0) return null;
        return named
            .OrderByDescending(number => int.TryParse(number, out var value) ? value : int.MinValue)
            .ThenByDescending(number => number, StringComparer.OrdinalIgnoreCase)
            .First();
    }
}
