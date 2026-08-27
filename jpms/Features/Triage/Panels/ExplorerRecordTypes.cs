using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Triage.Panels;

/// <summary>
/// What the record explorer can search, and how each type reads on screen. One place for the
/// labels so the type picker, the empty states and the open document header can never disagree.
/// </summary>
public static class ExplorerRecordTypes
{
    /// <summary>Every record family the explorer offers — the same set an email can be linked to.</summary>
    public static readonly RecordType[] All =
    {
        RecordType.Request, RecordType.Variation, RecordType.TenderEnquiry, RecordType.WorkOrder,
        RecordType.BidPackageInvite, RecordType.Defect, RecordType.Lad, RecordType.Todo,
        RecordType.CalendarEvent
    };

    public static string Label(RecordType type) => type switch
    {
        RecordType.Request => "Requests / RFIs",
        RecordType.Variation => "Variation Orders",
        RecordType.TenderEnquiry => "Tender Enquiries",
        RecordType.WorkOrder => "Work Orders",
        RecordType.BidPackageInvite => "Bid Package Invites",
        RecordType.Defect => "Defects",
        RecordType.Lad => "LADs claims",
        RecordType.Todo => "To-do items",
        RecordType.CalendarEvent => "Calendar events",
        _ => type.ToString()
    };

    /// <summary>Where a record's own full page lives — the explorer reads, the page edits.</summary>
    public static string? FullPageHref(LinkableRecord record) => record.Type switch
    {
        RecordType.Request => $"/projects/{record.ProjectId}/requests/view/{record.RecordId}",
        RecordType.Variation or RecordType.VariationQuote => $"/projects/{record.ProjectId}/variations/{record.RecordId}",
        RecordType.BidPackageInvite => $"/projects/{record.ProjectId}/bid-package-invites/{record.RecordId}",
        RecordType.TenderEnquiry => $"/tender-enquiries/{record.RecordId}",
        RecordType.WorkOrder => $"/projects/{record.ProjectId}/work-orders",
        RecordType.Defect => $"/projects/{record.ProjectId}/defects",
        RecordType.Scheduling or RecordType.Lad => $"/projects/{record.ProjectId}/programme",
        RecordType.Todo => string.IsNullOrEmpty(record.ProjectId) ? "/todos" : $"/projects/{record.ProjectId}/todos",
        RecordType.CalendarEvent => $"/projects/{record.ProjectId}/calendar?event={record.RecordId}",
        _ => null
    };
}
