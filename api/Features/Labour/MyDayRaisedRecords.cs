using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Closeout;
using Jewel.JPMS.Api.Features.SiteInstructions;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>
/// The records a worker's day raises beside its note, in the worker's name and linked to the
/// note, for the office to triage: an instruction given on site becomes a Site Instruction
/// (SI-####, never a Request — Jeremy, 21 Sep 2026) and a defect becomes a defect (DEF-####).
/// Neither is written into the day's words, which go to the client in the Contractor's Report.
/// The rows are added to the context; the day's one save commits them with the note.
/// </summary>
public sealed class MyDayRaisedRecords
{
    private readonly JpmsContext context;
    public MyDayRaisedRecords(JpmsContext context) { this.context = context; }

    public async Task<string> RaiseInstructionAsync(
        SiteLogInstruction? instruction, ProgressUpdateEntity note, string email, CancellationToken cancellationToken)
    {
        if (!SiteLogRules.IsGiven(instruction)) return "";
        var givenBy = instruction!.GivenBy.Trim();
        var row = new SiteInstructionEntity
        {
            SiteInstructionId = Guid.NewGuid().ToString("N"),
            ProjectId = note.ProjectId,
            Number = await SiteInstructionNumbers.NextAsync(context, cancellationToken),
            Title = TitleOf(givenBy, note),
            Instruction = instruction.Instruction.Trim(),
            GivenBy = givenBy,
            IsVerbal = instruction.IsVerbal,
            GivenOn = note.WorkDate,
            ProgressUpdateId = note.ProgressUpdateId,
            RaisedByEmail = email,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.SiteInstructions.Add(row);
        return row.Reference;
    }

    public async Task<string> RaiseDefectAsync(string defect, ProgressUpdateEntity note, string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(defect)) return "";
        var row = new DefectEntity
        {
            DefectId = CloseoutIdentifierFactory.NextDefectId(),
            ProjectId = note.ProjectId,
            Number = await DefectNumbers.NextAsync(context, cancellationToken),
            Description = defect.Trim(),
            Status = (int)DefectStatus.Open,
            RaisedAt = DateTimeOffset.UtcNow,
            ProgressUpdateId = note.ProgressUpdateId,
            RaisedByEmail = email
        };
        context.Defects.Add(row);
        return row.Reference;
    }

    private static string TitleOf(string givenBy, ProgressUpdateEntity note)
    {
        var day = note.WorkDate ?? note.CreatedAt;
        return $"Given on site by {givenBy}, {day:d MMM yyyy}";
    }
}
