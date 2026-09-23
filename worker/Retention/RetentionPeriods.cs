namespace Jewel.JPMS.Worker.Retention;

/// <summary>
/// How long the portal keeps what it no longer needs. A spent credential row is a liability
/// tied to a person's email, not a record of the business; a cached scan can be re-read from
/// the document it came from. Each period is the grace after the row stopped being useful.
/// The onboarding forms keep their own clocks — FormRetention in contracts, run nightly by
/// Worker/Forms/FormRetentionWorker — because each starts on a date the office records.
/// </summary>
internal static class RetentionPeriods
{
    /// <summary>Sessions, invite/reset tokens and OAuth codes and tokens, after they expired or
    /// were consumed — long enough to investigate an incident, no longer.</summary>
    public static readonly TimeSpan SpentCredentials = TimeSpan.FromDays(30);

    /// <summary>An access request nobody has approved or declined.</summary>
    public static readonly TimeSpan UnansweredAccessRequests = TimeSpan.FromDays(90);

    /// <summary>The OCR text of a scanned document; the next read re-OCRs the file on a miss.</summary>
    public static readonly TimeSpan ScanText = TimeSpan.FromDays(180);

    /// <summary>The audit trail: who did what to a contract, an order, a valuation. Kept as long
    /// as the financial records it evidences — six years past the year they belong to — and no
    /// longer, because every row names a person.</summary>
    public static readonly TimeSpan AuditTrail = TimeSpan.FromDays(365 * 7);

    /// <summary>The agent activity log: the assistant's own runs, with who asked and the cost.
    /// Operational, not evidential — two years is enough to read the trend.</summary>
    public static readonly TimeSpan AgentActivity = TimeSpan.FromDays(365 * 2);
}
