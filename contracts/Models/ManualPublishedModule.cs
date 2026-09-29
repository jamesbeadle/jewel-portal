namespace Jewel.JPMS.Models;

/// <summary>
/// One module as a site view shows it: the last approved text and nothing else, with whether the
/// signed-in reader has acknowledged this version.
/// </summary>
public sealed record ManualPublishedModule(
    string ManualModuleId,
    string Code,
    string Title,
    string Purpose,
    string Body,
    int Version,
    DateTimeOffset ApprovedAt,
    string ApprovedByEmail,
    string OwnerEmail,
    DateTimeOffset? NextReviewAt,
    IReadOnlyList<string> LinkedFormSlugs,
    string LinkedStandards,
    string ChangeSummary,
    DateTimeOffset? AcknowledgedAt)
{
    public bool HasAcknowledged => AcknowledgedAt is not null;

    public bool HasLinkedForms => LinkedFormSlugs.Count > 0;

    public string Family => ManualModuleCodes.FamilyOf(Code);
}

/// <summary>A published view of the manual: its modules in family order, as issued at one moment.</summary>
public sealed record ManualViewReading(ManualView View, IReadOnlyList<ManualPublishedModule> Modules, DateTimeOffset IssuedAt)
{
    public int ModuleCount => Modules.Count;

    public int OutstandingCount => Modules.Count(module => !module.HasAcknowledged);
}

/// <summary>One approved version of a module, kept whole when the next supersedes it.</summary>
public sealed record ManualModuleVersion(
    string ManualModuleVersionId,
    string ManualModuleId,
    int Version,
    string Title,
    string Body,
    string ChangeSummary,
    string ApprovedByEmail,
    DateTimeOffset ApprovedAt,
    DateTimeOffset? SupersededAt,
    int AcknowledgedCount)
{
    public bool IsCurrent => SupersededAt is null;
}

/// <summary>One person's acknowledgement of one version of a module: a typed name and a server time.</summary>
public sealed record ManualAcknowledgement(
    string ManualAcknowledgementId,
    string ManualModuleId,
    int Version,
    string Email,
    string TypedName,
    DateTimeOffset AcknowledgedAt);

/// <summary>A module in full for its own page: the master row, the text the site sees, every version and every acknowledgement.</summary>
public sealed record ManualModuleDetail(
    ManualModule Module,
    string PublishedBody,
    IReadOnlyList<ManualModuleVersion> Versions,
    IReadOnlyList<ManualAcknowledgement> Acknowledgements);

/// <summary>What loading the JBB baseline did: the modules it created and the codes it left alone.</summary>
public sealed record ManualBaselineImport(IReadOnlyList<string> CreatedCodes, IReadOnlyList<string> SkippedCodes)
{
    public int CreatedCount => CreatedCodes.Count;
}
