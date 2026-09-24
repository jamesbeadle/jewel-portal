using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>Which photographs Section 9 prints (the FD's ask, 24 Sep 2026: Report 31's four days
/// held 160, issued reports carry about 12 a day): every photograph on the selected updates
/// except those the report leaves out. The updates keep every photograph as the record.</summary>
internal static class ContractorsReportPhotoChoices
{
    public static IReadOnlyList<ContractorsReportUpdate> Printed(ContractorsReport report, IReadOnlyList<ContractorsReportUpdate> updates)
    {
        var excluded = report.ExcludedPhotoIds.ToHashSet();
        return Selected(report, updates)
            .Select(update => update with { Photos = update.Photos.Where(photo => !excluded.Contains(photo.ProgressPhotoId)).ToList() })
            .ToList();
    }

    public static IReadOnlyList<ContractorsReportPhotoChoice> For(ContractorsReport report, IReadOnlyList<ContractorsReportUpdate> updates)
    {
        var excluded = report.ExcludedPhotoIds.ToHashSet();
        return Selected(report, updates)
            .SelectMany(update => update.Photos.Select(photo => new ContractorsReportPhotoChoice(
                photo.ProgressPhotoId, update.ProgressUpdateId, update.WorkDate, photo.FileName,
                !excluded.Contains(photo.ProgressPhotoId))))
            .ToList();
    }

    private static IEnumerable<ContractorsReportUpdate> Selected(ContractorsReport report, IReadOnlyList<ContractorsReportUpdate> updates) =>
        updates.Where(update => report.SelectedUpdateIds.Contains(update.ProgressUpdateId));
}
