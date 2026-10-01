using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// A week an operative submitted from My day for the office to review in one step (2026-10-01).
/// One row per worker and week (WeekStart is the Monday, midnight UTC); Status mirrors the
/// contracts WorkerWeekSubmissionStatus. Submitting again after a send-back reuses the row. The
/// sign-off itself approves the week's timesheets and writes the LabourWeekSignOff markers — this
/// row records the asking and the answer, with who answered and when.
/// </summary>
[Index(nameof(WorkerId), nameof(WeekStart), IsUnique = true, Name = "IX_WorkerWeekSubmissions_WorkerId_WeekStart")]
public sealed class WorkerWeekSubmissionEntity
{
    [Key, MaxLength(64)] public string WorkerWeekSubmissionId { get; set; } = "";
    [MaxLength(64)]      public string WorkerId { get; set; } = "";
    public DateTimeOffset WeekStart { get; set; }
    public int Status { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    [MaxLength(256)]     public string ReviewedByEmail { get; set; } = "";
    public DateTimeOffset? ReviewedAt { get; set; }
    public string ReviewNote { get; set; } = "";
}
