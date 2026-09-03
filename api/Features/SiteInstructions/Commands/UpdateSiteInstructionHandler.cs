using Jewel.JPMS.Contracts.SiteInstructions;

namespace Jewel.JPMS.Api.Features.SiteInstructions.Commands;

public sealed class UpdateSiteInstructionHandler : ICommandHandler<UpdateSiteInstruction, SiteInstruction>
{
    private readonly JpmsContext context;
    public UpdateSiteInstructionHandler(JpmsContext context) { this.context = context; }

    public async Task<SiteInstruction> HandleAsync(UpdateSiteInstruction command, CancellationToken cancellationToken)
    {
        var entity = await context.SiteInstructions.FindAsync(new object[] { command.SiteInstructionId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Site instruction {command.SiteInstructionId} not found.");
        entity.Title = command.Title;
        entity.Instruction = command.Instruction;
        entity.Location = command.Location;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
