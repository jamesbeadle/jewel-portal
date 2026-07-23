using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for the Request family (RFI/RFA/RFC/RFQ/RFP/NOD/EOT). Wraps the existing
// Requests table; this is the provider behind the Request category in triage and behind the (now
// adapter) AssignMessageToRequest path.
//
// The tag stem is project-qualified (see RequestTags): request references are only unique per
// project (every project runs its own RFI-001…), while JPMS mailbox tags share one flat category
// space — so the stem carries the project reference, e.g. "JBB-2026-001-RFI-012".
public sealed class RequestLinkProvider : ILinkableRecordProvider
{
    private readonly JpmsContext context;

    public RequestLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.Request;

    // Every request reference family, plus the REQ-NNNN fallback used when a request has no human ref.
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[]
    {
        "RFI", "RFA", "RFC", "RFQ", "RFP", "NOD", "EOT", "REQ"
    };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var projectRef = await RequestTags.ProjectRefAsync(context, projectId, ct);
        // Closed requests are finished business, so they're not offered in the triage picker — an
        // email must be linked to a live request (any status but Closed). Reopen the request first
        // if a late reply genuinely belongs to it.
        var entities = await context.Requests.AsNoTracking()
            .Where(r => r.ProjectId == projectId && r.Status != (int)RequestStatus.Closed)
            .ToListAsync(ct);
        // Pickers read in reference order — the same ordering as the project's Requests register
        // (ProjectRequests.razor): references grouped by prefix (NOD, REQ, RFI, …) with numbers
        // ascending within each group, so the REQ-#### run comes first and the RFI block matches
        // the RFI register order. Sorted here so every consumer of the list (the triage Link panel
        // and the To-do form's request-link dropdown) agrees.
        return entities.Select(e => ToLinkable(e, projectRef))
            .OrderBy(r => ReferenceKey(r.Reference).Prefix, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => ReferenceKey(r.Reference).Number)
            .ThenBy(r => r.Reference, StringComparer.OrdinalIgnoreCase)
            .ToList().AsReadOnly();
    }

    // Mirrors ProjectRequests.razor's ReferenceKey: the number is parsed rather than
    // string-compared so unpadded or suffixed references (RFI-49, RFI-049A, RFI-1000) still land
    // in numeric order; free-text or blank references sort after the numbered run.
    private static (string Prefix, int Number) ReferenceKey(string? reference)
    {
        var raw = (reference ?? "").Trim();
        if (raw.Length == 0) return ("\uFFFF", int.MaxValue); // blanks last

        var dash = raw.IndexOf('-');
        if (dash > 0)
        {
            var digits = new string(raw[(dash + 1)..].TakeWhile(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var number))
                return (raw[..dash], number);
        }
        return (raw, int.MaxValue); // unnumbered free text after numbered refs
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var entity = await context.Requests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.RequestId == recordId, ct);
        if (entity is null) return null;

        var projectRef = await RequestTags.ProjectRefAsync(context, entity.ProjectId, ct);
        return ToLinkable(entity, projectRef);
    }

    private static LinkableRecord ToLinkable(RequestEntity entity, string? projectRef) => new(
        Type:         RecordType.Request,
        RecordId:     entity.RequestId,
        ProjectId:    entity.ProjectId,
        Reference:    string.IsNullOrWhiteSpace(entity.Reference) ? entity.TagReference : entity.Reference.Trim(),
        TagReference: RequestTags.Stem(projectRef, entity.ProjectId, entity.TagReference),
        Title:        entity.Title,
        StatusLabel:  ((RequestStatus)entity.Status).DisplayName(),
        Summary:      RecordSummaries.Clip(entity.Description));
}
