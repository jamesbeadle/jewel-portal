using Jewel.JPMS.Contracts.SiteInstructions;

namespace Jewel.JPMS.Contracts.Labour;

/// <summary>An instruction given on site during the day, as the worker heard it: what was asked,
/// who asked it, and whether it was said rather than written. The day's log raises it as a Site
/// Instruction (SI-####) for the office to triage — never a Request, never a line in the day's
/// words (Jeremy, 21 Sep 2026: "Site Instruction always"). A verbal direction is not an
/// instruction until it is confirmed in writing, which is why the register is told it was verbal.</summary>
public sealed record SiteLogInstruction(string Instruction, string GivenBy, bool IsVerbal = true);

/// <summary>What the day's log may raise beside its note, checked the same way on the phone and at
/// the door: an instruction needs the words and who gave them, within the register's own limits.</summary>
public static class SiteLogRules
{
    public static bool IsGiven(SiteLogInstruction? instruction) => instruction is not null;

    public static IReadOnlyList<string> CheckInstruction(SiteLogInstruction? instruction)
    {
        if (instruction is null) return Array.Empty<string>();
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(instruction.Instruction)) errors.Add("Say what was asked, or clear the instruction box.");
        if (string.IsNullOrWhiteSpace(instruction.GivenBy)) errors.Add("Say who gave the instruction.");
        if (instruction.GivenBy.Length > SiteInstructionLimits.GivenByMaxLength)
            errors.Add($"Who gave it is limited to {SiteInstructionLimits.GivenByMaxLength} characters.");
        return errors;
    }
}
