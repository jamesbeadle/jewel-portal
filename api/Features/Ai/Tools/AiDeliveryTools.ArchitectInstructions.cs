using Jewel.JPMS.Api.Features.ArchitectInstructions;
using Jewel.JPMS.Contracts.ArchitectInstructions;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiDeliveryTools
{
    private static AiTool ListArchitectInstructions()
    {
        return new(
            "list_architect_instructions",
            "A project's Architect's Instruction register — the formal written instructions that "
            + "turn a requested change into work Jewel is entitled to be paid for. Each row: our "
            + "AI reference and the architect's own number, title, the instruction date, who "
            + "issued and filed it, the variations it covers (one instruction routinely covers "
            + "several), and documentAwaited — true means the row is a placeholder still waiting "
            + "for the paperwork. This register is what a variation at Awaiting AI is waiting for.",
            AiToolSchema.Object(
                ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false)),
            AiToolKind.Read,
            ArchitectInstructionRoles.AllowedToRead,
            ListArchitectInstructionsAsync);
    }

    private static async Task<string> ListArchitectInstructionsAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var projectId = ProjectId(context, input);
        if (string.IsNullOrWhiteSpace(projectId)) return Fail(NoProject);

        var instructions = await Query<ListArchitectInstructionsForProject, IReadOnlyList<ArchitectInstruction>>(
            context, new ListArchitectInstructionsForProject(projectId), ct);
        return Serialise(new
        {
            ok = true,
            projectId,
            count = instructions.Count,
            instructions = instructions.Select(InstructionRow)
        });
    }

    private static object InstructionRow(ArchitectInstruction instruction) => new
    {
        instruction.ArchitectInstructionId,
        instruction.Reference,
        architectsOwnRef = instruction.InstructionRef,
        display = instruction.DisplayReference,
        instruction.Title,
        instruction.Notes,
        instructedAt = instruction.InstructedAt,
        receivedAt = instruction.ReceivedAt,
        instruction.IssuedByEmail,
        instruction.FiledByEmail,
        source = instruction.Source.ToString(),
        documentAwaited = !instruction.HasFile,
        linkedVariations = instruction.Links.Select(LinkedVariationRow)
    };

    private static object LinkedVariationRow(ArchitectInstructionVariationLink link) => new
    {
        link.VariationOrderId,
        link.DisplayNumber,
        link.Title,
        status = link.Status.ToString()
    };
}
