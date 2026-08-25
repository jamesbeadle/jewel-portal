namespace Jewel.JPMS.Services.Ai;

/// <summary>
/// Works out which record a route is showing, so a conversation started on V72 is findable from V72
/// rather than only from whoever started it.
///
/// <para>Derived from the URL shape rather than a hand-kept list: project record pages are all
/// <c>/projects/{projectId}/{section}/{recordId}</c>, so a new tab following that shape is picked up
/// without anyone remembering to add it here. Only the singular names are mapped, because "variation"
/// is what the model should say and "variations" is what the route says.</para>
/// </summary>
public static class RouteRecord
{
    private const string ProjectPrefix = "/projects/";
    // The one company-wide record page: an enquiry sits on a Lead project but is reached from the
    // Internal folder, so its route carries no project segment.
    private const string TenderEnquiryPrefix = "/tender-enquiries/";
    private const string TenderEnquiryType = "tender enquiry";

    /// <summary>The record on this route, or (null, null) when the page is not about one.</summary>
    public static (string? Type, string? Id) From(string path)
    {
        if (path.StartsWith(TenderEnquiryPrefix, StringComparison.Ordinal))
        {
            var enquiryId = path[TenderEnquiryPrefix.Length..].Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return string.IsNullOrEmpty(enquiryId) ? (null, null) : (TenderEnquiryType, enquiryId.Split('?')[0]);
        }
        if (!path.StartsWith(ProjectPrefix, StringComparison.Ordinal)) return (null, null);

        var segments = path[ProjectPrefix.Length..]
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        // [projectId, section, recordId] — anything shorter is the project or a register, not a record.
        if (segments.Length < 3) return (null, null);

        var type = Singular(segments[1]);
        if (type is null) return (null, null);

        // The request detail route is /requests/view/{id} — "view" is a literal segment, not the
        // id (without this the scope stamped RecordId "view"); and /requests/{kind} is the
        // register's tab (all, general, rfis…), never a record.
        if (string.Equals(segments[2], "view", StringComparison.OrdinalIgnoreCase))
            return segments.Length >= 4 ? (type, segments[3]) : (null, null);
        if (string.Equals(segments[1], "requests", StringComparison.OrdinalIgnoreCase))
            return (null, null);

        return (type, segments[2]);
    }

    /// <summary>Null for a section that is not a record register — settings, financials and so on.</summary>
    private static string? Singular(string section) => section.ToLowerInvariant() switch
    {
        "variations" => "variation",
        // The old route kept alive so links already sent out still land — same record.
        "voq" => "variation",
        "requests" => "request",
        "rfis" => "request",
        "bid-packages" => "bid package",
        // The invite page is the bid package's working surface — same record, its own route.
        "bid-package-invites" => "bid package",
        "work-orders" => "work order",
        "drawings" => "drawing",
        "defects" => "defect",
        "valuations" => "valuation",
        "valuation-invoices" => "valuation invoice",
        _ => null
    };
}
