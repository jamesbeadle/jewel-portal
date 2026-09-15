using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for sales leads (the Sales pane, 2026-09-15): an estimate enquiry
// forwarded to the projects mailbox is tagged "JPMS/LD-####" to the lead it is about, and the
// lead reads its mail back live like every other record. A lead belongs to NO project, so every
// projection carries an empty ProjectId — even a Won lead's: from Won onwards the project's own
// records own the correspondence. ForProjectAsync therefore ignores the project it is handed and
// lists the company-wide register (every lead that is not Won, newest first).
public sealed class LeadLinkProvider : ILinkableRecordProvider, ITagResolvingProvider
{
    private readonly JpmsContext context;

    public LeadLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.Lead;

    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "LD" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var entities = await context.Leads.AsNoTracking()
            .Where(lead => lead.Stage != (int)LeadStage.Won)
            .OrderByDescending(lead => lead.Number)
            .ToListAsync(ct);
        return entities.Select(ToLinkable).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var entity = await context.Leads.AsNoTracking()
            .FirstOrDefaultAsync(lead => lead.LeadId == recordId, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    public async Task<LinkableRecord?> FindByTagAsync(string tagReference, CancellationToken ct)
    {
        if (!TagReferenceParsing.TryParseNumber(tagReference, "LD", out var number)) return null;
        var entity = await context.Leads.AsNoTracking()
            .FirstOrDefaultAsync(lead => lead.Number == number, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    private static LinkableRecord ToLinkable(LeadEntity entity)
    {
        var reference = entity.DisplayReference;
        var stage = (LeadStage)entity.Stage;
        return new LinkableRecord(
            Type:         RecordType.Lead,
            RecordId:     entity.LeadId,
            ProjectId:    "",
            Reference:    reference,
            TagReference: reference,
            Title:        TitleFor(entity, reference),
            StatusLabel:  stage.DisplayName(),
            Summary:      RecordSummaries.Clip(entity.Summary),
            IsActive:     stage.IsOpen());
    }

    private static string TitleFor(LeadEntity entity, string reference)
    {
        var parts = new[] { entity.ContactName, entity.SiteAddress }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part.Trim())
            .ToList();
        return parts.Count == 0 ? reference : string.Join(" — ", parts);
    }
}
