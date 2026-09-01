using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Drawings;
using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

/// <summary>
/// Creates the Lead-stage project shell a fresh enquiry lives on: reference minted from the
/// organisation and year, the architect practice found (by name) or created and set as the
/// project's correspondent party, the site address carried across. The project exists before
/// the enquiry row is written, so the enquiry always has a home.
/// </summary>
public sealed class TenderEnquiryProjectCreator
{
    private const string ReferenceNumberFormat = "000";

    private readonly JpmsContext context;

    public TenderEnquiryProjectCreator(JpmsContext context) { this.context = context; }

    public async Task<ProjectEntity> CreateAsync(
        TenderEnquiryProjectDraft draft, TenderEnquiryDetails details, string projectManagerEmail,
        CancellationToken cancellationToken)
    {
        var architect = await FindOrCreateArchitectAsync(details, cancellationToken);
        var project = new ProjectEntity
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            Reference = await MintReferenceAsync(draft.Organisation, cancellationToken),
            Name = TenderEnquiryDetailsRules.Clamp(draft.Name, 256),
            ClientName = ClientNameFor(draft, details),
            Organisation = (int)draft.Organisation,
            Stage = (int)ProjectStage.Lead,
            ProjectManagerEmail = projectManagerEmail,
            CreatedAt = DateTimeOffset.UtcNow,
            PartyKind = (int)PartyKind.Architect,
            PartyId = architect.ArchitectId,
            AddressLine = TenderEnquiryDetailsRules.Clamp(draft.AddressLine, 256),
            Town = TenderEnquiryDetailsRules.Clamp(draft.Town, 128),
            Postcode = TenderEnquiryDetailsRules.Clamp(draft.Postcode, 16)
        };
        context.Projects.Add(project);
        // Every project starts with the standard drawing-folder set — leads included, so the
        // register is ready the moment the job is won; one SaveChanges covers both.
        await StandardDrawingFolders.AddMissingAsync(context, project.ProjectId, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return project;
    }

    // The architect's client is usually unnamed at enquiry stage — the practice stands in until
    // someone edits the project details.
    private static string ClientNameFor(TenderEnquiryProjectDraft draft, TenderEnquiryDetails details) =>
        string.IsNullOrWhiteSpace(draft.ClientName)
            ? $"Client of {details.ArchitectPracticeName.Trim()}"
            : TenderEnquiryDetailsRules.Clamp(draft.ClientName, 256);

    private async Task<ArchitectEntity> FindOrCreateArchitectAsync(TenderEnquiryDetails details, CancellationToken cancellationToken)
    {
        var practiceName = details.ArchitectPracticeName.Trim();
        var existing = await context.Architects
            .FirstOrDefaultAsync(row => row.Name.ToLower() == practiceName.ToLower(), cancellationToken);
        if (existing is not null) return existing;

        var architect = new ArchitectEntity
        {
            ArchitectId = Guid.NewGuid().ToString("N"),
            Name = TenderEnquiryDetailsRules.Clamp(practiceName, 256),
            ContactName = NullIfBlank(details.ArchitectContactName),
            ContactEmail = NullIfBlank(details.ArchitectContactEmail),
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Architects.Add(architect);
        return architect;
    }

    // JBB-2026-014: the next free number in this organisation's year. Counted from the rows that
    // already carry the stem, then walked forward so a hand-typed reference can never be reused.
    private async Task<string> MintReferenceAsync(Organisation organisation, CancellationToken cancellationToken)
    {
        var stem = $"{organisation.ShortCode()}-{DateTime.UtcNow:yyyy}-";
        var taken = await context.Projects
            .Where(row => row.Reference.StartsWith(stem))
            .Select(row => row.Reference)
            .ToListAsync(cancellationToken);
        var next = taken.Count + 1;
        while (taken.Contains(stem + next.ToString(ReferenceNumberFormat), StringComparer.OrdinalIgnoreCase)) next++;
        return stem + next.ToString(ReferenceNumberFormat);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
