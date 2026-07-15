using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// One per project: the client-retention terms and the confirmed state of the two release
// milestones. Forecast amounts and due dates are never stored — they're calculated from
// the valuation figures (see RetentionSchedule in contracts).
public sealed class ProjectRetentionEntity
{
    [Key, MaxLength(64)] public string ProjectRetentionId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public decimal RetentionPercent { get; set; }
    public decimal CompletionReleasePercent { get; set; }
    public int DefectsPeriodMonths { get; set; }
    public DateTimeOffset? PracticalCompletionAt { get; set; }
    public DateTimeOffset? CompletionReleaseConfirmedAt { get; set; }
    public decimal CompletionReleaseAmount { get; set; }
    public DateTimeOffset? FinalReleaseConfirmedAt { get; set; }
    public decimal FinalReleaseAmount { get; set; }
}
