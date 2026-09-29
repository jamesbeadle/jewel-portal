using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>The references of the records a day's log raised beside its note — the Site
/// Instruction and the defect linked to the note — read back for a set of notes at once, so a
/// card or a feed can say what went to the office without loading either register.</summary>
public sealed class MyDayRaisedReferences
{
    public sealed record References(string SiteInstruction = "", string Defect = "");

    private readonly IReadOnlyDictionary<string, string> instructionsByNote;
    private readonly IReadOnlyDictionary<string, string> defectsByNote;

    private MyDayRaisedReferences(IReadOnlyDictionary<string, string> instructionsByNote, IReadOnlyDictionary<string, string> defectsByNote)
    { this.instructionsByNote = instructionsByNote; this.defectsByNote = defectsByNote; }

    public static async Task<MyDayRaisedReferences> ForNotesAsync(
        JpmsContext context, IReadOnlyList<string> noteIds, CancellationToken cancellationToken)
    {
        var instructions = await context.SiteInstructions
            .Where(row => row.ProgressUpdateId != null && noteIds.Contains(row.ProgressUpdateId))
            .Select(row => new { row.ProgressUpdateId, row.Number })
            .ToListAsync(cancellationToken);
        var defects = await context.Defects
            .Where(row => row.ProgressUpdateId != null && noteIds.Contains(row.ProgressUpdateId))
            .Select(row => new { row.ProgressUpdateId, row.Number })
            .ToListAsync(cancellationToken);
        return new MyDayRaisedReferences(
            instructions.ToDictionary(row => row.ProgressUpdateId!, row => SiteInstructionEntity.ReferenceFor(row.Number)),
            defects.ToDictionary(row => row.ProgressUpdateId!, row => DefectEntity.ReferenceFor(row.Number)));
    }

    public References For(string noteId) =>
        new(instructionsByNote.GetValueOrDefault(noteId, ""), defectsByNote.GetValueOrDefault(noteId, ""));
}
