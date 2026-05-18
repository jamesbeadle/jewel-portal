namespace Jewel.JourneyIndex.Models;

/// <summary>
/// A user role / persona in the Project Program Scheduler.
/// Mirrors the structure of <c>docs/requirements/personas.md</c>.
/// </summary>
public sealed record Persona(
    string Slug,
    string Code,
    string Name,
    string ShortDescription,
    string Role,
    string ReportsTo,
    string ToolingToday,
    string Frequency,
    IReadOnlyList<string> Goals,
    IReadOnlyList<string> PainPoints,
    IReadOnlyList<string> KeyResponsibilities,
    string DevicesAndEnvironment,
    string? Note,
    string AccentDotClass,
    IReadOnlyList<JourneyStub> Journeys
);

/// <summary>
/// Placeholder for a user journey — populated as journeys are articulated
/// in <c>docs/user-journeys/</c>.
/// </summary>
public sealed record JourneyStub(
    string Slug,
    string Title,
    string Status  // "Not started" | "Draft" | "In Review" | "Confirmed"
);
