using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.SiteInstructions;

namespace Jewel.JPMS.Api.Features.SiteInstructions.Commands;

public sealed class AddSiteInstructionHandler : ICommandHandler<AddSiteInstruction, SiteInstruction>
{
    private readonly JpmsContext context;
    public AddSiteInstructionHandler(JpmsContext context) { this.context = context; }

    public async Task<SiteInstruction> HandleAsync(AddSiteInstruction command, CancellationToken cancellationToken)
    {
        // Global sequence (like defect and inventory numbers): max + 1, never a row count —
        // deleted rows must not re-issue a number, because the number is the mailbox tag stem
        // ("JPMS/SI-0001").
        var nextNumber = (await context.SiteInstructions.MaxAsync(row => (int?)row.Number, cancellationToken) ?? 0) + 1;

        var entity = new SiteInstructionEntity
        {
            SiteInstructionId = Guid.NewGuid().ToString("N"),
            ProjectId = command.ProjectId,
            Number = nextNumber,
            Title = command.Title,
            Instruction = command.Instruction,
            Location = command.Location,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.SiteInstructions.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
