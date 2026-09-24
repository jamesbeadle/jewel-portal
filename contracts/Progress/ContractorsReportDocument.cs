namespace Jewel.JPMS.Contracts.Progress;

/// <summary>
/// The Contractor's Report as it will print — composed from the register at read time, so the
/// page shows exactly what Word and PDF will carry, and the two renderers draw the same thing.
/// Sections in PLG's order: 1 Progress Against Programme, 2 Look Ahead, 3 Decisions /
/// Instructions Needed, 4 Variations, 5 Neighbours, 6 Health &amp; Safety, 7 Building Control,
/// 8 Specialist Subcontractors, 9 Site Photographs (the days' photos, under the same headings as
/// Section 1). <see cref="Findings"/> is the gate: while it holds anything the build is refused
/// naming the line.
/// </summary>
public sealed record ContractorsReportDocument(
    ContractorsReportHeader Header,
    IReadOnlyList<ContractorsReportDay> Progress,
    IReadOnlyList<ContractorsReportLookAheadItem> LookAhead,
    IReadOnlyList<ContractorsReportDecision> Decisions,
    IReadOnlyList<ContractorsReportVariation> Variations,
    decimal VariationsTotal,
    string Neighbours,
    string HealthAndSafety,
    ContractorsReportBuildingControl BuildingControl,
    IReadOnlyList<ContractorsReportSubcontractor> Subcontractors,
    IReadOnlyList<ContractorsReportFinding> Findings)
{
    public bool CanBeBuilt => Findings.Count == 0;
}

public sealed record ContractorsReportHeader(
    string ProjectName,
    string ProjectReference,
    string DocumentTitle,
    string ValuationNumber,
    // The highest certificate number on the register at build time, or null when none is filed.
    string? LastCertificateNumber,
    string ProgrammeReference,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string PreparedByName,
    string IssuedTo,
    DateOnly DateOfIssue);

/// <summary>One working day of Section 1 — Friday first, carrying the weekend, then Monday to
/// Thursday. A day with no selected update is listed with no entries so the reader sees the
/// week whole; a day with no photographs is left out of Section 9.</summary>
public sealed record ContractorsReportDay(
    DateOnly Date,
    string Heading,
    IReadOnlyList<ContractorsReportEntry> Entries)
{
    public IReadOnlyList<ContractorsReportPhoto> Photos => Entries.SelectMany(entry => entry.Photos).ToList();
}

public sealed record ContractorsReportEntry(
    string ProgressUpdateId,
    string Title,
    string Description,
    IReadOnlyList<ContractorsReportPhoto> Photos);

public sealed record ContractorsReportPhoto(string ProgressPhotoId, string FileName, string ContentType);

/// <summary>Section 3: an RFI open at build time.</summary>
public sealed record ContractorsReportDecision(string RequestId, string Reference, string Title, string Status, DateOnly? ResponseDue);

/// <summary>Section 4: a variation not yet approved or rejected, at its value excluding VAT.</summary>
public sealed record ContractorsReportVariation(string VariationOrderId, string DisplayNumber, string Title, string Status, decimal Value);

/// <summary>Section 7: the standing Building Control contact from the project's case, plus the
/// entered liaison line.</summary>
public sealed record ContractorsReportBuildingControl(
    string? BodyName,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string Liaison);

/// <summary>A work order released or active in the period, with the entered attendance. The
/// page's attendance table lists every one on site; Section 8 prints only those given days on site
/// — Nigel, 20 Sep, and Report 30 as issued.</summary>
public sealed record ContractorsReportSubcontractor(
    string WorkOrderId,
    string Reference,
    string Supplier,
    string Scope,
    decimal Value,
    DateOnly? TargetCompletion,
    int? AttendanceDays,
    bool IsClientNominated);

/// <summary>A line the report may not carry as written — a banned word, named by section and line.</summary>
public sealed record ContractorsReportFinding(string Section, string Line, string Reason);

/// <summary>A progress update in the period, for the page's tick list.</summary>
public sealed record ContractorsReportUpdateChoice(string ProgressUpdateId, DateOnly WorkDate, string Title, int PhotoCount, bool IsSelected);

/// <summary>The report page's one read: the record, the document it composes to, the updates in
/// its period to choose from, and the work orders on site to enter attendance against — of which
/// only those that attended reach the document's Section 8.</summary>
public sealed record ContractorsReportView(
    ContractorsReport Report,
    ContractorsReportDocument Document,
    IReadOnlyList<ContractorsReportUpdateChoice> UpdatesInPeriod,
    IReadOnlyList<ContractorsReportSubcontractor> WorkOrdersOnSite);
