using System.Text.RegularExpressions;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>The Valuation No. a new report opens with (Jeremy's rule, 24 Sep 2026): the project's
/// current valuation — the newest on the Valuation Reports register not yet Confirmed — numbered
/// as its name reads ("Valuation 21 - September 2026" is 21; the claim's own number is a
/// sequence, not the valuation's). With no such valuation, the highest payment certificate.</summary>
internal static partial class ContractorsReportValuations
{
    public static async Task<string?> CurrentNumberAsync(JpmsContext context, string projectId, CancellationToken cancellationToken)
    {
        var names = await context.ValuationClaims.AsNoTracking()
            .Where(row => row.ProjectId == projectId && row.Status != (int)ValuationClaimStatus.Confirmed)
            .OrderByDescending(row => row.ClaimDate)
            .ThenByDescending(row => row.ClaimNumber)
            .Select(row => row.Name)
            .ToListAsync(cancellationToken);
        var current = names.Select(NumberIn).FirstOrDefault(number => number is not null);
        return current ?? await ContractorsReportCertificates.LastNumberAsync(context, projectId, cancellationToken);
    }

    public static string? NumberIn(string name)
    {
        var match = ValuationNumber().Match(name);
        return match.Success ? match.Groups["number"].Value : null;
    }

    [GeneratedRegex(@"\bvaluation\s*(?:no\.?\s*)?(?<number>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex ValuationNumber();
}
