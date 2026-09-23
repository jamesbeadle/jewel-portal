namespace Jewel.JPMS.Models;

/// <summary>
/// The rules a right-to-work check is held to, carried from the dashboard's register (gov.uk,
/// checked 19 Aug 2026 and 23 Sep 2026): no completed check, no start; a pass is a legal statement
/// and cannot be half true; the date of the check is never backdated or forward-dated; an employee
/// checked after their first day is marked as a late check; time-limited permission is followed up
/// ten weeks before it runs out.
/// </summary>
public static class RightToWorkRules
{
    public const int FollowUpDaysBeforeExpiry = 70;
    public const string LateCheck = "Late check, no statutory excuse for the period before this date";
    public const string WhoIsBeingEngaged = "Who is being engaged?";
    public const string HowEngaged = "Engaged as an employee or an individual subcontractor?";
    public const string WhichRoute = "Which route did you use?";

    public static bool IsCleared(RightToWorkCheckDetails details) =>
        details.Outcome == RightToWorkOutcome.Pass && HasAllThreeConfirmations(details) && details.CheckedByName.Length > 0;

    public static bool IsLate(RightToWorkCheckDetails details) =>
        details.EngagedAs == Engagement.Employee && details.EngagedSince is { } since && details.CheckedOn > since;

    public static DateOnly? FollowUpFor(DateOnly? permissionExpiresOn) => permissionExpiresOn?.AddDays(-FollowUpDaysBeforeExpiry);

    public static IReadOnlyList<string> ProblemsWith(RightToWorkCheckDetails details, DateOnly today)
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(details.PersonName)) problems.Add(WhoIsBeingEngaged);
        if (string.IsNullOrWhiteSpace(details.CheckedByName)) problems.Add("Person who carried out the check is required");
        if (details.CheckedOn == default) problems.Add("The date of the check is the thing that gives you the excuse. Fill it in.");
        if (details.CheckedOn > today) problems.Add("The date of the check cannot be in the future. Never backdate, never forward-date.");
        if (NeedsAReference(details)) problems.Add("That route needs the share code or the IDSP report reference.");
        if (IsAHalfTruePass(details))
            problems.Add("All three confirmations must be ticked for a pass. If one of them is not true, this is not a completed check.");
        if (IsUndatedTimeLimitedPass(details))
            problems.Add("Time-limited permission needs the expiry date, or nobody will know to check again.");
        return problems;
    }

    /// <summary>What is still missing from a check, in the register's words; a query or a fail has nothing left to finish.</summary>
    public static IReadOnlyList<string> GapsIn(RightToWorkCheckDetails details)
    {
        var gaps = new List<string>();
        if (details.CheckedByName.Length == 0) gaps.Add("no checker named");
        if (details.CheckedOn == default) gaps.Add("no date of check");
        if (!details.IsDocumentGenuine) gaps.Add("document not confirmed genuine");
        if (!details.IsLikenessConfirmed) gaps.Add("likeness not confirmed");
        if (!details.IsPermittedToDoTheWork) gaps.Add("permission to do this work not confirmed");
        if (!details.IsEvidenceFiled) gaps.Add("evidence not filed");
        if (NeedsAReference(details)) gaps.Add("no share code or IDSP reference");
        return gaps;
    }

    public static bool IsDoNotStart(RightToWorkCheckDetails details) => details.Outcome != RightToWorkOutcome.Pass;

    private static bool HasAllThreeConfirmations(RightToWorkCheckDetails details) =>
        details.IsDocumentGenuine && details.IsLikenessConfirmed && details.IsPermittedToDoTheWork;

    private static bool NeedsAReference(RightToWorkCheckDetails details) =>
        details.Route != RightToWorkRoute.OriginalPassportSeen && string.IsNullOrWhiteSpace(details.Reference);

    private static bool IsAHalfTruePass(RightToWorkCheckDetails details) =>
        details.Outcome == RightToWorkOutcome.Pass && !HasAllThreeConfirmations(details);

    private static bool IsUndatedTimeLimitedPass(RightToWorkCheckDetails details) =>
        details.Outcome == RightToWorkOutcome.Pass && details.IsTimeLimited && details.PermissionExpiresOn is null;
}
